using System;
using System.IO;

namespace MapleLib.Img;

internal class FileChangeInfo
{
	public string Path { get; set; }

	public string OldPath { get; set; }

	public WatcherChangeTypes ChangeType { get; set; }

	public DateTime Timestamp { get; set; }

	public WatchType WatchType { get; set; }

	public string Category { get; set; }

	public string RelativePath { get; set; }
}
