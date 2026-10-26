namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public static class PetSkillFlagExtensions
{
	public static int GetValue(this PetSkillFlag flag)
	{
		return 1 << (int)flag;
	}

	public static bool Check(this PetSkillFlag flag, int value)
	{
		return (value & flag.GetValue()) == flag.GetValue();
	}
}
