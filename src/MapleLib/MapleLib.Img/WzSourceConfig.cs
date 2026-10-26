using MapleLib.WzLib;

namespace MapleLib.Img;

public class WzSourceConfig
{
	public string SourcePath { get; set; }

	public string VersionName { get; set; }

	public string DisplayName { get; set; }

	public WzMapleVersion Encryption { get; set; } = WzMapleVersion.GMS;

	public bool ExtractAllCategories { get; set; } = true;

	public string[] CategoriesToExtract { get; set; }
}
