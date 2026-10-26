namespace MapleLib.WzLib.WzStructure.Data;

public class MapConstants
{
	public const int MinMap = 0;

	public const int MaxMap = 999999999;

	public const int MaxMapLayers = 8;

	public static bool IsZerosTemple(int MapId)
	{
		return MapId / 10000000 == 32;
	}
}
