using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class VirtualWzDirectory : WzDirectory
{
	private readonly ImgFileSystemManager _manager;

	private readonly string _categoryName;

	private readonly string _filesystemPath;

	private readonly string _relativePath;

	private List<WzImage> _images;

	private List<VirtualWzDirectory> _subDirectories;

	private bool _subDirsPopulated;

	private bool _imagesPopulated;

	public string FilesystemPath => _filesystemPath;

	public string CategoryName => _categoryName;

	public string RelativePath => _relativePath;

	public override WzFile WzFileParent => null;

	public override List<WzImage> WzImages
	{
		get
		{
			EnsureImagesPopulated();
			return _images;
		}
	}

	public override List<WzDirectory> WzDirectories
	{
		get
		{
			EnsureSubdirectoriesPopulated();
			return _subDirectories.Cast<WzDirectory>().ToList();
		}
	}

	public override WzObject this[string name]
	{
		get
		{
			EnsureSubdirectoriesPopulated();
			EnsureImagesPopulated();
			string text = name.ToLower();
			foreach (WzImage image in _images)
			{
				if (image.Name.ToLower() == text)
				{
					return image;
				}
			}
			foreach (VirtualWzDirectory subDirectory in _subDirectories)
			{
				if (subDirectory.Name.ToLower() == text)
				{
					return subDirectory;
				}
			}
			return null;
		}
	}

	public ImgFileSystemManager Manager => _manager;

	internal VirtualWzDirectory(ImgFileSystemManager manager, string categoryName, string filesystemPath)
		: base(categoryName)
	{
		_manager = manager ?? throw new ArgumentNullException("manager");
		_categoryName = categoryName;
		_filesystemPath = filesystemPath;
		_relativePath = string.Empty;
	}

	internal VirtualWzDirectory(ImgFileSystemManager manager, string categoryName, string filesystemPath, string relativePath, VirtualWzDirectory parent)
		: base(Path.GetFileName(filesystemPath))
	{
		_manager = manager;
		_categoryName = categoryName;
		_filesystemPath = filesystemPath;
		_relativePath = relativePath;
		base.parent = parent;
	}

	private void EnsureSubdirectoriesPopulated()
	{
		if (!_subDirsPopulated)
		{
			PopulateSubdirectories();
			_subDirsPopulated = true;
		}
	}

	private void EnsureImagesPopulated()
	{
		if (!_imagesPopulated)
		{
			PopulateImages();
			_imagesPopulated = true;
		}
	}

	private void PopulateSubdirectories()
	{
		_subDirectories = new List<VirtualWzDirectory>();
		if (!Directory.Exists(_filesystemPath))
		{
			return;
		}
		foreach (string item2 in Directory.EnumerateDirectories(_filesystemPath))
		{
			string fileName = Path.GetFileName(item2);
			string relativePath = (string.IsNullOrEmpty(_relativePath) ? fileName : Path.Combine(_relativePath, fileName));
			VirtualWzDirectory item = new VirtualWzDirectory(_manager, _categoryName, item2, relativePath, this);
			_subDirectories.Add(item);
		}
	}

	private void PopulateImages()
	{
		_images = new List<WzImage>();
		if (!Directory.Exists(_filesystemPath))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(_filesystemPath, "*.img"))
		{
			string fileName = Path.GetFileName(item);
			string relativePath = (string.IsNullOrEmpty(_relativePath) ? fileName : Path.Combine(_relativePath, fileName));
			WzImage wzImage = _manager.LoadImage(_categoryName, relativePath);
			if (wzImage != null)
			{
				wzImage.Parent = this;
				_images.Add(wzImage);
			}
		}
	}

	public void Refresh()
	{
		_subDirsPopulated = false;
		_imagesPopulated = false;
		_images?.Clear();
		_subDirectories?.Clear();
	}

	public bool RemoveImage(string imageName)
	{
		if (_images == null || string.IsNullOrEmpty(imageName))
		{
			return false;
		}
		WzImage wzImage = _images.FirstOrDefault((WzImage img) => img.Name?.Equals(imageName, StringComparison.OrdinalIgnoreCase) ?? false);
		if (wzImage != null)
		{
			_images.Remove(wzImage);
			wzImage.Dispose();
			return true;
		}
		return false;
	}

	public bool RemoveImageByPath(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return false;
		}
		string fileName = Path.GetFileName(filePath);
		return RemoveImage(fileName);
	}

	public override WzImage GetImageByName(string name)
	{
		EnsureImagesPopulated();
		string nameLower = name.ToLower();
		return _images.FirstOrDefault((WzImage img) => img.Name.ToLower() == nameLower);
	}

	public override WzDirectory GetDirectoryByName(string name)
	{
		EnsureSubdirectoriesPopulated();
		string nameLower = name.ToLower();
		return _subDirectories.FirstOrDefault((VirtualWzDirectory dir) => dir.Name.ToLower() == nameLower);
	}

	public override int CountImages()
	{
		EnsureSubdirectoriesPopulated();
		EnsureImagesPopulated();
		int num = _images.Count;
		foreach (VirtualWzDirectory subDirectory in _subDirectories)
		{
			num += subDirectory.CountImages();
		}
		return num;
	}

	public IEnumerable<WzImage> GetAllImagesRecursive()
	{
		EnsureSubdirectoriesPopulated();
		EnsureImagesPopulated();
		foreach (WzImage image in _images)
		{
			yield return image;
		}
		foreach (VirtualWzDirectory subDir in _subDirectories)
		{
			foreach (WzImage item in subDir.GetAllImagesRecursive())
			{
				yield return item;
			}
		}
	}

	public WzImage FindImage(string relativePath)
	{
		string[] array = relativePath.Split(new char[2] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return null;
		}
		if (array.Length == 1)
		{
			return GetImageByName(array[0]);
		}
		VirtualWzDirectory virtualWzDirectory = this;
		for (int i = 0; i < array.Length - 1; i++)
		{
			if (!(virtualWzDirectory.GetDirectoryByName(array[i]) is VirtualWzDirectory virtualWzDirectory2))
			{
				return null;
			}
			virtualWzDirectory = virtualWzDirectory2;
		}
		return virtualWzDirectory.GetImageByName(array[^1]);
	}

	public string GetImageFilePath(string imageName)
	{
		if (!imageName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			imageName += ".img";
		}
		return Path.Combine(_filesystemPath, imageName);
	}

	public bool ImageExists(string imageName)
	{
		return File.Exists(GetImageFilePath(imageName));
	}

	public IEnumerable<string> GetSubdirectoryNames()
	{
		if (!Directory.Exists(_filesystemPath))
		{
			return Enumerable.Empty<string>();
		}
		return Directory.EnumerateDirectories(_filesystemPath).Select(Path.GetFileName);
	}

	public IEnumerable<string> GetImageNames()
	{
		if (!Directory.Exists(_filesystemPath))
		{
			return Enumerable.Empty<string>();
		}
		return Directory.EnumerateFiles(_filesystemPath, "*.img").Select(Path.GetFileName);
	}

	public bool SaveImage(WzImage image)
	{
		if (image == null || string.IsNullOrEmpty(image.Name))
		{
			return false;
		}
		string filePath = Path.Combine(_filesystemPath, image.Name);
		return _manager.SaveImageToFile(image, filePath);
	}

	public int SaveAllChangedImages()
	{
		EnsureSubdirectoriesPopulated();
		EnsureImagesPopulated();
		int num = 0;
		foreach (WzImage image in _images)
		{
			if (image.Changed && SaveImage(image))
			{
				num++;
			}
		}
		foreach (VirtualWzDirectory subDirectory in _subDirectories)
		{
			num += subDirectory.SaveAllChangedImages();
		}
		return num;
	}

	public override void Dispose()
	{
		_images?.Clear();
		_subDirectories?.Clear();
		_images = null;
		_subDirectories = null;
		_subDirsPopulated = false;
		_imagesPopulated = false;
	}
}
