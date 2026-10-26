using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class VersionInfo
{
	[JsonProperty("version")]
	public string Version { get; set; }

	[JsonProperty("displayName")]
	public string DisplayName { get; set; }

	[JsonProperty("sourceRegion")]
	public string SourceRegion { get; set; }

	[JsonProperty("extractedDate")]
	public DateTime ExtractedDate { get; set; }

	[JsonProperty("encryption")]
	public string Encryption { get; set; }

	[JsonProperty("is64Bit")]
	public bool Is64Bit { get; set; }

	[JsonProperty("isPreBB")]
	public bool IsPreBB { get; set; }

	[JsonProperty("isBetaMs")]
	public bool IsBetaMs { get; set; }

	[JsonProperty("isBigBang2")]
	public bool IsBigBang2 { get; set; }

	[JsonProperty("patchVersion")]
	public int PatchVersion { get; set; }

	[JsonProperty("categories")]
	public Dictionary<string, CategoryInfo> Categories { get; set; } = new Dictionary<string, CategoryInfo>();

	[JsonProperty("features")]
	public VersionFeatures Features { get; set; } = new VersionFeatures();

	[JsonIgnore]
	public string DirectoryPath { get; set; }

	[JsonIgnore]
	public bool IsValid { get; set; }

	[JsonIgnore]
	public bool IsExternal { get; set; }

	[JsonIgnore]
	public List<string> ValidationErrors { get; set; } = new List<string>();

	[JsonIgnore]
	public int TotalImageCount
	{
		get
		{
			int num = 0;
			foreach (CategoryInfo value in Categories.Values)
			{
				num += value.FileCount;
			}
			return num;
		}
	}
}
