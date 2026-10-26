using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class WzFileDataSource : IDataSource, IDisposable
{
	private readonly WzFileManager _wzManager;

	private readonly string _wzPath;

	private readonly HaCreatorConfig _config;

	private bool _disposed;

	private bool _initialized;

	public string Name => Path.GetFileName(_wzPath);

	public bool IsInitialized => _initialized;

	public VersionInfo VersionInfo => null;

	public WzFileManager WzManager => _wzManager;

	public WzFileDataSource(string wzPath, HaCreatorConfig config = null)
	{
		_wzPath = wzPath;
		_config = config ?? new HaCreatorConfig();
		_wzManager = new WzFileManager(wzPath, bIsStandAloneWzFile: false);
	}

	public void Initialize()
	{
		if (!_initialized)
		{
			_wzManager.BuildWzFileList();
			_initialized = true;
		}
	}

	public WzImage GetImage(string category, string imageName)
	{
		WzDirectory directory = GetDirectory(category);
		if (directory == null)
		{
			return null;
		}
		return directory[imageName] as WzImage;
	}

	public WzImage GetImageByPath(string relativePath)
	{
		string[] array = relativePath.Split(new char[2] { '/', '\\' }, 2, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 2)
		{
			return null;
		}
		return GetImage(array[0], array[1]);
	}

	public IEnumerable<WzImage> GetImagesInCategory(string category)
	{
		IEnumerable<WzDirectory> dirs = GetDirectories(category);
		foreach (WzDirectory dir in dirs)
		{
			foreach (WzImage wzImage in dir.WzImages)
			{
				yield return wzImage;
			}
		}
	}

	public IEnumerable<WzImage> GetImagesInDirectory(string category, string subDirectory)
	{
		IEnumerable<WzDirectory> dirs = GetDirectories(category);
		foreach (WzDirectory dir in dirs)
		{
			WzDirectory current = dir;
			string[] parts = subDirectory.Split(new char[2] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array = parts;
			foreach (string part in array)
			{
				current = current[part] as WzDirectory;
				if (current == null)
				{
					break;
				}
			}
			if (current == null)
			{
				continue;
			}
			foreach (WzImage wzImage in current.WzImages)
			{
				yield return wzImage;
			}
		}
	}

	public IEnumerable<string> GetImageNamesInDirectory(string category, string subDirectory)
	{
		IEnumerable<WzDirectory> dirs = GetDirectories(category);
		foreach (WzDirectory dir in dirs)
		{
			WzDirectory current = dir;
			string[] parts = subDirectory.Split(new char[2] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array = parts;
			foreach (string part in array)
			{
				current = current[part] as WzDirectory;
				if (current == null)
				{
					break;
				}
			}
			if (current == null)
			{
				continue;
			}
			foreach (WzImage img in current.WzImages)
			{
				string name = img.Name;
				if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
				{
					name = name.Substring(0, name.Length - 4);
				}
				yield return name;
			}
		}
	}

	public bool ImageExists(string category, string imageName)
	{
		return GetImage(category, imageName) != null;
	}

	public bool CategoryExists(string category)
	{
		return _wzManager[category] != null;
	}

	public IEnumerable<string> GetCategories()
	{
		string[] sTANDARD_CATEGORIES = ImgFileSystemManager.STANDARD_CATEGORIES;
		foreach (string cat in sTANDARD_CATEGORIES)
		{
			if (_wzManager[cat.ToLower()] != null)
			{
				yield return cat;
			}
		}
	}

	public IEnumerable<string> GetSubdirectories(string category)
	{
		WzDirectory directory = GetDirectory(category);
		if (directory == null)
		{
			return Enumerable.Empty<string>();
		}
		return GetSubdirectoriesRecursive(directory, "");
	}

	private IEnumerable<string> GetSubdirectoriesRecursive(WzDirectory dir, string prefix)
	{
		foreach (WzDirectory subDir in dir.WzDirectories)
		{
			string path = (string.IsNullOrEmpty(prefix) ? subDir.Name : (prefix + "/" + subDir.Name));
			yield return path;
			foreach (string item in GetSubdirectoriesRecursive(subDir, path))
			{
				yield return item;
			}
		}
	}

	public WzDirectory GetDirectory(string category)
	{
		return _wzManager[category.ToLower()];
	}

	public IEnumerable<WzDirectory> GetDirectories(string baseCategory)
	{
		return _wzManager.GetWzDirectoriesFromBase(baseCategory.ToLower());
	}

	public void PreloadCategory(string category)
	{
		foreach (WzImage item in GetImagesInCategory(category))
		{
			_ = item.WzProperties;
		}
	}

	public void ClearCache()
	{
	}

	public DataSourceStats GetStats()
	{
		return new DataSourceStats
		{
			CategoryCount = GetCategories().Count(),
			ImageCount = _wzManager.WzFileList.Sum((WzFile wz) => wz.WzDirectory?.CountImages() ?? 0)
		};
	}

	public bool SaveImage(string category, WzImage image, string relativePath = null)
	{
		MarkImageUpdated(category, image);
		return true;
	}

	public void MarkImageUpdated(string category, WzImage image)
	{
		_wzManager.SetWzFileUpdated(category.ToLower(), image);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_wzManager?.Dispose();
			_disposed = true;
		}
	}
}
