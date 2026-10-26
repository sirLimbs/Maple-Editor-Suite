using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace MapleImgEditor;

internal static class IconRewardRenderer
{
	public const float DefaultGlowHue = 61f;

	private const float BoxCenterX = 28f;

	private const float BoxCenterY = 27f;

	private const float BoxInnerSize = 32f;

	private static readonly Lazy<Bitmap?> Template = new Lazy<Bitmap?>(LoadTemplate);

	public static Bitmap Render(Bitmap? inner, float glowHue, out Point innerOffset)
	{
		Bitmap template = Template.Value ?? throw new InvalidOperationException("The default iconReward template could not be loaded.");
		Bitmap result = new Bitmap(template.Width, template.Height, PixelFormat.Format32bppArgb);
		float hueShift = glowHue - DefaultGlowHue;
		for (int y = 0; y < template.Height; y++)
		{
			for (int x = 0; x < template.Width; x++)
			{
				Color pixel = template.GetPixel(x, y);
				bool insideBox = x >= 7 && x <= 48 && y >= 7 && y <= 47;
				result.SetPixel(x, y, insideBox ? pixel : TintPixel(pixel, hueShift));
			}
		}
		innerOffset = Point.Empty;
		if (inner != null && inner.Width > 0 && inner.Height > 0)
		{
			float scale = Math.Min(1f, Math.Min(BoxInnerSize / inner.Width, BoxInnerSize / inner.Height));
			int width = Math.Max(1, (int)Math.Round(inner.Width * scale));
			int height = Math.Max(1, (int)Math.Round(inner.Height * scale));
			int left = (int)Math.Round(BoxCenterX - width / 2f);
			int top = (int)Math.Round(BoxCenterY - height / 2f);
			innerOffset = new Point(left, top);
			using Graphics graphics = Graphics.FromImage(result);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = ((scale < 1f) ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor);
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.DrawImage(inner, new Rectangle(left, top, width, height));
		}
		return result;
	}

	public static Color GetGlowColor(float glowHue)
	{
		return FromHsv(glowHue, 0.53f, 1f, 255);
	}

	private static Color TintPixel(Color pixel, float hueShift)
	{
		if (pixel.A == 0)
		{
			return pixel;
		}
		int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
		int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
		float value = max / 255f;
		float saturation = ((max == 0) ? 0f : ((max - min) / (float)max));
		if (saturation < 0.4f || value < 0.4f)
		{
			return pixel;
		}
		float hue = pixel.GetHue();
		if (hue < 15f || hue > 95f)
		{
			return pixel;
		}
		float targetHue = ((hue + hueShift) % 360f + 360f) % 360f;
		return FromHsv(targetHue, saturation, value, pixel.A);
	}

	private static Color FromHsv(float hue, float saturation, float value, int alpha)
	{
		hue = ((hue % 360f) + 360f) % 360f;
		float chroma = value * saturation;
		float x = chroma * (1f - Math.Abs((hue / 60f) % 2f - 1f));
		float m = value - chroma;
		float r;
		float g;
		float b;
		if (hue < 60f)
		{
			r = chroma;
			g = x;
			b = 0f;
		}
		else if (hue < 120f)
		{
			r = x;
			g = chroma;
			b = 0f;
		}
		else if (hue < 180f)
		{
			r = 0f;
			g = chroma;
			b = x;
		}
		else if (hue < 240f)
		{
			r = 0f;
			g = x;
			b = chroma;
		}
		else if (hue < 300f)
		{
			r = x;
			g = 0f;
			b = chroma;
		}
		else
		{
			r = chroma;
			g = 0f;
			b = x;
		}
		return Color.FromArgb(alpha, (int)Math.Round((r + m) * 255f), (int)Math.Round((g + m) * 255f), (int)Math.Round((b + m) * 255f));
	}

	private static Bitmap? LoadTemplate()
	{
		try
		{
			using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapleImgEditor.Assets.iconRewardTemplate.png");
			if (stream == null)
			{
				return null;
			}
			using Image image = Image.FromStream(stream);
			return new Bitmap(image);
		}
		catch
		{
			return null;
		}
	}
}
