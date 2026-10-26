using System;

namespace MapleLib.Img;

public class PackingProgressEventArgs : EventArgs
{
	public PackingProgress Progress { get; }

	public PackingProgressEventArgs(PackingProgress progress)
	{
		Progress = progress;
	}
}
