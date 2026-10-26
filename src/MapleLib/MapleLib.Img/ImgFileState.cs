using System;

namespace MapleLib.Img;

internal class ImgFileState
{
	public string FilePath { get; set; }

	public long FileSize { get; set; }

	public DateTime LastWriteTime { get; set; }

	public string ContentHash { get; set; }

	public DateTime RecordedAt { get; set; }
}
