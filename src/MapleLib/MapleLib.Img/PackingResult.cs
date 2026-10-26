using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleLib.Img;

public class PackingResult
{
	public string VersionPath { get; set; }

	public string OutputPath { get; set; }

	public bool Success { get; set; }

	public string ErrorMessage { get; set; }

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public TimeSpan Duration => EndTime - StartTime;

	public VersionInfo VersionInfo { get; set; }

	public Dictionary<string, CategoryPackingResult> CategoriesPacked { get; set; } = new Dictionary<string, CategoryPackingResult>();

	public int TotalImagesPacked => CategoriesPacked.Values.Sum((CategoryPackingResult c) => c.ImagesPacked);

	public long TotalOutputSize => CategoriesPacked.Values.Sum((CategoryPackingResult c) => c.OutputFileSize);
}
