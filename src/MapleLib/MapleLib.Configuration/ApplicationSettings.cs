using MapleLib.WzLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapleLib.Configuration;

public class ApplicationSettings
{
	[JsonProperty(PropertyName = "WindowMaximized")]
	public bool WindowMaximized = false;

	[JsonProperty(PropertyName = "WindowWidth")]
	public int Width = 1024;

	[JsonProperty(PropertyName = "WindowHeight")]
	public int Height = 768;

	[JsonProperty(PropertyName = "FirstRun")]
	public bool FirstRun = true;

	[JsonProperty(PropertyName = "LastBrowserPath")]
	public string LastBrowserPath = "";

	[JsonProperty(PropertyName = "MapleStoryVersion")]
	[JsonConverter(typeof(StringEnumConverter))]
	public WzMapleVersion MapleVersion = WzMapleVersion.BMS;

	[JsonProperty(PropertyName = "MapleStoryVersion_CustomEncryptionName")]
	public string MapleVersion_CustomEncryptionName = "Default";

	[JsonProperty(PropertyName = "MapleStoryVersion_CustomAESUserKey")]
	public string MapleVersion_CustomAESUserKey = string.Empty;

	[JsonProperty(PropertyName = "MapleStoryVersion_EncryptionBytes")]
	public string MapleVersion_CustomEncryptionBytes = "0x00-0x00-0x00-0x00";
}
