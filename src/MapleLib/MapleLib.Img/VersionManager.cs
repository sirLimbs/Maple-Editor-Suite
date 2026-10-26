#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MapleLib.WzLib;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class VersionManager
{
	private const string MANIFEST_FILENAME = "manifest.json";

	public static readonly string[] REQUIRED_CATEGORIES = new string[2] { "String", "Map" };

	public static readonly string[] STANDARD_CATEGORIES = new string[17]
	{
		"Base", "String", "Map", "Mob", "Npc", "Reactor", "Sound", "Skill", "Character", "Item",
		"UI", "Effect", "Etc", "Quest", "Morph", "TamingMob", "List"
	};

	private readonly string _rootPath;

	private readonly List<VersionInfo> _availableVersions = new List<VersionInfo>();

	private bool _scanned;

	private FileSystemWatcherService _watcherService;

	private bool _hotSwapEnabled;

	private readonly List<string> _additionalWatchPaths = new List<string>();

	public string RootPath => _rootPath;

	public IReadOnlyList<VersionInfo> AvailableVersions
	{
		get
		{
			if (!_scanned)
			{
				ScanVersions();
			}
			return _availableVersions.AsReadOnly();
		}
	}

	public int VersionCount => AvailableVersions.Count;

	public bool HotSwapEnabled => _hotSwapEnabled;

	public event EventHandler<VersionsChangedEventArgs> VersionsChanged;

	public VersionManager(string rootPath)
	{
		_rootPath = rootPath ?? throw new ArgumentNullException("rootPath");
		if (!Directory.Exists(rootPath))
		{
			Directory.CreateDirectory(rootPath);
		}
	}

	public List<VersionInfo> ScanVersions()
	{
		List<VersionInfo> list = _availableVersions.Where((VersionInfo v) => v.IsExternal).ToList();
		_availableVersions.Clear();
		if (Directory.Exists(_rootPath))
		{
			foreach (string item in Directory.EnumerateDirectories(_rootPath))
			{
				VersionInfo versionInfo = LoadVersionManifest(item);
				if (versionInfo != null)
				{
					_availableVersions.Add(versionInfo);
				}
			}
		}
		foreach (VersionInfo externalVersion in list)
		{
			if (Directory.Exists(externalVersion.DirectoryPath) && !_availableVersions.Any((VersionInfo v) => v.DirectoryPath.Equals(externalVersion.DirectoryPath, StringComparison.OrdinalIgnoreCase)))
			{
				_availableVersions.Add(externalVersion);
			}
		}
		_availableVersions.Sort((VersionInfo a, VersionInfo b) => string.Compare(a.Version, b.Version, StringComparison.OrdinalIgnoreCase));
		_scanned = true;
		return _availableVersions;
	}

	public void Refresh()
	{
		_scanned = false;
		ScanVersions();
	}

	public VersionInfo GetVersion(string versionId)
	{
		return AvailableVersions.FirstOrDefault((VersionInfo v) => v.Version.Equals(versionId, StringComparison.OrdinalIgnoreCase));
	}

	public bool VersionExists(string versionId)
	{
		return GetVersion(versionId) != null;
	}

	public VersionInfo LoadVersionManifest(string versionPath)
	{
		string path = Path.Combine(versionPath, "manifest.json");
		VersionInfo versionInfo;
		if (File.Exists(path))
		{
			try
			{
				string value = File.ReadAllText(path);
				versionInfo = JsonConvert.DeserializeObject<VersionInfo>(value);
				versionInfo.DirectoryPath = versionPath;
			}
			catch (Exception)
			{
				versionInfo = CreateBasicVersionInfo(versionPath);
			}
		}
		else
		{
			versionInfo = CreateBasicVersionInfo(versionPath);
		}
		ValidateVersion(versionInfo);
		return versionInfo;
	}

	private VersionInfo CreateBasicVersionInfo(string versionPath)
	{
		string fileName = Path.GetFileName(versionPath);
		VersionInfo versionInfo = new VersionInfo
		{
			Version = fileName,
			DisplayName = fileName,
			DirectoryPath = versionPath,
			ExtractedDate = Directory.GetCreationTime(versionPath),
			Encryption = WzMapleVersion.BMS.ToString()
		};
		foreach (string item in Directory.EnumerateDirectories(versionPath))
		{
			string fileName2 = Path.GetFileName(item);
			int num = Directory.EnumerateFiles(item, "*.img", SearchOption.AllDirectories).Count();
			if (num > 0)
			{
				versionInfo.Categories[fileName2] = new CategoryInfo
				{
					FileCount = num,
					LastModified = Directory.GetLastWriteTime(item)
				};
			}
		}
		return versionInfo;
	}

	public void SaveVersionManifest(VersionInfo versionInfo)
	{
		if (string.IsNullOrEmpty(versionInfo.DirectoryPath))
		{
			throw new ArgumentException("VersionInfo must have a DirectoryPath set");
		}
		string path = Path.Combine(versionInfo.DirectoryPath, "manifest.json");
		string contents = JsonConvert.SerializeObject(versionInfo, Formatting.Indented);
		File.WriteAllText(path, contents);
	}

	public VersionInfo CreateVersionManifest(string versionPath, string versionId, string displayName, WzMapleVersion encryption, bool is64Bit = false, bool isPreBB = false, int patchVersion = 0)
	{
		VersionInfo versionInfo = new VersionInfo
		{
			Version = versionId,
			DisplayName = displayName,
			DirectoryPath = versionPath,
			ExtractedDate = DateTime.Now,
			Encryption = encryption.ToString(),
			Is64Bit = is64Bit,
			IsPreBB = isPreBB,
			PatchVersion = patchVersion
		};
		if (!Directory.Exists(versionPath))
		{
			Directory.CreateDirectory(versionPath);
		}
		SaveVersionManifest(versionInfo);
		return versionInfo;
	}

	public bool ValidateVersion(VersionInfo versionInfo)
	{
		versionInfo.ValidationErrors.Clear();
		versionInfo.IsValid = true;
		if (!Directory.Exists(versionInfo.DirectoryPath))
		{
			versionInfo.ValidationErrors.Add("Directory not found: " + versionInfo.DirectoryPath);
			versionInfo.IsValid = false;
			return false;
		}
		string[] rEQUIRED_CATEGORIES = REQUIRED_CATEGORIES;
		foreach (string text in rEQUIRED_CATEGORIES)
		{
			string path = Path.Combine(versionInfo.DirectoryPath, text);
			if (!Directory.Exists(path))
			{
				versionInfo.ValidationErrors.Add("Required category missing: " + text);
				versionInfo.IsValid = false;
			}
			else if (!Directory.EnumerateFiles(path, "*.img", SearchOption.AllDirectories).Any())
			{
				versionInfo.ValidationErrors.Add("Category '" + text + "' has no .img files");
				versionInfo.IsValid = false;
			}
		}
		string path2 = Path.Combine(versionInfo.DirectoryPath, "String", "Map.img");
		if (!File.Exists(path2))
		{
			versionInfo.ValidationErrors.Add("String/Map.img not found - required for map names");
			versionInfo.IsValid = false;
		}
		return versionInfo.IsValid;
	}

	public bool ValidateVersion(string versionId)
	{
		VersionInfo version = GetVersion(versionId);
		if (version == null)
		{
			return false;
		}
		return ValidateVersion(version);
	}

	public ValidationReport GetValidationReport(VersionInfo versionInfo)
	{
		ValidationReport validationReport = new ValidationReport
		{
			VersionId = versionInfo.Version,
			DirectoryPath = versionInfo.DirectoryPath,
			CheckedAt = DateTime.Now
		};
		string[] sTANDARD_CATEGORIES = STANDARD_CATEGORIES;
		foreach (string text in sTANDARD_CATEGORIES)
		{
			string path = Path.Combine(versionInfo.DirectoryPath, text);
			CategoryValidationResult categoryValidationResult = new CategoryValidationResult
			{
				CategoryName = text,
				IsRequired = REQUIRED_CATEGORIES.Contains(text)
			};
			if (Directory.Exists(path))
			{
				categoryValidationResult.Exists = true;
				categoryValidationResult.FileCount = Directory.EnumerateFiles(path, "*.img", SearchOption.AllDirectories).Count();
				categoryValidationResult.TotalSize = Directory.EnumerateFiles(path, "*.img", SearchOption.AllDirectories).Sum((string f) => new FileInfo(f).Length);
			}
			validationReport.Categories.Add(categoryValidationResult);
		}
		validationReport.IsValid = validationReport.Categories.Where((CategoryValidationResult c) => c.IsRequired).All((CategoryValidationResult c) => c.Exists && c.FileCount > 0);
		return validationReport;
	}

	public VersionInfo AddExternalVersion(string versionPath)
	{
		if (!Directory.Exists(versionPath))
		{
			return null;
		}
		if (_availableVersions.Any((VersionInfo v) => v.DirectoryPath.Equals(versionPath, StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}
		VersionInfo versionInfo = LoadVersionManifest(versionPath);
		if (versionInfo == null)
		{
			return null;
		}
		versionInfo.IsExternal = true;
		_availableVersions.Add(versionInfo);
		_availableVersions.Sort((VersionInfo a, VersionInfo b) => string.Compare(a.Version, b.Version, StringComparison.OrdinalIgnoreCase));
		return versionInfo;
	}

	public bool DeleteVersion(string versionId)
	{
		VersionInfo version = GetVersion(versionId);
		if (version == null)
		{
			return false;
		}
		try
		{
			Directory.Delete(version.DirectoryPath, recursive: true);
			_availableVersions.Remove(version);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool RenameVersion(string oldVersionId, string newVersionId)
	{
		VersionInfo version = GetVersion(oldVersionId);
		if (version == null)
		{
			return false;
		}
		if (VersionExists(newVersionId))
		{
			return false;
		}
		try
		{
			string text = Path.Combine(_rootPath, newVersionId);
			Directory.Move(version.DirectoryPath, text);
			version.Version = newVersionId;
			version.DirectoryPath = text;
			SaveVersionManifest(version);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public ImgFileSystemManager CreateManager(string versionId, HaCreatorConfig config = null)
	{
		VersionInfo version = GetVersion(versionId);
		if (version == null)
		{
			throw new ArgumentException("Version not found: " + versionId);
		}
		return new ImgFileSystemManager(version.DirectoryPath, config);
	}

	public ImgFileSystemManager CreateManager(VersionInfo version, HaCreatorConfig config = null)
	{
		return new ImgFileSystemManager(version.DirectoryPath, config);
	}

	public void EnableHotSwap(bool enable, int debounceMs = 500, IEnumerable<string> additionalPaths = null)
	{
		if (enable && !_hotSwapEnabled)
		{
			InitializeFileWatchers(debounceMs, additionalPaths);
			_hotSwapEnabled = true;
		}
		else if (!enable && _hotSwapEnabled)
		{
			DisposeFileWatchers();
			_hotSwapEnabled = false;
		}
	}

	public void AddWatchPath(string path)
	{
		if (_hotSwapEnabled && _watcherService != null && !string.IsNullOrEmpty(path) && Directory.Exists(path) && !_additionalWatchPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
		{
			_additionalWatchPaths.Add(path);
			_watcherService.WatchPath(path, WatchType.VersionRoot);
		}
	}

	public void RemoveWatchPath(string path)
	{
		if (_hotSwapEnabled && _watcherService != null && !string.IsNullOrEmpty(path))
		{
			_additionalWatchPaths.Remove(path);
			_watcherService.UnwatchPath(path);
		}
	}

	private void InitializeFileWatchers(int debounceMs, IEnumerable<string> additionalPaths)
	{
		_watcherService = new FileSystemWatcherService(debounceMs);
		_watcherService.VersionDirectoryChanged += OnVersionDirectoryChanged;
		_watcherService.WatcherError += OnWatcherError;
		if (Directory.Exists(_rootPath))
		{
			_watcherService.WatchPath(_rootPath, WatchType.VersionRoot);
		}
		if (additionalPaths != null)
		{
			foreach (string additionalPath in additionalPaths)
			{
				if (Directory.Exists(additionalPath))
				{
					_additionalWatchPaths.Add(additionalPath);
					string directoryName = Path.GetDirectoryName(additionalPath);
					if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
					{
						_watcherService.WatchPath(directoryName, WatchType.VersionRoot);
					}
				}
			}
		}
		Debug.WriteLine($"VersionManager hot swap enabled for {_watcherService.WatchedPaths.Count} paths");
	}

	private void DisposeFileWatchers()
	{
		if (_watcherService != null)
		{
			_watcherService.VersionDirectoryChanged -= OnVersionDirectoryChanged;
			_watcherService.WatcherError -= OnWatcherError;
			_watcherService.Dispose();
			_watcherService = null;
		}
		_additionalWatchPaths.Clear();
		Debug.WriteLine("VersionManager hot swap disabled");
	}

	private void OnVersionDirectoryChanged(object sender, VersionDirectoryChangedEventArgs e)
	{
		try
		{
			VersionInfo versionInfo = null;
			switch (e.ChangeType)
			{
			case WatcherChangeTypes.Created:
			{
				if (!Directory.Exists(e.VersionPath))
				{
					break;
				}
				Thread.Sleep(100);
				VersionInfo versionInfo2 = LoadVersionManifest(e.VersionPath);
				if (versionInfo2 != null && versionInfo2.IsValid && !_availableVersions.Any((VersionInfo v) => v.DirectoryPath.Equals(e.VersionPath, StringComparison.OrdinalIgnoreCase)))
				{
					_availableVersions.Add(versionInfo2);
					_availableVersions.Sort((VersionInfo a, VersionInfo b) => string.Compare(a.Version, b.Version, StringComparison.OrdinalIgnoreCase));
					versionInfo = versionInfo2;
					VersionChangeType changeType = VersionChangeType.Added;
					OnVersionsChanged(changeType, versionInfo);
				}
				break;
			}
			case WatcherChangeTypes.Deleted:
				versionInfo = _availableVersions.FirstOrDefault((VersionInfo v) => v.DirectoryPath.Equals(e.VersionPath, StringComparison.OrdinalIgnoreCase));
				if (versionInfo != null)
				{
					_availableVersions.Remove(versionInfo);
					VersionChangeType changeType = VersionChangeType.Removed;
					OnVersionsChanged(changeType, versionInfo);
				}
				break;
			case WatcherChangeTypes.Renamed:
				Refresh();
				OnVersionsChanged(VersionChangeType.Refreshed, null);
				break;
			}
			Debug.WriteLine($"VersionManager hot swap: {e.ChangeType} - {e.VersionPath}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Error handling version directory change: " + ex.Message);
		}
	}

	private void OnWatcherError(object sender, ErrorEventArgs e)
	{
		Debug.WriteLine("VersionManager watcher error: " + e.GetException()?.Message);
	}

	protected void OnVersionsChanged(VersionChangeType changeType, VersionInfo affectedVersion)
	{
		VersionsChanged?.Invoke(this, new VersionsChangedEventArgs(changeType, affectedVersion));
	}
}
