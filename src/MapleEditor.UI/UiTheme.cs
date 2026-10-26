using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MapleEditor.UI;

public static class UiTheme
{
	public static readonly Color AppBackground = Color.FromArgb(11, 14, 20);

	public static readonly Color Surface = Color.FromArgb(20, 25, 34);

	public static readonly Color SurfaceRaised = Color.FromArgb(28, 36, 48);

	public static readonly Color SurfaceHover = Color.FromArgb(38, 49, 64);

	public static readonly Color Border = Color.FromArgb(45, 56, 72);

	public static readonly Color BorderStrong = Color.FromArgb(66, 82, 104);

	public static readonly Color Accent = Color.FromArgb(255, 122, 69);

	public static readonly Color AccentHover = Color.FromArgb(255, 145, 98);

	public static readonly Color AccentPressed = Color.FromArgb(224, 94, 46);

	public static readonly Color AccentSoft = Color.FromArgb(60, 255, 122, 69);

	public static readonly Color Info = Color.FromArgb(110, 168, 254);

	public static readonly Color Text = Color.FromArgb(243, 246, 251);

	public static readonly Color TextSubtle = Color.FromArgb(154, 166, 184);

	public static readonly Color TextMuted = Color.FromArgb(112, 123, 140);

	public static readonly Color Success = Color.FromArgb(61, 220, 151);

	public static readonly Color Danger = Color.FromArgb(255, 92, 122);

	public static readonly Color Warning = Color.FromArgb(255, 190, 92);

	private static readonly Lazy<Image> _banner = new Lazy<Image>(() => LoadImage("header-banner.png"));

	private static readonly Lazy<Image> _logo = new Lazy<Image>(() => LoadImage("logo.png"));

	private static readonly Lazy<Icon> _appIcon = new Lazy<Icon>(LoadAppIcon);

	public static string DisplayFont => "Bahnschrift";

	public static Image Banner => _banner.Value;

	public static Image Logo => _logo.Value;

	public static Icon AppIcon => _appIcon.Value;

	public static Font Font(float size, FontStyle style = FontStyle.Regular)
	{
		return new Font("Bahnschrift", size, style, GraphicsUnit.Point);
	}

	public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
	{
		float num = Math.Max(0f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
		GraphicsPath graphicsPath = new GraphicsPath();
		if (num <= 0.1f)
		{
			graphicsPath.AddRectangle(bounds);
			return graphicsPath;
		}
		float num2 = num * 2f;
		graphicsPath.AddArc(bounds.Left, bounds.Top, num2, num2, 180f, 90f);
		graphicsPath.AddArc(bounds.Right - num2, bounds.Top, num2, num2, 270f, 90f);
		graphicsPath.AddArc(bounds.Right - num2, bounds.Bottom - num2, num2, num2, 0f, 90f);
		graphicsPath.AddArc(bounds.Left, bounds.Bottom - num2, num2, num2, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static void FillRounded(Graphics g, RectangleF bounds, float radius, Color color)
	{
		using GraphicsPath path = RoundedRect(bounds, radius);
		using SolidBrush brush = new SolidBrush(color);
		g.FillPath(brush, path);
	}

	public static void DrawRoundedBorder(Graphics g, RectangleF bounds, float radius, Color color, float width = 1f)
	{
		using GraphicsPath path = RoundedRect(bounds, radius);
		using Pen pen = new Pen(color, width);
		g.DrawPath(pen, path);
	}

	public static Color Blend(Color from, Color to, double amount)
	{
		amount = Math.Max(0.0, Math.Min(1.0, amount));
		return Color.FromArgb(
			(int)(from.A + (to.A - from.A) * amount),
			(int)(from.R + (to.R - from.R) * amount),
			(int)(from.G + (to.G - from.G) * amount),
			(int)(from.B + (to.B - from.B) * amount));
	}

	public static Color Lighten(Color color, double amount)
	{
		return Blend(color, Color.White, amount);
	}

	public static Color Darken(Color color, double amount)
	{
		return Blend(color, Color.Black, amount);
	}

	public static void DrawImageAspectFill(Graphics g, Image image, Rectangle bounds)
	{
		if (image == null || bounds.Width <= 0 || bounds.Height <= 0)
		{
			return;
		}
		float num = (float)bounds.Width / image.Width;
		float num2 = (float)bounds.Height / image.Height;
		float num3 = Math.Max(num, num2);
		int width = (int)Math.Ceiling(image.Width * num3);
		int height = (int)Math.Ceiling(image.Height * num3);
		int x = bounds.X + (bounds.Width - width) / 2;
		int y = bounds.Y + (bounds.Height - height) / 2;
		InterpolationMode interpolationMode = g.InterpolationMode;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.DrawImage(image, new Rectangle(x, y, width, height));
		g.InterpolationMode = interpolationMode;
	}

	public static void EnableDoubleBuffer(Control control)
	{
		if (control == null)
		{
			return;
		}
		try
		{
			typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(control, true);
		}
		catch
		{
		}
	}

	public static void StyleMenu(MenuStrip menu)
	{
		if (menu == null)
		{
			return;
		}
		menu.BackColor = Surface;
		menu.ForeColor = Text;
		menu.Renderer = new ModernMenuRenderer();
		menu.Padding = new Padding(12, 4, 12, 4);
	}

	public static void StyleStatus(StatusStrip status)
	{
		if (status == null)
		{
			return;
		}
		status.BackColor = Surface;
		status.ForeColor = TextSubtle;
		status.SizingGrip = false;
		status.Renderer = new ModernMenuRenderer();
	}

	private static Icon LoadAppIcon()
	{
		try
		{
			Assembly assembly = typeof(UiTheme).Assembly;
			using Stream stream = assembly.GetManifestResourceStream("MapleEditor.UI.Assets.app.ico");
			if (stream != null)
			{
				return new Icon(stream);
			}
		}
		catch
		{
		}
		try
		{
			string processPath = Environment.ProcessPath;
			if (!string.IsNullOrWhiteSpace(processPath))
			{
				Icon icon = Icon.ExtractAssociatedIcon(processPath);
				if (icon != null)
				{
					return icon;
				}
			}
		}
		catch
		{
		}
		return (Icon)SystemIcons.Application.Clone();
	}

	private static Image LoadImage(string fileName)
	{
		try
		{
			Assembly assembly = typeof(UiTheme).Assembly;
			using Stream stream = assembly.GetManifestResourceStream("MapleEditor.UI.Assets." + fileName);
			if (stream != null)
			{
				using Image image = Image.FromStream(stream);
				return new Bitmap(image);
			}
		}
		catch
		{
		}
		return new Bitmap(1, 1);
	}
}
