using System;

namespace MapleLib.Img;

public class ExtractionProgressEventArgs : EventArgs
{
	public ExtractionProgress Progress { get; }

	public ExtractionProgressEventArgs(ExtractionProgress progress)
	{
		Progress = progress;
	}
}
