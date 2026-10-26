using System;

namespace MapleLib.WzLib.WzStructure.Data.QuestStructure;

public static class QuestMedalTypeExt
{
	public static string ToReadableString(this QuestMedalType state)
	{
		return state.ToString().Replace("_", " ");
	}

	public static QuestMedalType ToEnum(this string name)
	{
		if (Enum.TryParse<QuestMedalType>(name.Replace(" ", "_"), out var result))
		{
			return result;
		}
		return QuestMedalType.NoneOrUnknown;
	}

	public static QuestMedalType ToEnum(int value)
	{
		if (Enum.IsDefined(typeof(QuestMedalType), value))
		{
			return (QuestMedalType)value;
		}
		return QuestMedalType.NoneOrUnknown;
	}
}
