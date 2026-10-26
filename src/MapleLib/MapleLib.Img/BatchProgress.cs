namespace MapleLib.Img;

public class BatchProgress
{
	public int CurrentVersion { get; set; }

	public int TotalVersions { get; set; }

	public string CurrentVersionName { get; set; }

	public string CurrentCategory { get; set; }

	public double OverallProgress { get; set; }

	public double VersionProgress { get; set; }

	public string Message { get; set; }

	public bool IsComplete { get; set; }
}
