using System;
using System.IO;

namespace MapleLib.Img;

public class ImgFileChangedEventArgs : EventArgs
{
	public string FilePath { get; }

	public string Category { get; }

	public string RelativePath { get; }

	public WatcherChangeTypes ChangeType { get; }

	public string OldPath { get; }

	public ImgFileChangedEventArgs(string filePath, string category, string relativePath, WatcherChangeTypes changeType, string oldPath = null)
	{
		FilePath = filePath;
		Category = category;
		RelativePath = relativePath;
		ChangeType = changeType;
		OldPath = oldPath;
	}
}
