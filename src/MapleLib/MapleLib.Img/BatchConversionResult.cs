using System;
using System.Collections.Generic;

namespace MapleLib.Img;

public class BatchConversionResult
{
	public bool Success { get; set; }

	public int SuccessCount { get; set; }

	public int FailureCount { get; set; }

	public TimeSpan ElapsedTime { get; set; }

	public List<VersionConversionResult> VersionResults { get; set; } = new List<VersionConversionResult>();

	public List<string> Errors { get; set; } = new List<string>();
}
