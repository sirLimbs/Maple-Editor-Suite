using System;

namespace MapleLib.WzLib;

public class WzImageResource : IDisposable
{
	private bool parsed;

	private WzImage img;

	public WzImageResource(WzImage img)
	{
		this.img = img;
		parsed = img.Parsed;
		if (!parsed)
		{
			img.ParseImage();
		}
	}

	public void Dispose()
	{
		if (!parsed)
		{
			img.UnparseImage();
		}
	}
}
