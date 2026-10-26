namespace MapleLib.WzLib.WzStructure.Data;

public static class BackgroundTypeExtensions
{
	public static string GetDescription(this BackgroundType type)
	{
		if (1 == 0)
		{
		}
		string result = type switch
		{
			BackgroundType.Regular => "Static background, no tiling or movement. Single image at fixed position.", 
			BackgroundType.HorizontalTiling => "Tiles horizontally across the screen. Use cx to set tile interval.", 
			BackgroundType.VerticalTiling => "Tiles vertically down the screen. Use cy to set tile interval.", 
			BackgroundType.HVTiling => "Tiles both horizontally and vertically. Creates a repeating pattern.", 
			BackgroundType.HorizontalMoving => "Scrolls horizontally in a loop. Creates animated sky/cloud effects.", 
			BackgroundType.VerticalMoving => "Scrolls vertically in a loop. Creates rain/snow falling effects.", 
			BackgroundType.HorizontalMovingHVTiling => "Scrolls horizontally while tiling in both directions.", 
			BackgroundType.VerticalMovingHVTiling => "Scrolls vertically while tiling in both directions.", 
			_ => "Unknown background type.", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetFriendlyName(this BackgroundType type)
	{
		if (1 == 0)
		{
		}
		string result = type switch
		{
			BackgroundType.Regular => "Regular", 
			BackgroundType.HorizontalTiling => "Horizontal Copies", 
			BackgroundType.VerticalTiling => "Vertical Copies", 
			BackgroundType.HVTiling => "H+V Copies", 
			BackgroundType.HorizontalMoving => "Horizontal Moving+Copies", 
			BackgroundType.VerticalMoving => "Vertical Moving+Copies", 
			BackgroundType.HorizontalMovingHVTiling => "H+V Copies, Horizontal Moving", 
			BackgroundType.VerticalMovingHVTiling => "H+V Copies, Vertical Moving", 
			_ => "Unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
