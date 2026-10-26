using System;
using System.IO;
using MapleLib.WzLib;

namespace MapleLib.Img;

public sealed class ImgFileWzImageReference : WzObject
{
	private readonly VirtualWzDirectory _parentDir;

	private readonly string _fileName;

	public VirtualWzDirectory ParentDirectory => _parentDir;

	public string FileName => _fileName;

	public string RelativePath
	{
		get
		{
			if (string.IsNullOrEmpty(_parentDir.RelativePath))
			{
				return _fileName;
			}
			return Path.Combine(_parentDir.RelativePath, _fileName);
		}
	}

	public override string Name
	{
		get
		{
			return _fileName;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public override WzObjectType ObjectType => WzObjectType.Image;

	public override WzObject Parent
	{
		get
		{
			return _parentDir;
		}
		internal set
		{
		}
	}

	public override WzFile WzFileParent => null;

	public override object WzValue => RelativePath;

	public ImgFileWzImageReference(VirtualWzDirectory parentDir, string fileName)
	{
		_parentDir = parentDir ?? throw new ArgumentNullException("parentDir");
		_fileName = fileName ?? throw new ArgumentNullException("fileName");
	}

	public override void Remove()
	{
		throw new NotSupportedException();
	}

	public override void Dispose()
	{
	}

	public WzImage Resolve()
	{
		WzImage wzImage = _parentDir.Manager.LoadImage(_parentDir.CategoryName, RelativePath);
		if (wzImage != null)
		{
			wzImage.Parent = _parentDir;
		}
		return wzImage;
	}
}
