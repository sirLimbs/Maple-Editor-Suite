using System;

namespace MapleLib.Img;

public class ImgFileModifiedEventArgs : EventArgs
{
	public string FilePath { get; }

	public ImgChangeType ChangeType { get; }

	public string OldPath { get; }

	public bool HasLocalChanges { get; set; }

	public ImgFileModifiedEventArgs(string filePath, ImgChangeType changeType, string oldPath = null)
	{
		FilePath = filePath;
		ChangeType = changeType;
		OldPath = oldPath;
	}
}
