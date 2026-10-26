using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;

namespace MapleLib.WzLib.WzProperties;

public static class WzPngFormatExtensions
{
	public static PixelFormat GetPixelFormat(this WzPngFormat format)
	{
		if (1 == 0)
		{
		}
		PixelFormat result = format switch
		{
			WzPngFormat.Format1 => PixelFormat.Format32bppArgb, 
			WzPngFormat.Format2 => PixelFormat.Format32bppArgb, 
			WzPngFormat.Format3 => PixelFormat.Format32bppArgb, 
			WzPngFormat.Format257 => PixelFormat.Format16bppArgb1555, 
			WzPngFormat.Format513 => PixelFormat.Format16bppRgb565, 
			WzPngFormat.Format517 => PixelFormat.Format16bppRgb565, 
			WzPngFormat.Format1026 => PixelFormat.Format32bppArgb, 
			WzPngFormat.Format2050 => PixelFormat.Format32bppArgb, 
			_ => PixelFormat.Format32bppArgb, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static SurfaceFormat GetXNASurfaceFormat(this WzPngFormat format)
	{
		if (1 == 0)
		{
		}
		SurfaceFormat result = format switch
		{
			WzPngFormat.Format1 => SurfaceFormat.Bgra4444, 
			WzPngFormat.Format2 => SurfaceFormat.Bgra32, 
			WzPngFormat.Format3 => SurfaceFormat.Dxt3, 
			WzPngFormat.Format257 => SurfaceFormat.Bgra5551, 
			WzPngFormat.Format513 => SurfaceFormat.Bgr565, 
			WzPngFormat.Format517 => SurfaceFormat.Bgr565, 
			WzPngFormat.Format1026 => SurfaceFormat.Dxt3, 
			WzPngFormat.Format2050 => SurfaceFormat.Dxt5, 
			_ => SurfaceFormat.Bgra32, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static int GetDecodedSize(this WzPngFormat format, int width, int height)
	{
		if (1 == 0)
		{
		}
		int result = format switch
		{
			WzPngFormat.Format1 => width * height * 2, 
			WzPngFormat.Format2 => width * height * 4, 
			WzPngFormat.Format3 => width * height * 4, 
			WzPngFormat.Format257 => width * height * 2, 
			WzPngFormat.Format513 => width * height * 2, 
			WzPngFormat.Format517 => width * height / 128, 
			WzPngFormat.Format1026 => width * height * 4, 
			WzPngFormat.Format2050 => width * height, 
			_ => width * height * 4, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
