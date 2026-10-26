using MapleLib.WzLib.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapleLib.Configuration;

public class UserSettings
{
	public enum UserSettingsThemeColor
	{
		Dark,
		Light
	}

	[JsonProperty(PropertyName = "Indentation")]
	public int Indentation = 0;

	[JsonProperty(PropertyName = "LineBreakType")]
	[JsonConverter(typeof(StringEnumConverter))]
	public LineBreak LineBreakType = LineBreak.None;

	[JsonProperty(PropertyName = "DefaultXmlFolder")]
	public string DefaultXmlFolder = "";

	[JsonProperty(PropertyName = "UseApngIncompatibilityFrame")]
	public bool UseApngIncompatibilityFrame = true;

	[JsonProperty(PropertyName = "AutoAssociate")]
	public bool AutoAssociate = true;

	[JsonProperty(PropertyName = "Sort")]
	public bool Sort = false;

	[JsonProperty(PropertyName = "SuppressWarnings")]
	public bool SuppressWarnings = false;

	[JsonProperty(PropertyName = "ParseImagesInSearch")]
	public bool ParseImagesInSearch = false;

	[JsonProperty(PropertyName = "SearchStringValues")]
	public bool SearchStringValues = true;

	[JsonProperty(PropertyName = "DevImgSequences")]
	public bool DevImgSequences = false;

	[JsonProperty(PropertyName = "CartesianPlane")]
	public bool CartesianPlane = true;

	[JsonProperty(PropertyName = "DelayNextLoop")]
	public int DelayNextLoop = 60;

	[JsonProperty(PropertyName = "PlanePosition")]
	public string PlanePosition = "Center";

	[JsonProperty(PropertyName = "ThemeColor")]
	public int ThemeColor = 1;

	[JsonProperty(PropertyName = "EnableCrossHairDebugInformation")]
	public bool EnableCrossHairDebugInformation = true;

	[JsonProperty(PropertyName = "EnableBorderDebugInformation")]
	public bool EnableBorderDebugInformation = true;

	[JsonProperty(PropertyName = "ImageZoomLevel")]
	public double ImageZoomLevel = 3.0;

	[JsonProperty(PropertyName = "AutoloadRelatedWzFiles")]
	public bool AutoloadRelatedWzFiles = false;
}
