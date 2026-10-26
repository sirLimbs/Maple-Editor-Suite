using System;
using System.Linq;

namespace MapleLib.WzLib.WzStructure.Data.ItemStructure;

public static class InventoryTypeExtensions
{
	public static short GetBitfieldEncoding(this InventoryType inventoryType)
	{
		return (short)(2 << (int)inventoryType);
	}

	public static InventoryType? GetByType(byte type)
	{
		return Enum.GetValues<InventoryType>().FirstOrDefault((InventoryType t) => (uint)t == type);
	}

	public static InventoryType? GetByWZName(string name)
	{
		if (1 == 0)
		{
		}
		InventoryType? result = name switch
		{
			"Install" => (InventoryType?)InventoryType.SETUP, 
			"Consume" => InventoryType.USE, 
			"Etc" => InventoryType.ETC, 
			"Cash" => InventoryType.CASH, 
			"Pet" => InventoryType.CASH, 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
