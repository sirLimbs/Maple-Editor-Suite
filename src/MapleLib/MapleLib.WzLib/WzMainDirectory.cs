namespace MapleLib.WzLib;

public class WzMainDirectory
{
	private readonly WzFile file;

	private readonly WzDirectory directory;

	public WzFile File => file;

	public WzDirectory MainDir => directory;

	public WzMainDirectory(WzFile file)
	{
		this.file = file;
		directory = file.WzDirectory;
	}

	public WzMainDirectory(WzFile file, WzDirectory directory)
	{
		this.file = file;
		this.directory = directory;
	}
}
