namespace MapleLib.WzLib;

public static class WzFileParseStatusExtension
{
	public static string GetErrorDescription(this WzFileParseStatus status)
	{
		return status switch
		{
			WzFileParseStatus.Success => "Success", 
			WzFileParseStatus.Failed_Unknown => "Failed, in this case the causes are undetermined.", 
			WzFileParseStatus.Path_Is_Null => "Path is null", 
			WzFileParseStatus.Error_Game_Ver_Hash => "Error with game version hash : The specified game version is incorrect and WzLib was unable to determine the version itself", 
			_ => string.Empty, 
		};
	}
}
