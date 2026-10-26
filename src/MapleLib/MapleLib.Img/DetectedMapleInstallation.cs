using MapleLib.WzLib;

namespace MapleLib.Img;

public class DetectedMapleInstallation
{
	public string Path { get; set; }

	public string FolderName { get; set; }

	public int WzFileCount { get; set; }

	public bool Is64Bit { get; set; }

	public string SuggestedVersionName { get; set; }

	public WzMapleVersion SuggestedEncryption { get; set; }
}
