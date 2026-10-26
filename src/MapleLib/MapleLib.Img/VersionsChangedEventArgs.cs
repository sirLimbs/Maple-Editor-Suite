using System;

namespace MapleLib.Img;

public class VersionsChangedEventArgs : EventArgs
{
	public VersionChangeType ChangeType { get; }

	public VersionInfo AffectedVersion { get; }

	public VersionsChangedEventArgs(VersionChangeType changeType, VersionInfo affectedVersion)
	{
		ChangeType = changeType;
		AffectedVersion = affectedVersion;
	}
}
