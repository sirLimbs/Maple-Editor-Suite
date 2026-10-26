using System.IO;
using System.Text.RegularExpressions;

namespace MapleImgEditor;

internal static class GameFileDetection
{
	private static readonly Regex ExtractedCharacterRegex = new Regex("\\\\Data\\\\Character\\\\(?<category>[^\\\\]+)\\\\(?<itemId>\\d+)\\.img$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex WzCharacterRegex = new Regex("\\\\Character(?:\\.wz)?\\\\(?<category>[^\\\\]+)\\\\(?<itemId>\\d+)\\.img$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static GameFileInfo Detect(string path)
	{
		string text = path.Replace('/', '\\');
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
		Match match = ExtractedCharacterRegex.Match(text);
		if (match.Success)
		{
			return new GameFileInfo(IsGameAsset: true, "Character", match.Groups["category"].Value, match.Groups["itemId"].Value, "Data\\Character\\" + match.Groups["category"].Value);
		}
		Match match2 = WzCharacterRegex.Match(text);
		if (match2.Success)
		{
			return new GameFileInfo(IsGameAsset: true, "Character.wz", match2.Groups["category"].Value, match2.Groups["itemId"].Value, "Character.wz\\" + match2.Groups["category"].Value);
		}
		return new GameFileInfo(IsGameAsset: false, string.Empty, string.Empty, fileNameWithoutExtension, "Standalone IMG");
	}
}
