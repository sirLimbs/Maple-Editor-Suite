using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class SubdirectoryEntry
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("images")]
	public List<ImageIndexEntry> Images { get; set; } = new List<ImageIndexEntry>();

	[JsonProperty("subdirs")]
	public List<SubdirectoryEntry> Subdirectories { get; set; } = new List<SubdirectoryEntry>();

	[JsonIgnore]
	public int TotalImageCount => Images.Count + Subdirectories.Sum((SubdirectoryEntry s) => s.TotalImageCount);
}
