using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class HybridDataSource : IDataSource, IDisposable
{
	private readonly ImgFileSystemDataSource _imgSource;

	private readonly WzFileDataSource _wzSource;

	private readonly bool _hasImgSource;

	private readonly bool _hasWzSource;

	private bool _disposed;

	public string Name => _imgSource?.Name ?? _wzSource?.Name ?? "Hybrid";

	public bool IsInitialized => (_hasImgSource && _imgSource.IsInitialized) || (_hasWzSource && _wzSource.IsInitialized);

	public VersionInfo VersionInfo => _imgSource?.VersionInfo ?? _wzSource?.VersionInfo;

	public ImgFileSystemDataSource ImgSource => _imgSource;

	public WzFileDataSource WzSource => _wzSource;

	public HybridDataSource(string path, HaCreatorConfig config = null)
	{
		if (config == null)
		{
			config = new HaCreatorConfig();
		}
		if (File.Exists(Path.Combine(path, "manifest.json")) || Directory.Exists(Path.Combine(path, "String")))
		{
			try
			{
				_imgSource = new ImgFileSystemDataSource(path, config);
				_hasImgSource = true;
			}
			catch
			{
			}
		}
		if (!string.IsNullOrEmpty(config.Legacy.WzFilePath) && Directory.Exists(config.Legacy.WzFilePath))
		{
			try
			{
				_wzSource = new WzFileDataSource(config.Legacy.WzFilePath, config);
				_wzSource.Initialize();
				_hasWzSource = true;
				return;
			}
			catch
			{
				return;
			}
		}
		if (Directory.GetFiles(path, "*.wz").Any())
		{
			try
			{
				_wzSource = new WzFileDataSource(path, config);
				_wzSource.Initialize();
				_hasWzSource = true;
			}
			catch
			{
			}
		}
	}

	public WzImage GetImage(string category, string imageName)
	{
		if (_hasImgSource)
		{
			WzImage image = _imgSource.GetImage(category, imageName);
			if (image != null)
			{
				return image;
			}
		}
		if (_hasWzSource)
		{
			return _wzSource.GetImage(category, imageName);
		}
		return null;
	}

	public WzImage GetImageByPath(string relativePath)
	{
		if (_hasImgSource)
		{
			WzImage imageByPath = _imgSource.GetImageByPath(relativePath);
			if (imageByPath != null)
			{
				return imageByPath;
			}
		}
		if (_hasWzSource)
		{
			return _wzSource.GetImageByPath(relativePath);
		}
		return null;
	}

	public IEnumerable<WzImage> GetImagesInCategory(string category)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			return _imgSource.GetImagesInCategory(category);
		}
		if (_hasWzSource)
		{
			return _wzSource.GetImagesInCategory(category);
		}
		return Enumerable.Empty<WzImage>();
	}

	public IEnumerable<WzImage> GetImagesInDirectory(string category, string subDirectory)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			return _imgSource.GetImagesInDirectory(category, subDirectory);
		}
		if (_hasWzSource)
		{
			return _wzSource.GetImagesInDirectory(category, subDirectory);
		}
		return Enumerable.Empty<WzImage>();
	}

	public IEnumerable<string> GetImageNamesInDirectory(string category, string subDirectory)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			return _imgSource.GetImageNamesInDirectory(category, subDirectory);
		}
		if (_hasWzSource)
		{
			return _wzSource.GetImageNamesInDirectory(category, subDirectory);
		}
		return Enumerable.Empty<string>();
	}

	public bool ImageExists(string category, string imageName)
	{
		if (_hasImgSource && _imgSource.ImageExists(category, imageName))
		{
			return true;
		}
		if (_hasWzSource)
		{
			return _wzSource.ImageExists(category, imageName);
		}
		return false;
	}

	public bool CategoryExists(string category)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			return true;
		}
		if (_hasWzSource)
		{
			return _wzSource.CategoryExists(category);
		}
		return false;
	}

	public IEnumerable<string> GetCategories()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (_hasImgSource)
		{
			foreach (string category in _imgSource.GetCategories())
			{
				hashSet.Add(category);
			}
		}
		if (_hasWzSource)
		{
			foreach (string category2 in _wzSource.GetCategories())
			{
				hashSet.Add(category2);
			}
		}
		return hashSet;
	}

	public IEnumerable<string> GetSubdirectories(string category)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			return _imgSource.GetSubdirectories(category);
		}
		if (_hasWzSource)
		{
			return _wzSource.GetSubdirectories(category);
		}
		return Enumerable.Empty<string>();
	}

	public WzDirectory GetDirectory(string category)
	{
		if (_hasImgSource)
		{
			WzDirectory directory = _imgSource.GetDirectory(category);
			if (directory != null)
			{
				return directory;
			}
		}
		if (_hasWzSource)
		{
			return _wzSource.GetDirectory(category);
		}
		return null;
	}

	public IEnumerable<WzDirectory> GetDirectories(string baseCategory)
	{
		if (_hasImgSource && _imgSource.CategoryExists(baseCategory))
		{
			return _imgSource.GetDirectories(baseCategory);
		}
		if (_hasWzSource)
		{
			return _wzSource.GetDirectories(baseCategory);
		}
		return Enumerable.Empty<WzDirectory>();
	}

	public void PreloadCategory(string category)
	{
		if (_hasImgSource && _imgSource.CategoryExists(category))
		{
			_imgSource.PreloadCategory(category);
		}
		else if (_hasWzSource)
		{
			_wzSource.PreloadCategory(category);
		}
	}

	public void ClearCache()
	{
		_imgSource?.ClearCache();
		_wzSource?.ClearCache();
	}

	public DataSourceStats GetStats()
	{
		DataSourceStats dataSourceStats = new DataSourceStats();
		if (_hasImgSource)
		{
			DataSourceStats stats = _imgSource.GetStats();
			dataSourceStats.CategoryCount += stats.CategoryCount;
			dataSourceStats.ImageCount += stats.ImageCount;
			dataSourceStats.CachedImageCount += stats.CachedImageCount;
			dataSourceStats.CacheHitCount += stats.CacheHitCount;
			dataSourceStats.CacheMissCount += stats.CacheMissCount;
		}
		if (_hasWzSource)
		{
			DataSourceStats stats2 = _wzSource.GetStats();
			dataSourceStats.CategoryCount = Math.Max(dataSourceStats.CategoryCount, stats2.CategoryCount);
			dataSourceStats.ImageCount += stats2.ImageCount;
		}
		return dataSourceStats;
	}

	public bool SaveImage(string category, WzImage image, string relativePath = null)
	{
		if (_hasImgSource)
		{
			return _imgSource.SaveImage(category, image, relativePath);
		}
		if (_hasWzSource)
		{
			return _wzSource.SaveImage(category, image, relativePath);
		}
		return false;
	}

	public void MarkImageUpdated(string category, WzImage image)
	{
		if (_hasImgSource)
		{
			_imgSource.MarkImageUpdated(category, image);
		}
		else if (_hasWzSource)
		{
			_wzSource.MarkImageUpdated(category, image);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_imgSource?.Dispose();
			_wzSource?.Dispose();
			_disposed = true;
		}
	}
}
