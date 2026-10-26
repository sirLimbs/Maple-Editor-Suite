#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapleLib.WzLib;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class ImgFileSystemManager : IDisposable
{
	public static readonly string[] STANDARD_CATEGORIES = new string[17]
	{
		"Base", "String", "Map", "Mob", "Npc", "Reactor", "Sound", "Skill", "Character", "Item",
		"UI", "Effect", "Etc", "Quest", "Morph", "TamingMob", "List"
	};

	private const string MANIFEST_FILENAME = "manifest.json";

	private const string INDEX_FILENAME = "index.json";

	private readonly string _versionPath;

	private readonly HaCreatorConfig _config;

	private readonly VersionInfo _versionInfo;

	private readonly WzMapleVersion _mapleVersion;

	private readonly byte[] _wzIv;

	private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

	private readonly LRUCache<string, WzImage> _imageCache;

	private readonly ConcurrentDictionary<string, VirtualWzDirectory> _directoryCache = new ConcurrentDictionary<string, VirtualWzDirectory>();

	private readonly ConcurrentDictionary<string, List<string>> _categoryIndex = new ConcurrentDictionary<string, List<string>>();

	private const int DEFAULT_MAX_CACHE_ITEMS = 500;

	private const long DEFAULT_MAX_CACHE_BYTES = 536870912L;

	private bool _isInitialized;

	private bool _disposed;

	private int _cacheHits;

	private int _cacheMisses;

	private int _diskReads;

	private FileSystemWatcherService _watcherService;

	private bool _hotSwapEnabled;

	public string VersionPath => _versionPath;

	public VersionInfo VersionInfo => _versionInfo;

	public bool IsInitialized => _isInitialized;

	public WzMapleVersion MapleVersion => _mapleVersion;

	public bool HotSwapEnabled => _hotSwapEnabled;

	public event EventHandler<CategoryIndexChangedEventArgs> CategoryIndexChanged;

	public ImgFileSystemManager(string versionPath, HaCreatorConfig config = null, WzMapleVersion? explicitMapleVersion = null, byte[] explicitCustomIv = null)
	{
		if (!Directory.Exists(versionPath))
		{
			throw new DirectoryNotFoundException("Version directory not found: " + versionPath);
		}
		_versionPath = versionPath;
		_config = config ?? new HaCreatorConfig();
		CacheConfig cache = _config.Cache;
		_imageCache = new LRUCache<string, WzImage>((cache != null && cache.MaxMemoryCacheMB > 0) ? ((long)_config.Cache.MaxMemoryCacheMB * 1024L * 1024) : 536870912, EstimateWzImageSize);
		string path = Path.Combine(versionPath, "manifest.json");
		if (File.Exists(path))
		{
			string value = File.ReadAllText(path);
			_versionInfo = JsonConvert.DeserializeObject<VersionInfo>(value);
			_versionInfo.DirectoryPath = versionPath;
		}
		else
		{
			_versionInfo = new VersionInfo
			{
				Version = Path.GetFileName(versionPath),
				DisplayName = Path.GetFileName(versionPath),
				DirectoryPath = versionPath,
				ExtractedDate = DateTime.Now
			};
		}
		if (explicitMapleVersion.HasValue)
		{
			if (explicitMapleVersion.Value == WzMapleVersion.CUSTOM && explicitCustomIv != null && explicitCustomIv.Length == 4)
			{
				_mapleVersion = WzMapleVersion.CUSTOM;
				_wzIv = explicitCustomIv.ToArray();
			}
			else if (explicitMapleVersion.Value != WzMapleVersion.CUSTOM && explicitMapleVersion.Value != WzMapleVersion.GENERATE)
			{
				_mapleVersion = explicitMapleVersion.Value;
				_wzIv = WzTool.GetIvByMapleVersion(_mapleVersion);
			}
			else
			{
				_mapleVersion = WzMapleVersion.BMS;
				_wzIv = WzTool.GetIvByMapleVersion(_mapleVersion);
			}
		}
		else
		{
			_mapleVersion = WzMapleVersion.BMS;
			_wzIv = WzTool.GetIvByMapleVersion(_mapleVersion);
		}
	}

	public void Initialize()
	{
		if (!_isInitialized)
		{
			BuildCategoryIndex();
			_isInitialized = true;
		}
	}

	public async Task InitializeAsync()
	{
		if (!_isInitialized)
		{
			await Task.Run(() =>
			{
				BuildCategoryIndex();
			});
			_isInitialized = true;
		}
	}

	private void BuildCategoryIndex()
	{
		foreach (string item in Directory.EnumerateDirectories(_versionPath))
		{
			string fileName = Path.GetFileName(item);
			List<string> list = LoadCategoryIndex(fileName, item);
			if (list.Count > 0)
			{
				_categoryIndex[fileName.ToLower()] = list;
				if (!_versionInfo.Categories.ContainsKey(fileName))
				{
					_versionInfo.Categories[fileName] = new CategoryInfo
					{
						FileCount = list.Count,
						LastModified = DateTime.Now
					};
				}
			}
		}
	}

	private void ScanDirectoryForImages(string baseDir, string currentDir, List<string> imageFiles)
	{
		foreach (string item2 in Directory.EnumerateFiles(currentDir, "*.img"))
		{
			string item = item2.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar);
			imageFiles.Add(item);
		}
		foreach (string item3 in Directory.EnumerateDirectories(currentDir))
		{
			ScanDirectoryForImages(baseDir, item3, imageFiles);
		}
	}

	public WzImage LoadImage(string category, string relativePath)
	{
		string key = category.ToLower() + "/" + relativePath.ToLower();
		if (_imageCache.TryGet(key, out var value))
		{
			Interlocked.Increment(ref _cacheHits);
			return value;
		}
		Interlocked.Increment(ref _cacheMisses);
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		if (!File.Exists(text))
		{
			return null;
		}
		WzImage wzImage = LoadImageFromFile(text, Path.GetFileName(relativePath));
		if (wzImage != null)
		{
			_imageCache.Add(key, wzImage);
		}
		return wzImage;
	}

	private WzImage LoadImageFromFile(string filePath, string imageName)
	{
		try
		{
			Interlocked.Increment(ref _diskReads);
			WzImgDeserializer wzImgDeserializer = new WzImgDeserializer(freeResources: true);
			WzImage wzImage = wzImgDeserializer.WzImageFromIMGFile(filePath, _wzIv, imageName, out var successfullyParsedImage);
			if (successfullyParsedImage && wzImage != null)
			{
				return wzImage;
			}
			Debug.WriteLine($"[ImgFileSystemManager] Failed to parse image: {filePath} (success={successfullyParsedImage}, image={wzImage != null})");
			Debug.WriteLine("[ImgFileSystemManager] Using IV: " + BitConverter.ToString(_wzIv));
			return null;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[ImgFileSystemManager] Error loading image " + filePath + ": " + ex.Message);
			Debug.WriteLine("[ImgFileSystemManager] Stack trace: " + ex.StackTrace);
			return null;
		}
	}

	public IEnumerable<WzImage> LoadImagesInCategory(string category)
	{
		EnsureInitialized();
		string categoryLower = category.ToLower();
		if (!_categoryIndex.TryGetValue(categoryLower, out var imagePaths))
		{
			yield break;
		}
		foreach (string relativePath in imagePaths)
		{
			WzImage image = LoadImage(category, relativePath);
			if (image != null)
			{
				yield return image;
			}
		}
	}

	public IEnumerable<WzImage> LoadImagesInDirectory(string category, string subDirectory)
	{
		EnsureInitialized();
		string categoryLower = category.ToLower();
		if (!_categoryIndex.TryGetValue(categoryLower, out var imagePaths))
		{
			yield break;
		}
		string subDirNormalized = subDirectory.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		foreach (string relativePath in imagePaths)
		{
			if (relativePath.StartsWith(subDirNormalized, StringComparison.OrdinalIgnoreCase))
			{
				WzImage image = LoadImage(category, relativePath);
				if (image != null)
				{
					yield return image;
				}
			}
		}
	}

	public IEnumerable<string> GetImageNamesInDirectory(string category, string subDirectory)
	{
		EnsureInitialized();
		string categoryLower = category.ToLower();
		if (!_categoryIndex.TryGetValue(categoryLower, out var imagePaths))
		{
			yield break;
		}
		string subDirNormalized = subDirectory.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		foreach (string relativePath in imagePaths)
		{
			if (relativePath.StartsWith(subDirNormalized, StringComparison.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(relativePath);
				if (fileName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
				{
					yield return fileName.Substring(0, fileName.Length - 4);
				}
				else
				{
					yield return fileName;
				}
			}
		}
	}

	public async Task<List<WzImage>> LoadImagesParallelAsync(string category, int maxParallelism = 4)
	{
		EnsureInitialized();
		string categoryLower = category.ToLower();
		if (!_categoryIndex.TryGetValue(categoryLower, out var imagePaths))
		{
			return new List<WzImage>();
		}
		ConcurrentBag<WzImage> images = new ConcurrentBag<WzImage>();
		await Parallel.ForEachAsync(imagePaths, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxParallelism
		}, async (string path, CancellationToken ct) =>
		{
			WzImage image = await Task.Run(() => LoadImage(category, path), ct);
			if (image != null)
			{
				images.Add(image);
			}
		});
		return images.ToList();
	}

	public VirtualWzDirectory GetDirectory(string category)
	{
		string key = category.ToLower();
		if (_directoryCache.TryGetValue(key, out var value))
		{
			return value;
		}
		string text = Path.Combine(_versionPath, category);
		if (!Directory.Exists(text))
		{
			return null;
		}
		VirtualWzDirectory virtualWzDirectory = new VirtualWzDirectory(this, category, text);
		_directoryCache[key] = virtualWzDirectory;
		return virtualWzDirectory;
	}

	public IEnumerable<string> GetCategories()
	{
		EnsureInitialized();
		return _categoryIndex.Keys;
	}

	public bool CategoryExists(string category)
	{
		EnsureInitialized();
		return _categoryIndex.ContainsKey(category.ToLower());
	}

	public bool ImageExists(string category, string relativePath)
	{
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		return File.Exists(text);
	}

	public string GetImageDiagnostics(string category, string relativePath)
	{
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		string path = Path.Combine(_versionPath, category);
		bool flag = Directory.Exists(path);
		bool value = File.Exists(text);
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("Version path: ");
		handler.AppendFormatted(_versionPath);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("Category: ");
		handler.AppendFormatted(category);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("Relative path: ");
		handler.AppendFormatted(relativePath);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("Full path: ");
		handler.AppendFormatted(text);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(27, 1, stringBuilder2);
		handler.AppendLiteral("Category directory exists: ");
		handler.AppendFormatted(flag);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("File exists: ");
		handler.AppendFormatted(value);
		stringBuilder8.AppendLine(ref handler);
		if (flag)
		{
			List<string> values = Directory.EnumerateFiles(path, "*.img", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Take(10)
				.ToList();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(38, 1, stringBuilder2);
			handler.AppendLiteral("First 10 .img files in category root: ");
			handler.AppendFormatted(string.Join(", ", values));
			stringBuilder9.AppendLine(ref handler);
		}
		return stringBuilder.ToString();
	}

	public IEnumerable<string> GetSubdirectories(string category)
	{
		string categoryPath = Path.Combine(_versionPath, category);
		if (!Directory.Exists(categoryPath))
		{
			return Enumerable.Empty<string>();
		}
		return from d in Directory.EnumerateDirectories(categoryPath, "*", SearchOption.AllDirectories)
			select d.Substring(categoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
	}

	public bool SaveImage(WzImage image, string category, string relativePath)
	{
		if (image == null)
		{
			throw new ArgumentNullException("image");
		}
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		return SaveImageToFile(image, text);
	}

	public bool SaveImageToFile(WzImage image, string filePath)
	{
		if (image == null)
		{
			throw new ArgumentNullException("image");
		}
		try
		{
			string directoryName = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string text = filePath + ".tmp";
			using (FileStream output = File.Open(text, FileMode.Create))
			{
				using WzBinaryWriter writer = new WzBinaryWriter(output, _wzIv);
				image.SaveImage(writer);
			}
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			File.Move(text, filePath);
			string cacheKeyFromPath = GetCacheKeyFromPath(filePath);
			if (cacheKeyFromPath != null)
			{
				_imageCache.Add(cacheKeyFromPath, image);
			}
			image.Changed = false;
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Error saving image " + filePath + ": " + ex.Message);
			return false;
		}
	}

	private string GetCacheKeyFromPath(string filePath)
	{
		if (!filePath.StartsWith(_versionPath, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string text = filePath.Substring(_versionPath.Length).TrimStart(Path.DirectorySeparatorChar);
		string[] array = text.Split(Path.DirectorySeparatorChar, 2);
		if (array.Length < 2)
		{
			return null;
		}
		return array[0].ToLower() + "/" + array[1].ToLower();
	}

	public WzImage CreateImage(string category, string relativePath)
	{
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		if (File.Exists(text))
		{
			return null;
		}
		string directoryName = Path.GetDirectoryName(text);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		string fileName = Path.GetFileName(text);
		WzImage wzImage = new WzImage(fileName);
		wzImage.Changed = true;
		if (SaveImageToFile(wzImage, text))
		{
			string key = category.ToLower();
			if (!_categoryIndex.ContainsKey(key))
			{
				_categoryIndex[key] = new List<string>();
			}
			_categoryIndex[key].Add(relativePath);
			return wzImage;
		}
		return null;
	}

	public bool DeleteImage(string category, string relativePath)
	{
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		try
		{
			if (!File.Exists(text))
			{
				return false;
			}
			File.Delete(text);
			RemoveFromCache(category, relativePath);
			string key = category.ToLower();
			if (_categoryIndex.TryGetValue(key, out var value))
			{
				value.Remove(relativePath);
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Error deleting image " + text + ": " + ex.Message);
			return false;
		}
	}

	public string GetImagePath(string category, string relativePath)
	{
		string text = Path.Combine(_versionPath, category, relativePath);
		if (!text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			text += ".img";
		}
		return text;
	}

	public void PreloadCategory(string category)
	{
		foreach (WzImage item in LoadImagesInCategory(category))
		{
		}
	}

	public async Task PreloadCategoryAsync(string category)
	{
		await LoadImagesParallelAsync(category);
	}

	public void ClearCache()
	{
		_imageCache.Clear();
	}

	public void RemoveFromCache(string category, string relativePath)
	{
		string key = category.ToLower() + "/" + relativePath.ToLower();
		_imageCache.Remove(key);
	}

	public DataSourceStats GetStats()
	{
		CacheStatistics statistics = _imageCache.GetStatistics();
		return new DataSourceStats
		{
			CategoryCount = _categoryIndex.Count,
			ImageCount = _categoryIndex.Values.Sum((List<string> v) => v.Count),
			CachedImageCount = statistics.ItemCount,
			CacheHitCount = (int)statistics.HitCount,
			CacheMissCount = (int)statistics.MissCount,
			DiskReadCount = _diskReads,
			MemoryUsageBytes = statistics.SizeBytes
		};
	}

	public void TrimCache(int maxItems)
	{
		Debug.WriteLine($"TrimCache called - LRU cache auto-manages at {_imageCache.CurrentSizeBytes / 1024 / 1024}MB");
	}

	public IEnumerable<KeyValuePair<string, WzImage>> GetChangedImages()
	{
		IEnumerable<KeyValuePair<string, WzImage>> allItems = _imageCache.GetAllItems();
		return allItems.Where((KeyValuePair<string, WzImage> kvp) => kvp.Value != null && kvp.Value.Changed);
	}

	public int GetChangedImagesCount()
	{
		return _imageCache.GetAllValues().Count((WzImage img) => img?.Changed ?? false);
	}

	public int SaveAllChangedImages()
	{
		int num = 0;
		List<KeyValuePair<string, WzImage>> list = GetChangedImages().ToList();
		foreach (KeyValuePair<string, WzImage> item in list)
		{
			string key = item.Key;
			int num2 = key.IndexOf('/');
			if (num2 > 0)
			{
				string category = key.Substring(0, num2);
				string relativePath = key.Substring(num2 + 1);
				if (SaveImage(item.Value, category, relativePath))
				{
					num++;
				}
			}
		}
		return num;
	}

	public List<(string Category, string RelativePath, string ImageName)> GetChangedImagesInfo()
	{
		List<(string, string, string)> list = new List<(string, string, string)>();
		IEnumerable<KeyValuePair<string, WzImage>> changedImages = GetChangedImages();
		foreach (KeyValuePair<string, WzImage> item3 in changedImages)
		{
			string key = item3.Key;
			int num = key.IndexOf('/');
			if (num > 0)
			{
				string item = key.Substring(0, num);
				string text = key.Substring(num + 1);
				string item2 = item3.Value?.Name ?? Path.GetFileName(text);
				list.Add((item, text, item2));
			}
		}
		return list;
	}

	public long EstimateCacheMemoryUsage()
	{
		return _imageCache.CurrentSizeBytes;
	}

	public void GenerateCategoryIndices()
	{
		EnsureInitialized();
		foreach (string key in _categoryIndex.Keys)
		{
			GenerateCategoryIndex(key);
		}
	}

	public void GenerateCategoryIndex(string category)
	{
		string text = Path.Combine(_versionPath, category);
		if (Directory.Exists(text))
		{
			CategoryIndex categoryIndex = CategoryIndex.BuildFromDirectory(text, category);
			string indexPath = Path.Combine(text, "index.json");
			categoryIndex.Save(indexPath);
		}
	}

	private List<string> LoadCategoryIndex(string category, string categoryPath)
	{
		string indexPath = Path.Combine(categoryPath, "index.json");
		CategoryIndex categoryIndex = CategoryIndex.Load(indexPath);
		if (categoryIndex != null && !categoryIndex.IsStale(categoryPath))
		{
			return categoryIndex.AllImagePaths.ToList();
		}
		List<string> list = new List<string>();
		ScanDirectoryForImages(categoryPath, categoryPath, list);
		return list;
	}

	public bool HasCategoryIndices()
	{
		string[] sTANDARD_CATEGORIES = STANDARD_CATEGORIES;
		foreach (string path in sTANDARD_CATEGORIES)
		{
			string path2 = Path.Combine(_versionPath, path, "index.json");
			if (File.Exists(path2))
			{
				return true;
			}
		}
		return false;
	}

	public async Task PreloadCategoriesAsync(IEnumerable<string> categories, int maxParallelism = 2)
	{
		await Parallel.ForEachAsync(categories, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxParallelism
		}, async (string category, CancellationToken ct) =>
		{
			await LoadImagesParallelAsync(category);
		});
	}

	public async Task PreloadCommonCategoriesAsync()
	{
		if (CategoryExists("String"))
		{
			await LoadImagesParallelAsync("String");
		}
		LoadImage("Map", "MapHelper.img");
		LoadImage("UI", "UIToolTip.img");
	}

	public void EnableHotSwap(bool enable, int debounceMs = 500)
	{
		if (enable && !_hotSwapEnabled)
		{
			InitializeFileWatchers(debounceMs);
			_hotSwapEnabled = true;
		}
		else if (!enable && _hotSwapEnabled)
		{
			DisposeFileWatchers();
			_hotSwapEnabled = false;
		}
	}

	private void InitializeFileWatchers(int debounceMs)
	{
		_watcherService = new FileSystemWatcherService(debounceMs);
		_watcherService.ImgFileChanged += OnImgFileChanged;
		_watcherService.WatcherError += OnWatcherError;
		foreach (string key in _categoryIndex.Keys)
		{
			string path = Path.Combine(_versionPath, key);
			if (Directory.Exists(path))
			{
				_watcherService.WatchPath(path, WatchType.Category, key);
			}
		}
		Debug.WriteLine($"Hot swap enabled for {_categoryIndex.Count} categories");
	}

	private void DisposeFileWatchers()
	{
		if (_watcherService != null)
		{
			_watcherService.ImgFileChanged -= OnImgFileChanged;
			_watcherService.WatcherError -= OnWatcherError;
			_watcherService.Dispose();
			_watcherService = null;
		}
		Debug.WriteLine("Hot swap disabled");
	}

	private void OnImgFileChanged(object sender, ImgFileChangedEventArgs e)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			switch (e.ChangeType)
			{
			case WatcherChangeTypes.Created:
				AddToCategoryIndex(e.Category, e.RelativePath);
				OnCategoryIndexChanged(e.Category, CategoryChangeType.FileAdded, e.RelativePath);
				break;
			case WatcherChangeTypes.Deleted:
				RemoveFromCategoryIndex(e.Category, e.RelativePath);
				InvalidateCache(e.Category, e.RelativePath);
				OnCategoryIndexChanged(e.Category, CategoryChangeType.FileRemoved, e.RelativePath);
				break;
			case WatcherChangeTypes.Changed:
				if (!ExistsInCategoryIndex(e.Category, e.RelativePath))
				{
					AddToCategoryIndex(e.Category, e.RelativePath);
					OnCategoryIndexChanged(e.Category, CategoryChangeType.FileAdded, e.RelativePath);
				}
				else
				{
					InvalidateCache(e.Category, e.RelativePath);
					OnCategoryIndexChanged(e.Category, CategoryChangeType.FileModified, e.RelativePath);
				}
				break;
			case WatcherChangeTypes.Renamed:
				if (!string.IsNullOrEmpty(e.OldPath))
				{
					string fileName = Path.GetFileName(e.OldPath);
					RemoveFromCategoryIndex(e.Category, fileName);
					InvalidateCache(e.Category, fileName);
				}
				AddToCategoryIndex(e.Category, e.RelativePath);
				OnCategoryIndexChanged(e.Category, CategoryChangeType.FileRenamed, e.RelativePath);
				break;
			}
			Debug.WriteLine($"Hot swap: {e.ChangeType} - {e.Category}/{e.RelativePath}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Error handling file change: " + ex.Message);
		}
	}

	private void OnWatcherError(object sender, ErrorEventArgs e)
	{
		Debug.WriteLine("File watcher error: " + e.GetException()?.Message);
	}

	public void AddToCategoryIndex(string category, string relativePath)
	{
		if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(relativePath) || !relativePath.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string key = category.ToLower();
		_cacheLock.EnterWriteLock();
		try
		{
			if (_categoryIndex.TryGetValue(key, out var value))
			{
				if (!value.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
				{
					value.Add(relativePath);
				}
			}
			else
			{
				_categoryIndex[key] = new List<string> { relativePath };
			}
		}
		finally
		{
			_cacheLock.ExitWriteLock();
		}
	}

	public void RemoveFromCategoryIndex(string category, string relativePath)
	{
		if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(relativePath))
		{
			return;
		}
		string key = category.ToLower();
		_cacheLock.EnterWriteLock();
		try
		{
			if (_categoryIndex.TryGetValue(key, out var value))
			{
				string text = value.FirstOrDefault((string f) => f.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
				if (text != null)
				{
					value.Remove(text);
				}
			}
		}
		finally
		{
			_cacheLock.ExitWriteLock();
		}
	}

	public bool ExistsInCategoryIndex(string category, string relativePath)
	{
		if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(relativePath))
		{
			return false;
		}
		string key = category.ToLower();
		_cacheLock.EnterReadLock();
		try
		{
			if (_categoryIndex.TryGetValue(key, out var value))
			{
				return value.Any((string f) => f.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
			}
			return false;
		}
		finally
		{
			_cacheLock.ExitReadLock();
		}
	}

	public void InvalidateCache(string category, string relativePath)
	{
		string key = category.ToLower() + "/" + relativePath.ToLower();
		_imageCache.Remove(key);
		string key2 = category.ToLower();
		if (_directoryCache.TryGetValue(key2, out var value))
		{
			value.Refresh();
		}
	}

	protected void OnCategoryIndexChanged(string category, CategoryChangeType changeType, string relativePath)
	{
		CategoryIndexChanged?.Invoke(this, new CategoryIndexChangedEventArgs(category, changeType, relativePath));
	}

	public void RefreshCategoryIndex(string category = null)
	{
		if (category != null)
		{
			string text = Path.Combine(_versionPath, category);
			if (Directory.Exists(text))
			{
				List<string> list = new List<string>();
				ScanDirectoryForImages(text, text, list);
				_cacheLock.EnterWriteLock();
				try
				{
					_categoryIndex[category.ToLower()] = list;
				}
				finally
				{
					_cacheLock.ExitWriteLock();
				}
				OnCategoryIndexChanged(category, CategoryChangeType.IndexRefreshed, null);
			}
		}
		else
		{
			BuildCategoryIndex();
			OnCategoryIndexChanged(null, CategoryChangeType.IndexRefreshed, null);
		}
	}

	private void EnsureInitialized()
	{
		if (!_isInitialized)
		{
			Initialize();
		}
	}

	private static long EstimateWzImageSize(WzImage image)
	{
		if (image == null)
		{
			return 0L;
		}
		long num = 1024L;
		if (!image.Parsed)
		{
			return num;
		}
		try
		{
			HashSet<WzImageProperty> visited = new HashSet<WzImageProperty>(ReferenceEqualityComparer.Instance);
			if (image.WzProperties != null)
			{
				foreach (WzImageProperty wzProperty in image.WzProperties)
				{
					num += EstimatePropertySize(wzProperty, visited, 0);
				}
			}
		}
		catch
		{
			num = 102400L;
		}
		return num;
	}

	private static long EstimatePropertySize(WzImageProperty prop, HashSet<WzImageProperty> visited, int depth)
	{
		if (prop == null || depth > 50 || !visited.Add(prop))
		{
			return 0L;
		}
		long num = 100L;
		if (!(prop is WzCanvasProperty { PngProperty: var pngProperty }))
		{
			if (!(prop is WzStringProperty wzStringProperty))
			{
				if (prop is WzBinaryProperty wzBinaryProperty)
				{
					num += wzBinaryProperty.Length;
				}
			}
			else
			{
				num += (wzStringProperty.Value?.Length ?? 0) * 2;
			}
		}
		else if (pngProperty != null)
		{
			num += (long)(pngProperty.Width * pngProperty.Height) * 4L;
		}
		if (prop.WzProperties != null)
		{
			foreach (WzImageProperty wzProperty in prop.WzProperties)
			{
				num += EstimatePropertySize(wzProperty, visited, depth + 1);
			}
		}
		return num;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			if (_hotSwapEnabled)
			{
				DisposeFileWatchers();
				_hotSwapEnabled = false;
			}
			_imageCache?.Dispose();
			_directoryCache.Clear();
			_categoryIndex.Clear();
			_cacheLock?.Dispose();
			_disposed = true;
		}
	}
}
