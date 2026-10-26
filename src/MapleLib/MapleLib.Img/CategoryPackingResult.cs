using System;
using System.Collections.Generic;

namespace MapleLib.Img;

public class CategoryPackingResult
{
	public string CategoryName { get; set; }

	public bool Success { get; set; }

	public int ImagesPacked { get; set; }

	public string OutputFilePath { get; set; }

	public long OutputFileSize { get; set; }

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public TimeSpan Duration => EndTime - StartTime;

	public List<string> Errors { get; set; } = new List<string>();
}
