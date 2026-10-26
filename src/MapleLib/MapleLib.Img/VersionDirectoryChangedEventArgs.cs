using System;
using System.IO;

namespace MapleLib.Img;

public class VersionDirectoryChangedEventArgs : EventArgs
{
	public string VersionPath { get; }

	public WatcherChangeTypes ChangeType { get; }

	public VersionDirectoryChangedEventArgs(string versionPath, WatcherChangeTypes changeType)
	{
		VersionPath = versionPath;
		ChangeType = changeType;
	}
}
