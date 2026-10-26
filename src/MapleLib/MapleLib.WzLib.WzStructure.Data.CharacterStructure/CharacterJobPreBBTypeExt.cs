using System.Collections.Generic;
using System.Linq;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public static class CharacterJobPreBBTypeExt
{
	public static IEnumerable<CharacterJob> DecodeJobCodes(this CharacterJobPreBBType encoded)
	{
		List<CharacterJob> list = new List<CharacterJob>();
		if (encoded.HasFlag(CharacterJobPreBBType.Beginner))
		{
			list.Add(CharacterJob.Beginner);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ExplorerWarrior))
		{
			list.Add(CharacterJob.Warrior);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ExplorerMagician))
		{
			list.Add(CharacterJob.Magician);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ExplorerArcher))
		{
			list.Add(CharacterJob.Archer);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ExplorerThief))
		{
			list.Add(CharacterJob.Rogue);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ExplorerPirate))
		{
			list.Add(CharacterJob.Pirate);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.Noblesse))
		{
			list.Add(CharacterJob.Noblesse);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.DawnWarrior))
		{
			list.Add(CharacterJob.DawnWarrior1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.BlazeWizard))
		{
			list.Add(CharacterJob.BlazeWizard1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.WindArcher))
		{
			list.Add(CharacterJob.WindArcher1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.NightWalker))
		{
			list.Add(CharacterJob.NightWalker1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.ThunderBreaker))
		{
			list.Add(CharacterJob.ThunderBreaker1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.Evan))
		{
			list.Add(CharacterJob.EvanBeginner);
			list.Add(CharacterJob.Evan1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.Aran))
		{
			list.Add(CharacterJob.AranBeginner);
			list.Add(CharacterJob.EvanBeginner);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.AranWarrior))
		{
			list.Add(CharacterJob.Aran1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.EvanMagician))
		{
			list.Add(CharacterJob.EvanBeginner);
			list.Add(CharacterJob.Evan1);
		}
		if (encoded.HasFlag(CharacterJobPreBBType.Resistance))
		{
			list.Add(CharacterJob.Citizen);
			list.Add(CharacterJob.BattleMage1);
			list.Add(CharacterJob.WildHunter1);
			list.Add(CharacterJob.Mechanic1);
		}
		return from x in list.Distinct()
			orderby (int)x
			select x;
	}

	public static CharacterJobPreBBType EncodeJobs(IEnumerable<int> jobs)
	{
		CharacterJobPreBBType characterJobPreBBType = CharacterJobPreBBType.None;
		foreach (int job in jobs)
		{
			switch (job)
			{
			case 0:
				characterJobPreBBType |= CharacterJobPreBBType.Beginner;
				break;
			case 100:
				characterJobPreBBType |= CharacterJobPreBBType.ExplorerWarrior;
				break;
			case 200:
				characterJobPreBBType |= CharacterJobPreBBType.ExplorerMagician;
				break;
			case 300:
				characterJobPreBBType |= CharacterJobPreBBType.ExplorerArcher;
				break;
			case 400:
				characterJobPreBBType |= CharacterJobPreBBType.ExplorerThief;
				break;
			case 500:
				characterJobPreBBType |= CharacterJobPreBBType.ExplorerPirate;
				break;
			case 1000:
				characterJobPreBBType |= CharacterJobPreBBType.Noblesse;
				break;
			case 1100:
				characterJobPreBBType |= CharacterJobPreBBType.DawnWarrior;
				break;
			case 1200:
				characterJobPreBBType |= CharacterJobPreBBType.BlazeWizard;
				break;
			case 1300:
				characterJobPreBBType |= CharacterJobPreBBType.WindArcher;
				break;
			case 1400:
				characterJobPreBBType |= CharacterJobPreBBType.NightWalker;
				break;
			case 1500:
				characterJobPreBBType |= CharacterJobPreBBType.ThunderBreaker;
				break;
			case 2000:
				characterJobPreBBType |= CharacterJobPreBBType.Aran;
				break;
			case 2100:
				characterJobPreBBType |= CharacterJobPreBBType.AranWarrior;
				break;
			case 2001:
				characterJobPreBBType |= CharacterJobPreBBType.Evan;
				break;
			case 2200:
				characterJobPreBBType |= CharacterJobPreBBType.EvanMagician;
				break;
			case 3000:
			case 3200:
			case 3300:
			case 3500:
				characterJobPreBBType |= CharacterJobPreBBType.Resistance;
				break;
			}
		}
		return characterJobPreBBType;
	}

	public static string GetFormattedJobName(this CharacterJobPreBBType job)
	{
		string source = job.ToString();
		return string.Concat(source.Select((char x) => char.IsUpper(x) ? (" " + x) : x.ToString())).Trim();
	}

	public static bool IsExplorer(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllExplorers) != 0;
	}

	public static bool IsCygnus(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllCygnus) != 0;
	}

	public static bool IsHero(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllHeroes) != 0;
	}

	public static bool IsWarrior(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllWarriors) != 0;
	}

	public static bool IsMagician(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllMagicians) != 0;
	}

	public static bool IsArcher(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllArchers) != 0;
	}

	public static bool IsThief(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllThieves) != 0;
	}

	public static bool IsPirate(this CharacterJobPreBBType codes)
	{
		return (codes & CharacterJobPreBBType.AllPirates) != 0;
	}

	public static CharacterJobPreBBType ToJobCodes(this int encoded)
	{
		return (CharacterJobPreBBType)encoded;
	}
}
