using System;

namespace MapleLib.Img;

public class CategoryIndexChangedEventArgs : EventArgs
{
	public string Category { get; }

	public CategoryChangeType ChangeType { get; }

	public string RelativePath { get; }

	public CategoryIndexChangedEventArgs(string category, CategoryChangeType changeType, string relativePath)
	{
		Category = category;
		ChangeType = changeType;
		RelativePath = relativePath;
	}
}
