using System;
using System.Collections.Generic;
using System.IO;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class ImgFileSystemDataSource : IDataSource, IDisposable
{
	private readonly ImgFileSystemManager _manager;

	private readonly string _versionPath;

	private bool _disposed;

	public string Name => _manager.VersionInfo?.DisplayName ?? Path.GetFileName(_versionPath);

	public bool IsInitialized => _manager.IsInitialized;

	public VersionInfo VersionInfo => _manager.VersionInfo;

	public ImgFileSystemManager Manager => _manager;

	public bool HotSwapEnabled => _manager.HotSwapEnabled;

	public event EventHandler<CategoryIndexChangedEventArgs> CategoryIndexChanged
	{
		add
		{
			_manager.CategoryIndexChanged += value;
		}
		remove
		{
			_manager.CategoryIndexChanged -= value;
		}
	}

	public ImgFileSystemDataSource(string versionPath, HaCreatorConfig config = null)
	{
		_versionPath = versionPath;
		_manager = new ImgFileSystemManager(versionPath, config);
		_manager.Initialize();
	}

	public WzImage GetImage(string category, string imageName)
	{
		return _manager.LoadImage(category, imageName);
	}

	public WzImage GetImageByPath(string relativePath)
	{
		string[] array = relativePath.Split(new char[2] { '/', '\\' }, 2, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 2)
		{
			return null;
		}
		return _manager.LoadImage(array[0], array[1]);
	}

	public IEnumerable<WzImage> GetImagesInCategory(string category)
	{
		return _manager.LoadImagesInCategory(category);
	}

	public IEnumerable<WzImage> GetImagesInDirectory(string category, string subDirectory)
	{
		return _manager.LoadImagesInDirectory(category, subDirectory);
	}

	public IEnumerable<string> GetImageNamesInDirectory(string category, string subDirectory)
	{
		return _manager.GetImageNamesInDirectory(category, subDirectory);
	}

	public bool ImageExists(string category, string imageName)
	{
		return _manager.ImageExists(category, imageName);
	}

	public string GetImageDiagnostics(string category, string imageName)
	{
		return _manager.GetImageDiagnostics(category, imageName);
	}

	public bool CategoryExists(string category)
	{
		return _manager.CategoryExists(category);
	}

	public IEnumerable<string> GetCategories()
	{
		return _manager.GetCategories();
	}

	public IEnumerable<string> GetSubdirectories(string category)
	{
		return _manager.GetSubdirectories(category);
	}

	public WzDirectory GetDirectory(string category)
	{
		return _manager.GetDirectory(category);
	}

	public IEnumerable<WzDirectory> GetDirectories(string baseCategory)
	{
		VirtualWzDirectory dir = _manager.GetDirectory(baseCategory);
		if (dir != null)
		{
			yield return dir;
		}
	}

	public void PreloadCategory(string category)
	{
		_manager.PreloadCategory(category);
	}

	public void ClearCache()
	{
		_manager.ClearCache();
	}

	public DataSourceStats GetStats()
	{
		return _manager.GetStats();
	}

	public bool SaveImage(string category, WzImage image, string relativePath = null)
	{
		if (image == null)
		{
			return false;
		}
		string relativePath2 = relativePath ?? image.Name;
		return _manager.SaveImage(image, category, relativePath2);
	}

	public void MarkImageUpdated(string category, WzImage image)
	{
		SaveImage(category, image);
	}

	public int GetChangedImagesCount()
	{
		return _manager.GetChangedImagesCount();
	}

	public List<(string Category, string RelativePath, string ImageName)> GetChangedImagesInfo()
	{
		return _manager.GetChangedImagesInfo();
	}

	public int SaveAllChangedImages()
	{
		return _manager.SaveAllChangedImages();
	}

	public void EnableHotSwap(bool enable, int debounceMs = 500)
	{
		_manager.EnableHotSwap(enable, debounceMs);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_manager?.Dispose();
			_disposed = true;
		}
	}
}
