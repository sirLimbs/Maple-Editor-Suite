using System;
using System.Collections.Generic;

namespace MapleLib.Img;

public class ValidationReport
{
	public string VersionId { get; set; }

	public string DirectoryPath { get; set; }

	public DateTime CheckedAt { get; set; }

	public bool IsValid { get; set; }

	public List<CategoryValidationResult> Categories { get; set; } = new List<CategoryValidationResult>();
}
