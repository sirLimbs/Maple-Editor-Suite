namespace MapleLib.WzLib.Serializer;

public interface IWzDirectorySerializer : IWzFileSerializer
{
	void SerializeDirectory(WzDirectory dir, string path);
}
