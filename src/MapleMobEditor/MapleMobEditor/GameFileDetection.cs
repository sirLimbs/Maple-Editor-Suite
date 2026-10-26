using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MapleMobEditor;

internal static class GameFileDetection
{
	private static readonly Regex ExtractedMobRegex = new Regex("\\\\Data\\\\Mob\\\\(?<mobId>\\d+)\\.img(?:\\.xml)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex WzMobRegex = new Regex("\\\\Mob(?:\\.wz)?\\\\(?<mobId>\\d+)\\.img(?:\\.xml)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static GameFileInfo Detect(string path)
	{
		string text = path.Replace('/', '\\');
		string text2 = Path.GetFileNameWithoutExtension(text);
		if (text2.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = text2;
			text2 = text3.Substring(0, text3.Length - 4);
		}
		Match match = ExtractedMobRegex.Match(text);
		if (match.Success)
		{
			return new GameFileInfo(IsGameAsset: true, "Mob", string.Empty, match.Groups["mobId"].Value, "Data\\Mob");
		}
		Match match2 = WzMobRegex.Match(text);
		if (match2.Success)
		{
			return new GameFileInfo(IsGameAsset: true, "Mob.wz", string.Empty, match2.Groups["mobId"].Value, "Mob.wz");
		}
		return new GameFileInfo(IsGameAsset: false, string.Empty, string.Empty, text2, "Standalone IMG");
	}
}
