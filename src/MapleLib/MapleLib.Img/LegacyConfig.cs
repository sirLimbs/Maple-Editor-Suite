using Newtonsoft.Json;

namespace MapleLib.Img;

public class LegacyConfig
{
	[JsonProperty("wzFilePath")]
	public string WzFilePath { get; set; }

	[JsonProperty("allowWzFallback")]
	public bool AllowWzFallback { get; set; } = false;

	[JsonProperty("autoConvertOnLoad")]
	public bool AutoConvertOnLoad { get; set; } = false;
}
