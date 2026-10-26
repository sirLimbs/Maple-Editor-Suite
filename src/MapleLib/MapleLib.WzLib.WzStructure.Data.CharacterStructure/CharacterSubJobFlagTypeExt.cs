using System;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public static class CharacterSubJobFlagTypeExt
{
	public static string ToReadableString(this CharacterSubJobFlagType state)
	{
		return state.ToString().Replace("_", " ");
	}

	public static CharacterSubJobFlagType ToEnum(this string name)
	{
		if (Enum.TryParse<CharacterSubJobFlagType>(name.Replace(" ", "_"), out var result))
		{
			return result;
		}
		return CharacterSubJobFlagType.Any;
	}

	public static CharacterSubJobFlagType ToEnum(int value)
	{
		if (Enum.IsDefined(typeof(CharacterSubJobFlagType), value))
		{
			return (CharacterSubJobFlagType)value;
		}
		return CharacterSubJobFlagType.Any;
	}
}
