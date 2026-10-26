using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class RoundedPanel : Panel
{
	private int _cornerRadius = 14;

	private Color _borderColor = UiTheme.Border;

	public int CornerRadius
	{
		get => _cornerRadius;
		set
		{
			_cornerRadius = Math.Max(0, value);
			Invalidate();
		}
	}

	public Color BorderColor
	{
		get => _borderColor;
		set
		{
			_borderColor = value;
			Invalidate();
		}
	}

	public bool DrawBorder { get; set; } = true;

	public RoundedPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		BackColor = UiTheme.Surface;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using SolidBrush parentBrush = new SolidBrush(Parent?.BackColor ?? UiTheme.AppBackground);
		graphics.FillRectangle(parentBrush, ClientRectangle);
		Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
		if (bounds.Width <= 0 || bounds.Height <= 0)
		{
			return;
		}
		using GraphicsPath path = UiTheme.RoundedRect(bounds, CornerRadius);
		using SolidBrush brush = new SolidBrush(BackColor);
		graphics.FillPath(brush, path);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (!DrawBorder)
		{
			return;
		}
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
		using GraphicsPath path = UiTheme.RoundedRect(bounds, CornerRadius);
		using Pen pen = new Pen(BorderColor, 1f);
		e.Graphics.DrawPath(pen, path);
	}
}
