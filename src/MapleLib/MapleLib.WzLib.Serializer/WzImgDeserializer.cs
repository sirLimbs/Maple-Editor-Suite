using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.Serializer;

public class WzImgDeserializer : ProgressingWzSerializer
{
	private readonly bool freeResources;

	public WzImgDeserializer(bool freeResources)
	{
		this.freeResources = freeResources;
	}

	public WzImage WzImageFromIMGBytes(byte[] bytes, WzMapleVersion version, string name, bool freeResources)
	{
		byte[] ivByMapleVersion = WzTool.GetIvByMapleVersion(version);
		MemoryStream input = new MemoryStream(bytes);
		WzBinaryReader wzBinaryReader = new WzBinaryReader(input, ivByMapleVersion, 0L);
		WzImage wzImage = new WzImage(name, wzBinaryReader)
		{
			BlockSize = bytes.Length
		};
		wzImage.CalculateAndSetImageChecksum(bytes);
		wzImage.Offset = 0L;
		if (freeResources)
		{
			wzImage.ParseEverything = true;
			wzImage.ParseImage(forceReadFromData: true);
			wzImage.Changed = true;
			wzBinaryReader.Close();
		}
		return wzImage;
	}

	public WzImage WzImageFromIMGFile(string inPath, byte[] iv, string name, out bool successfullyParsedImage)
	{
		FileStream fileStream = File.OpenRead(inPath);
		WzBinaryReader wzBinaryReader = new WzBinaryReader(fileStream, iv, 0L);
		WzImage wzImage = new WzImage(name, wzBinaryReader)
		{
			BlockSize = (int)fileStream.Length
		};
		byte[] array = new byte[fileStream.Length];
		fileStream.Read(array, 0, (int)fileStream.Length);
		fileStream.Position = 0L;
		wzImage.CalculateAndSetImageChecksum(array);
		wzImage.Offset = 0L;
		if (freeResources)
		{
			wzImage.ParseEverything = true;
			successfullyParsedImage = wzImage.ParseImage(forceReadFromData: true);
			wzImage.Changed = true;
			wzBinaryReader.Close();
		}
		else
		{
			successfullyParsedImage = true;
		}
		return wzImage;
	}
}
