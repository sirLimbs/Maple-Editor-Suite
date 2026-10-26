using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class HueSlider : Control
{
	private int _value = 61;

	private bool _dragging;

	public event EventHandler ValueChanged;

	public int Minimum { get; set; }

	public int Maximum { get; set; } = 360;

	public int TrackHeight { get; set; } = 12;

	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			int num = Math.Max(Minimum, Math.Min(Maximum, value));
			if (num != _value)
			{
				_value = num;
				Invalidate();
				this.ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public HueSlider()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
		Height = 40;
		Cursor = Cursors.Hand;
		TabStop = true;
		BackColor = UiTheme.SurfaceRaised;
	}

	private Rectangle TrackBounds
	{
		get
		{
			return new Rectangle(10, (Height - TrackHeight) / 2, Math.Max(1, Width - 20), TrackHeight);
		}
	}

	private float Fraction
	{
		get
		{
			return (Value - Minimum) / (float)Math.Max(1, Maximum - Minimum);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.Clear(BackColor);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle track = TrackBounds;
		using GraphicsPath path = UiTheme.RoundedRect(track, TrackHeight / 2f);
		using LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(track.X, track.Y, track.Width, track.Height), Color.Red, Color.Red, 0f);
		brush.InterpolationColors = new ColorBlend
		{
			Colors = new Color[7]
			{
				Color.FromArgb(255, 255, 0, 0),
				Color.FromArgb(255, 255, 255, 0),
				Color.FromArgb(255, 0, 255, 0),
				Color.FromArgb(255, 0, 255, 255),
				Color.FromArgb(255, 0, 0, 255),
				Color.FromArgb(255, 255, 0, 255),
				Color.FromArgb(255, 255, 0, 0)
			},
			Positions = new float[7] { 0f, 0.16666667f, 0.33333334f, 0.5f, 0.6666667f, 0.8333333f, 1f }
		};
		graphics.FillPath(brush, path);
		using Pen border = new Pen(UiTheme.Border, 1f);
		graphics.DrawPath(border, path);
		int num = track.X + (int)Math.Round(Fraction * track.Width);
		int radius = 8;
		int x = Math.Max(track.X - radius, Math.Min(track.Right - radius, num - radius));
		Rectangle thumbBounds = new Rectangle(x, Height / 2 - radius, radius * 2, radius * 2);
		using SolidBrush fill = new SolidBrush(GetHueColor(Fraction));
		graphics.FillEllipse(fill, thumbBounds);
		using Pen thumbBorder = new Pen(Color.White, 2f);
		graphics.DrawEllipse(thumbBorder, thumbBounds);
		if (Focused)
		{
			Rectangle focus = thumbBounds;
			focus.Inflate(3, 3);
			using Pen focusPen = new Pen(Color.FromArgb(180, UiTheme.Accent), 1f);
			graphics.DrawEllipse(focusPen, focus);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_dragging = true;
			Focus();
			SetValueFromX(e.X);
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (_dragging)
		{
			SetValueFromX(e.X);
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_dragging = false;
		}
		base.OnMouseUp(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Left)
		{
			Value--;
		}
		else if (e.KeyCode == Keys.Right)
		{
			Value++;
		}
		else if (e.KeyCode == Keys.PageDown)
		{
			Value -= 15;
		}
		else if (e.KeyCode == Keys.PageUp)
		{
			Value += 15;
		}
		base.OnKeyDown(e);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		Invalidate();
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		Invalidate();
		base.OnLostFocus(e);
	}

	private void SetValueFromX(int x)
	{
		Rectangle track = TrackBounds;
		float num = (x - track.X) / (float)Math.Max(1, track.Width);
		Value = Minimum + (int)Math.Round(Math.Max(0f, Math.Min(1f, num)) * (Maximum - Minimum));
	}

	private static Color GetHueColor(float fraction)
	{
		float hue = Math.Max(0f, Math.Min(1f, fraction)) * 360f;
		int sector = (int)(hue / 60f) % 6;
		float num = hue / 60f - (float)(int)(hue / 60f);
		float num2 = 1f - num;
		return sector switch
		{
			0 => Color.FromArgb(255, 255, (int)(255f * num), 0), 
			1 => Color.FromArgb(255, (int)(255f * num2), 255, 0), 
			2 => Color.FromArgb(255, 0, 255, (int)(255f * num)), 
			3 => Color.FromArgb(255, 0, (int)(255f * num2), 255), 
			4 => Color.FromArgb(255, (int)(255f * num), 0, 255), 
			_ => Color.FromArgb(255, 255, 0, (int)(255f * num2)), 
		};
	}
}
