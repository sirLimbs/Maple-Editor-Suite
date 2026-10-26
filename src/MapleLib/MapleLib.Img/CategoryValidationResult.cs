namespace MapleLib.Img;

public class CategoryValidationResult
{
	public string CategoryName { get; set; }

	public bool IsRequired { get; set; }

	public bool Exists { get; set; }

	public int FileCount { get; set; }

	public long TotalSize { get; set; }
}
