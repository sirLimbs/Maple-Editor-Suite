using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public static class MapleJobTypeExtensions
{
	public static IEnumerable<CharacterClassType> GetAllJobTypes()
	{
		return from CharacterClassType j in Enum.GetValues(typeof(CharacterClassType))
			where j != CharacterClassType.NULL
			select j;
	}

	public static List<CharacterClassType> GetMatchingJobs(int jobBitfield)
	{
		List<CharacterClassType> list = new List<CharacterClassType>();
		IEnumerable<CharacterClassType> allJobTypes = GetAllJobTypes();
		foreach (CharacterClassType item in allJobTypes)
		{
			if (IsJobMatching(item, jobBitfield))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool IsJobMatching(CharacterClassType job, int jobBitfield)
	{
		return (jobBitfield & (1 << (int)job)) != 0;
	}
}
