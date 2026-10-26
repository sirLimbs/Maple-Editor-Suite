using System;

namespace MapleLib.Img;

public class PackingErrorEventArgs : EventArgs
{
	public Exception Exception { get; }

	public PackingErrorEventArgs(Exception ex)
	{
		Exception = ex;
	}
}
