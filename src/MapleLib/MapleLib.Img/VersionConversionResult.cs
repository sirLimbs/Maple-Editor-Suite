using System;

namespace MapleLib.Img;

public class VersionConversionResult
{
	public string VersionName { get; set; }

	public bool Success { get; set; }

	public string OutputPath { get; set; }

	public int CategoriesExtracted { get; set; }

	public int ImagesExtracted { get; set; }

	public TimeSpan ElapsedTime { get; set; }

	public string ErrorMessage { get; set; }
}
