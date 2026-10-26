using System;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class ImageIndexEntry
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("path")]
	public string RelativePath { get; set; }

	[JsonProperty("size")]
	public long SizeBytes { get; set; }

	[JsonProperty("modified")]
	public DateTime LastModified { get; set; }
}
