using System;

namespace MapleLib.Img;

public class CategoryPackingEventArgs : EventArgs
{
	public string Category { get; }

	public CategoryPackingResult Result { get; }

	public CategoryPackingEventArgs(string category, CategoryPackingResult result)
	{
		Category = category;
		Result = result;
	}
}
