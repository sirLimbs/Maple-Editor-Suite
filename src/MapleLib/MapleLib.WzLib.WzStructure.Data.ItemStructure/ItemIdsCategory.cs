namespace MapleLib.WzLib.WzStructure.Data.ItemStructure;

public class ItemIdsCategory
{
	public static int MEDAL_CATEGORY = 114;

	public static int BUFF_CATEGORY = 202;

	public static int PET_CATEGORY = 500;

	public static bool IsBuffItem(int itemId)
	{
		return itemId / 10000 == BUFF_CATEGORY;
	}

	public static bool IsEquipment(int itemId)
	{
		return itemId / 1000000 == 1;
	}
}
