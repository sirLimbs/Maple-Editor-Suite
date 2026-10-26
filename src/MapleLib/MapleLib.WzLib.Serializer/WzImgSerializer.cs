using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.Serializer;

public class WzImgSerializer : ProgressingWzSerializer, IWzImageSerializer, IWzDirectorySerializer, IWzFileSerializer
{
	private readonly byte[] _outputIv;

	public WzImgSerializer()
		: this(null)
	{
	}

	public WzImgSerializer(byte[] outputIv)
	{
		_outputIv = outputIv;
	}

	public static WzImgSerializer CreateForImgExtraction()
	{
		return new WzImgSerializer(WzAESConstant.WZ_BMSCLASSIC);
	}

	private byte[] GetOutputIv(WzImage img)
	{
		if (_outputIv != null)
		{
			return _outputIv;
		}
		return ((WzDirectory)img.parent).WzIv;
	}

	public byte[] SerializeImage(WzImage img)
	{
		total = 1;
		curr = 0;
		using MemoryStream memoryStream = new MemoryStream();
		using WzBinaryWriter writer = new WzBinaryWriter(memoryStream, GetOutputIv(img));
		img.SaveImage(writer);
		return memoryStream.ToArray();
	}

	public void SerializeImage(WzImage img, string outPath)
	{
		total = 1;
		curr = 0;
		if (Path.GetExtension(outPath) != ".img")
		{
			outPath += ".img";
		}
		using FileStream output = File.Create(outPath);
		using WzBinaryWriter writer = new WzBinaryWriter(output, GetOutputIv(img));
		img.SaveImage(writer);
	}

	public void SerializeDirectory(WzDirectory dir, string outPath)
	{
		total = dir.CountImages();
		curr = 0;
		if (!Directory.Exists(outPath))
		{
			ProgressingWzSerializer.CreateDirSafe(ref outPath);
		}
		if (outPath.Substring(outPath.Length - 1, 1) != "\\")
		{
			outPath += "\\";
		}
		foreach (WzDirectory wzDirectory in dir.WzDirectories)
		{
			SerializeDirectory(wzDirectory, outPath + wzDirectory.Name + "\\");
		}
		foreach (WzImage wzImage in dir.WzImages)
		{
			SerializeImage(wzImage, outPath + wzImage.Name);
		}
	}

	public void SerializeFile(WzFile f, string outPath)
	{
		SerializeDirectory(f.WzDirectory, outPath);
	}
}
