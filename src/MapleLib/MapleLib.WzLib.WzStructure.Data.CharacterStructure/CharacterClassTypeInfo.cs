using MapleLib.ClientLib;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public class CharacterClassTypeInfo
{
	public static CharacterClassTypeInfo[] JobTypes = new CharacterClassTypeInfo[26]
	{
		new CharacterClassTypeInfo(CharacterClassType.UltimateAdventurer, isCreationEnabled: false, 0, 100000000, 4161001, hairColor: true, skinColor: true, faceMark: false, hat: false, bottom: true, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Resistance, isCreationEnabled: true, 3000, 931000000, 4161054, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Adventurer, isCreationEnabled: true, 0, 4000000, 4161001, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Cygnus, isCreationEnabled: true, 1000, 130030000, 4161047, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Aran, isCreationEnabled: true, 2000, 914000000, 4161048, hairColor: true, skinColor: true, faceMark: false, hat: false, bottom: true, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Evan, isCreationEnabled: true, 2001, 900010000, 4161048, hairColor: true, skinColor: true, faceMark: false, hat: false, bottom: true, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Mercedes, isCreationEnabled: false, 2002, 910150000, 0, hairColor: false, skinColor: false, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Demon, isCreationEnabled: false, 3001, 931050310, 0, hairColor: false, skinColor: false, faceMark: true, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Phantom, isCreationEnabled: false, 2003, 915000000, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.DualBlade, isCreationEnabled: false, 0, 103050900, 4161001, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Mihile, isCreationEnabled: false, 5000, 913070000, 0, hairColor: true, skinColor: true, faceMark: false, hat: false, bottom: true, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Luminous, isCreationEnabled: false, 2004, 931030000, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Kaiser, isCreationEnabled: false, 6000, 940001000, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.AngelicBuster, isCreationEnabled: false, 6001, 940011000, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Cannoneer, isCreationEnabled: true, 0, 3000000, 4161001, hairColor: true, skinColor: true, faceMark: false, hat: false, bottom: true, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Xenon, isCreationEnabled: false, 3002, 931050920, 0, hairColor: true, skinColor: true, faceMark: true, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Zero, isCreationEnabled: false, 10112, 321000000, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Shade, isCreationEnabled: false, 2005, 927030050, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: true, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.ZenOrJett, isCreationEnabled: false, 0, 552000050, 0, hairColor: false, skinColor: false, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.ZenOrJett, isCreationEnabled: false, 0, 552000050, 0, hairColor: false, skinColor: false, faceMark: false, hat: false, bottom: false, cape: true, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Hayato, isCreationEnabled: false, 4001, 807000000, 0, hairColor: true, skinColor: true, faceMark: false, hat: true, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Kanna, isCreationEnabled: false, 4002, 807040000, 0, hairColor: true, skinColor: true, faceMark: false, hat: true, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.BeastTamer, isCreationEnabled: false, 11212, 866000000, 0, hairColor: false, skinColor: true, faceMark: true, hat: false, bottom: false, cape: false, ears: true, tail: true),
		new CharacterClassTypeInfo(CharacterClassType.PinkBean, isCreationEnabled: false, 13100, 866000000, 0, hairColor: false, skinColor: false, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.Kinesis, isCreationEnabled: false, 14000, 331001110, 0, hairColor: false, skinColor: true, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false),
		new CharacterClassTypeInfo(CharacterClassType.NULL, isCreationEnabled: false, 0, 999999999, 0, hairColor: false, skinColor: false, faceMark: false, hat: false, bottom: false, cape: false, ears: false, tail: false)
	};

	public CharacterClassType Type { get; }

	public bool IsCreationEnabled { get; }

	public int JobId { get; }

	public int Map { get; }

	public int StartingETCBookItemId { get; }

	public bool HairColor { get; }

	public bool SkinColor { get; }

	public bool FaceMark { get; }

	public bool Hat { get; }

	public bool Bottom { get; }

	public bool Cape { get; }

	public bool Ears { get; }

	public bool Tail { get; }

	public CharacterClassTypeInfo(CharacterClassType type, bool isCreationEnabled, int jobId, int map, int startingETCBookItemId, bool hairColor, bool skinColor, bool faceMark, bool hat, bool bottom, bool cape, bool ears, bool tail)
	{
		Type = type;
		IsCreationEnabled = isCreationEnabled;
		JobId = jobId;
		Map = map;
		StartingETCBookItemId = startingETCBookItemId;
		HairColor = hairColor;
		SkinColor = skinColor;
		FaceMark = faceMark;
		Hat = hat;
		Bottom = bottom;
		Cape = cape;
		Ears = ears;
		Tail = tail;
	}

	public static CharacterClassTypeInfo GetByJobId(int job, MapleStoryLocalisation msLocalisation)
	{
		if (job == JobTypes[1].JobId)
		{
			return JobTypes[1];
		}
		if (job == 508)
		{
			return msLocalisation switch
			{
				MapleStoryLocalisation.MapleStoryGlobal => JobTypes[17], 
				MapleStoryLocalisation.MapleStorySEA => JobTypes[17], 
				_ => JobTypes[99999], 
			};
		}
		CharacterClassTypeInfo[] jobTypes = JobTypes;
		foreach (CharacterClassTypeInfo characterClassTypeInfo in jobTypes)
		{
			if (characterClassTypeInfo.Type == (CharacterClassType)job)
			{
				return characterClassTypeInfo;
			}
		}
		return JobTypes[99999];
	}

	public static CharacterClassTypeInfo GetByType(int g)
	{
		if (g == 13)
		{
			return JobTypes[1];
		}
		CharacterClassTypeInfo[] jobTypes = JobTypes;
		foreach (CharacterClassTypeInfo characterClassTypeInfo in jobTypes)
		{
			if (characterClassTypeInfo.Type == (CharacterClassType)g)
			{
				return characterClassTypeInfo;
			}
		}
		return null;
	}
}
