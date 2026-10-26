using System;
using System.Collections.Generic;

namespace MapleLib.Img;

public class ExtractionValidationResult
{
	public string VersionPath { get; set; }

	public bool IsValid { get; set; }

	public DateTime CheckedAt { get; set; }

	public int TotalImageCount { get; set; }

	public List<string> Errors { get; set; } = new List<string>();

	public Dictionary<string, int> CategoryImageCounts { get; set; } = new Dictionary<string, int>();
}
