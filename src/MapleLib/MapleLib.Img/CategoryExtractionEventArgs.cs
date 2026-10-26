using System;

namespace MapleLib.Img;

public class CategoryExtractionEventArgs : EventArgs
{
	public string Category { get; }

	public CategoryExtractionResult Result { get; }

	public CategoryExtractionEventArgs(string category, CategoryExtractionResult result)
	{
		Category = category;
		Result = result;
	}
}
