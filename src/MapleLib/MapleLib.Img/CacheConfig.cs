using Newtonsoft.Json;

namespace MapleLib.Img;

public class CacheConfig
{
	[JsonProperty("maxMemoryCacheMB")]
	public int MaxMemoryCacheMB { get; set; } = 512;

	[JsonProperty("preloadCategories")]
	public string[] PreloadCategories { get; set; } = new string[1] { "String" };

	[JsonProperty("enableMemoryMappedFiles")]
	public bool EnableMemoryMappedFiles { get; set; } = true;

	[JsonProperty("maxCachedImages")]
	public int MaxCachedImages { get; set; } = 1000;
}
