using System;

namespace MapleLib.WzLib.WzStructure.Data;

public static class FieldLimitTypeExtension
{
	public static bool Check(int type, int fieldLimit)
	{
		return Check(type, (long)fieldLimit);
	}

	public static bool Check(int type, long fieldLimit)
	{
		return ((fieldLimit >> type) & 1) != 0;
	}

	public static bool Check(this FieldLimitType type, int fieldLimit)
	{
		return type.Check((long)fieldLimit);
	}

	public static bool Check(this FieldLimitType type, long fieldLimit)
	{
		return ((fieldLimit >> (int)type) & 1) != 0;
	}

	public static int GetMaxFieldLimitType()
	{
		int num = 0;
		foreach (FieldLimitType value in Enum.GetValues(typeof(FieldLimitType)))
		{
			if ((int)value > num)
			{
				num = (int)value;
			}
		}
		return num;
	}
}
