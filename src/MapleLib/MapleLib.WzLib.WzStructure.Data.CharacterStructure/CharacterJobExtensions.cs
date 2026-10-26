using System.Linq;
using System.Text.RegularExpressions;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

public static class CharacterJobExtensions
{
	public static string GetFormattedJobName(this CharacterJob job, bool bRemoveJobProgressionNumber = true)
	{
		string text = job.ToString();
		text = text.Replace("FirePoison", " (Fire/Poison)").Replace("IceLightning", " (Ice/Lightning)").Replace("CrossBowman", "Crossbowman")
			.Replace("CannonShooter", "Cannon Shooter");
		if (text == "Pirate" || text.StartsWith("Cannon"))
		{
			return "Pirate (" + text + ")";
		}
		if (bRemoveJobProgressionNumber)
		{
			text = Regex.Replace(text, "\\d+$", "");
		}
		text = string.Concat(text.Select((char x) => char.IsUpper(x) ? (" " + x) : x.ToString())).Trim();
		if (text.EndsWith(" Beginner"))
		{
			text = text.Replace(" Beginner", "") + " (Beginner)";
		}
		return text;
	}
}
