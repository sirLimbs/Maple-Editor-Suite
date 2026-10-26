using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class CategoryInfo
{
	[JsonProperty("fileCount")]
	public int FileCount { get; set; }

	[JsonProperty("totalSize")]
	public long TotalSize { get; set; }

	[JsonProperty("lastModified")]
	public DateTime LastModified { get; set; }

	[JsonProperty("subdirectories")]
	public List<string> Subdirectories { get; set; } = new List<string>();
}
