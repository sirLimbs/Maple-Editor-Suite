using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class BrandHeader : Control
{
	private string _title = "Maple Editor";

	private string _subtitle = "WZ Studio";

	private string _versionText = string.Empty;

	private string _rightText = string.Empty;

	private bool _compact;

	public string Title
	{
		get => _title;
		set
		{
			_title = value ?? string.Empty;
			Invalidate();
		}
	}

	public string Subtitle
	{
		get => _subtitle;
		set
		{
			_subtitle = value ?? string.Empty;
			Invalidate();
		}
	}

	public string VersionText
	{
		get => _versionText;
		set
		{
			_versionText = value ?? string.Empty;
			Invalidate();
		}
	}

	public string RightText
	{
		get => _rightText;
		set
		{
			_rightText = value ?? string.Empty;
			Invalidate();
		}
	}

	public bool Compact
	{
		get => _compact;
		set
		{
			_compact = value;
			Height = (value ? 92 : 166);
			Invalidate();
		}
	}

	public bool ShowLogo { get; set; } = true;

	public BrandHeader()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		BackColor = UiTheme.AppBackground;
		Height = 166;
		DoubleBuffered = true;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		Rectangle bounds = ClientRectangle;
		using SolidBrush background = new SolidBrush(UiTheme.AppBackground);
		graphics.FillRectangle(background, bounds);
		UiTheme.DrawImageAspectFill(graphics, UiTheme.Banner, bounds);
		using LinearGradientBrush shade = new LinearGradientBrush(bounds, Color.FromArgb(90, 0, 0, 0), Color.FromArgb(190, 8, 11, 16), 0f);
		graphics.FillRectangle(shade, bounds);
		int logoSize = Compact ? 56 : 92;
		int left = Compact ? 22 : 34;
		int textLeft = left;
		if (ShowLogo)
		{
			Rectangle logoBounds = new Rectangle(left, (Height - logoSize) / 2, logoSize, logoSize);
			InterpolationMode interpolationMode = graphics.InterpolationMode;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(UiTheme.Logo, logoBounds);
			graphics.InterpolationMode = interpolationMode;
			textLeft = logoBounds.Right + (Compact ? 16 : 22);
		}
		int chipWidth = 0;
		if (!string.IsNullOrWhiteSpace(VersionText))
		{
			using Font chipFont = UiTheme.Font(Compact ? 9f : 10f, FontStyle.Bold);
			Size chipText = TextRenderer.MeasureText(VersionText, chipFont);
			chipWidth = chipText.Width + 30;
			int chipHeight = Compact ? 28 : 34;
			Rectangle chip = new Rectangle(Width - chipWidth - (Compact ? 22 : 34), (Height - chipHeight) / 2, chipWidth, chipHeight);
			UiTheme.FillRounded(graphics, chip, chipHeight / 2f, Color.FromArgb(78, UiTheme.Accent));
			UiTheme.DrawRoundedBorder(graphics, chip, chipHeight / 2f, Color.FromArgb(170, UiTheme.Accent));
			TextRenderer.DrawText(graphics, VersionText, chipFont, chip, UiTheme.AccentHover, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
		}
		int rightReserve = chipWidth + (Compact ? 42 : 56);
		int textWidth = Math.Max(0, Width - textLeft - rightReserve);
		using Font titleFont = UiTheme.Font(Compact ? 17f : 27f, FontStyle.Bold);
		using Font subtitleFont = UiTheme.Font(Compact ? 9f : 11f, FontStyle.Regular);
		int titleHeight = TextRenderer.MeasureText("Ag", titleFont).Height;
		int subtitleHeight = TextRenderer.MeasureText("Ag", subtitleFont).Height;
		int totalHeight = titleHeight + 4 + (string.IsNullOrWhiteSpace(Subtitle) ? 0 : subtitleHeight);
		int top = (Height - totalHeight) / 2;
		Rectangle titleBounds = new Rectangle(textLeft, top, textWidth, titleHeight);
		TextRenderer.DrawText(graphics, Title, titleFont, titleBounds, UiTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		if (!string.IsNullOrWhiteSpace(Subtitle))
		{
			Rectangle subtitleBounds = new Rectangle(textLeft, titleBounds.Bottom + 4, textWidth, subtitleHeight);
			TextRenderer.DrawText(graphics, Subtitle, subtitleFont, subtitleBounds, UiTheme.TextSubtle, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		}
		if (!string.IsNullOrWhiteSpace(RightText))
		{
			using Font rightFont = UiTheme.Font(8.5f, FontStyle.Regular);
			Size rightSize = TextRenderer.MeasureText(RightText, rightFont);
			Rectangle rightBounds = new Rectangle(Width - rightSize.Width - 26, Height - rightSize.Height - 10, rightSize.Width, rightSize.Height);
			TextRenderer.DrawText(graphics, RightText, rightFont, rightBounds, UiTheme.TextMuted, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
		}
		using Pen accentPen = new Pen(UiTheme.Accent, 2f);
		graphics.DrawLine(accentPen, left, Height - 2, Math.Min(Width - 26, left + (Compact ? 260 : 420)), Height - 2);
	}
}
