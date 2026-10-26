using System;

namespace MapleLib.WzLib.WzStructure.Data.QuestStructure;

public static class QuestStateTypeExtensions
{
	public static string ToReadableString(this QuestStateType state)
	{
		return state.ToString();
	}

	public static QuestStateType ToEnum(this string name)
	{
		if (Enum.TryParse<QuestStateType>(name, out var result))
		{
			return result;
		}
		return QuestStateType.Not_Started;
	}
}
