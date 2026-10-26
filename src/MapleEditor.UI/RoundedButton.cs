using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public class RoundedButton : Button
{
	private bool _hover;

	private bool _pressed;

	public int CornerRadius { get; set; } = 10;

	public int ContentRightPadding { get; set; }

	public RoundedButton()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		FlatStyle = FlatStyle.Flat;
		FlatAppearance.BorderSize = 0;
		UseVisualStyleBackColor = false;
		BackColor = UiTheme.SurfaceRaised;
		ForeColor = UiTheme.Text;
		Cursor = Cursors.Hand;
		Height = 36;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		_hover = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		_hover = false;
		_pressed = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs mevent)
	{
		if (mevent.Button == MouseButtons.Left)
		{
			_pressed = true;
			Invalidate();
		}
		base.OnMouseDown(mevent);
	}

	protected override void OnMouseUp(MouseEventArgs mevent)
	{
		_pressed = false;
		Invalidate();
		base.OnMouseUp(mevent);
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		Invalidate();
		base.OnEnabledChanged(e);
	}

	protected override void OnPaint(PaintEventArgs pevent)
	{
		Graphics graphics = pevent.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		if (Parent != null)
		{
			using SolidBrush parentBrush = new SolidBrush(Parent.BackColor);
			graphics.FillRectangle(parentBrush, ClientRectangle);
		}
		Color baseColor = Enabled ? BackColor : UiTheme.Surface;
		if (baseColor.A == 0 || baseColor == Color.Transparent)
		{
			baseColor = UiTheme.SurfaceRaised;
		}
		if (!Enabled)
		{
			baseColor = UiTheme.Surface;
		}
		Color fill = _pressed ? UiTheme.Darken(baseColor, 0.12) : (_hover && Enabled ? UiTheme.Lighten(baseColor, 0.1) : baseColor);
		Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
		using GraphicsPath path = UiTheme.RoundedRect(bounds, CornerRadius);
		using SolidBrush brush = new SolidBrush(fill);
		graphics.FillPath(brush, path);
		Color border = FlatAppearance.BorderColor;
		if (border == Color.Empty || border == Color.Black)
		{
			border = UiTheme.Border;
		}
		if (!Enabled)
		{
			border = UiTheme.Border;
		}
		using Pen pen = new Pen(border, 1f);
		graphics.DrawPath(pen, path);
		Color textColor = Enabled ? ForeColor : UiTheme.TextMuted;
		Rectangle textBounds = new Rectangle(6, 0, Width - 12 - ContentRightPadding, Height);
		TextRenderer.DrawText(graphics, Text, Font, textBounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}

	protected override void OnKeyDown(KeyEventArgs kevent)
	{
		if (kevent.KeyCode == Keys.Space || kevent.KeyCode == Keys.Enter)
		{
			_pressed = true;
			Invalidate();
		}
		base.OnKeyDown(kevent);
	}

	protected override void OnKeyUp(KeyEventArgs kevent)
	{
		if (kevent.KeyCode == Keys.Space || kevent.KeyCode == Keys.Enter)
		{
			_pressed = false;
			Invalidate();
		}
		base.OnKeyUp(kevent);
	}
}
