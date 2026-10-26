using System.Collections.Generic;

namespace MapleLib.Img;

public class PackingProgress
{
	public string CurrentPhase { get; set; }

	public string CurrentFile { get; set; }

	public int TotalFiles { get; set; }

	public int ProcessedFiles { get; set; }

	public double ProgressPercentage => (TotalFiles > 0) ? ((double)ProcessedFiles / (double)TotalFiles * 100.0) : 0.0;

	public List<string> Errors { get; set; } = new List<string>();

	public bool IsCancelled { get; set; }
}
