using System;

namespace MapleLib.Img;

public class ExtractionErrorEventArgs : EventArgs
{
	public Exception Exception { get; }

	public ExtractionErrorEventArgs(Exception ex)
	{
		Exception = ex;
	}
}
