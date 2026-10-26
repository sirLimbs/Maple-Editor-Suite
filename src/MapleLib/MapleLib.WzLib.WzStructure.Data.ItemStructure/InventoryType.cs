namespace MapleLib.WzLib.WzStructure.Data.ItemStructure;

public enum InventoryType : byte
{
	EQUIP = 1,
	USE = 2,
	SETUP = 3,
	ETC = 4,
	CASH = 5,
	EQUIPPED = byte.MaxValue,
	NONE = 0
}
