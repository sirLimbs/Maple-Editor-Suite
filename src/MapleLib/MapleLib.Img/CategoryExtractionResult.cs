using System;
using System.Collections.Generic;

namespace MapleLib.Img;

public class CategoryExtractionResult
{
	public string CategoryName { get; set; }

	public bool Success { get; set; }

	public int ImagesExtracted { get; set; }

	public long TotalSize { get; set; }

	public int LinksResolved { get; set; }

	public int LinksFailed { get; set; }

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public TimeSpan Duration => EndTime - StartTime;

	public List<string> Errors { get; set; } = new List<string>();

	public short DetectedPatchVersion { get; set; }

	public string DetectedLocale { get; set; }
}
