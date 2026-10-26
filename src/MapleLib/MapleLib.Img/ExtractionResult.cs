using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleLib.Img;

public class ExtractionResult
{
	public bool Success { get; set; }

	public string VersionId { get; set; }

	public string OutputPath { get; set; }

	public string ErrorMessage { get; set; }

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public TimeSpan Duration => EndTime - StartTime;

	public VersionInfo VersionInfo { get; set; }

	public ExtractionValidationResult ValidationResult { get; set; }

	public Dictionary<string, CategoryExtractionResult> CategoriesExtracted { get; set; } = new Dictionary<string, CategoryExtractionResult>();

	public int TotalImagesExtracted => CategoriesExtracted.Values.Sum((CategoryExtractionResult c) => c.ImagesExtracted);

	public long TotalSize => CategoriesExtracted.Values.Sum((CategoryExtractionResult c) => c.TotalSize);

	public int TotalLinksResolved => CategoriesExtracted.Values.Sum((CategoryExtractionResult c) => c.LinksResolved);

	public int TotalLinksFailed => CategoriesExtracted.Values.Sum((CategoryExtractionResult c) => c.LinksFailed);
}
