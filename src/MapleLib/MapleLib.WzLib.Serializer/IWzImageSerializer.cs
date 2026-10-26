namespace MapleLib.WzLib.Serializer;

public interface IWzImageSerializer : IWzDirectorySerializer, IWzFileSerializer
{
	void SerializeImage(WzImage img, string path);
}
