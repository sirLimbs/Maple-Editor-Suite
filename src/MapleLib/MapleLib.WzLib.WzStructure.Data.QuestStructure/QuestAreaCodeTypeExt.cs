using System;

namespace MapleLib.WzLib.WzStructure.Data.QuestStructure;

public static class QuestAreaCodeTypeExt
{
	public static string ToReadableString(this QuestAreaCodeType state)
	{
		return state.ToString().Replace("_", " ");
	}

	public static QuestAreaCodeType ToEnum(this string name)
	{
		if (Enum.TryParse<QuestAreaCodeType>(name.Replace(" ", "_"), out var result))
		{
			return result;
		}
		return QuestAreaCodeType.Unknown;
	}

	public static QuestAreaCodeType ToEnum(int value)
	{
		if (Enum.IsDefined(typeof(QuestAreaCodeType), value))
		{
			return (QuestAreaCodeType)value;
		}
		return QuestAreaCodeType.Unknown;
	}
}
