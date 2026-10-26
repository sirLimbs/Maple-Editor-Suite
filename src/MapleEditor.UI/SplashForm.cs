using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class SplashForm : Form
{
	private readonly string _title;

	private readonly string _subtitle;

	private string _status = "Loading...";

	private double _progress = -1.0;

	private readonly System.Windows.Forms.Timer _animationTimer;

	private int _marqueeOffset;

	private readonly bool _startup;

	public SplashForm(string title, string subtitle, bool startup = false)
	{
		_title = title;
		_subtitle = subtitle;
		_startup = startup;
		Icon = UiTheme.AppIcon;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = startup ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		TopMost = startup;
		ClientSize = new Size(620, 340);
		BackColor = UiTheme.AppBackground;
		DoubleBuffered = true;
		Font = UiTheme.Font(10f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		_animationTimer = new System.Windows.Forms.Timer
		{
			Interval = 35
		};
		_animationTimer.Tick += (object? _, EventArgs _) =>
		{
			if (_progress < 0.0)
			{
				_marqueeOffset = (_marqueeOffset + 6) % 120;
				Invalidate();
			}
		};
		_animationTimer.Start();
	}

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ClassStyle |= 0x20000;
			return createParams;
		}
	}

	public void Report(string status, double progress = -1.0)
	{
		_status = status ?? string.Empty;
		_progress = Math.Max(-1.0, Math.Min(1.0, progress));
		Invalidate();
		Refresh();
		Application.DoEvents();
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		using GraphicsPath path = UiTheme.RoundedRect(new Rectangle(0, 0, Width, Height), 18f);
		Region = new Region(path);
		Opacity = 0.0;
		for (int i = 0; i <= 10; i++)
		{
			Opacity = i / 10.0;
			Application.DoEvents();
			Thread.Sleep(8);
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		_animationTimer.Stop();
		_animationTimer.Dispose();
		base.OnFormClosing(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		Rectangle bounds = ClientRectangle;
		using LinearGradientBrush background = new LinearGradientBrush(bounds, UiTheme.AppBackground, Color.FromArgb(22, 30, 42), 90f);
		graphics.FillRectangle(background, bounds);
		UiTheme.DrawImageAspectFill(graphics, UiTheme.Banner, new Rectangle(0, Height - 120, Width, 120));
		using SolidBrush overlay = new SolidBrush(Color.FromArgb(130, UiTheme.AppBackground));
		graphics.FillRectangle(overlay, new Rectangle(0, Height - 120, Width, 120));
		Rectangle logoBounds = new Rectangle(34, 34, 96, 96);
		InterpolationMode interpolationMode = graphics.InterpolationMode;
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.DrawImage(UiTheme.Logo, logoBounds);
		graphics.InterpolationMode = interpolationMode;
		using Font titleFont = UiTheme.Font(24f, FontStyle.Bold);
		using Font subtitleFont = UiTheme.Font(10.5f);
		using Font statusFont = UiTheme.Font(9.5f);
		TextRenderer.DrawText(graphics, _title, titleFont, new Rectangle(150, 46, Width - 190, 40), UiTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		TextRenderer.DrawText(graphics, _subtitle, subtitleFont, new Rectangle(152, 88, Width - 190, 24), UiTheme.TextSubtle, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		Rectangle progressBounds = new Rectangle(34, Height - 72, Width - 68, 10);
		UiTheme.FillRounded(graphics, progressBounds, 5f, Color.FromArgb(44, UiTheme.BorderStrong));
		if (_progress >= 0.0)
		{
			int width = (int)(progressBounds.Width * _progress);
			if (width > 2)
			{
				UiTheme.FillRounded(graphics, new Rectangle(progressBounds.X, progressBounds.Y, width, progressBounds.Height), 5f, UiTheme.Accent);
			}
		}
		else
		{
			int width = 120;
			int x = progressBounds.X + _marqueeOffset - 120;
			Rectangle clip = progressBounds;
			graphics.SetClip(clip);
			using LinearGradientBrush marquee = new LinearGradientBrush(new Rectangle(x, progressBounds.Y, width, progressBounds.Height), Color.FromArgb(0, UiTheme.Accent), UiTheme.Accent, 0f);
			graphics.FillRectangle(marquee, new Rectangle(x, progressBounds.Y, width, progressBounds.Height));
			graphics.ResetClip();
		}
		TextRenderer.DrawText(graphics, _status, statusFont, new Rectangle(34, Height - 50, Width - 68, 22), UiTheme.TextSubtle, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		using Font versionFont = UiTheme.Font(8.5f, FontStyle.Bold);
		TextRenderer.DrawText(graphics, "MAPLE EDITOR", versionFont, new Rectangle(Width - 190, Height - 50, 156, 22), UiTheme.Accent, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
		using Pen border = new Pen(Color.FromArgb(120, UiTheme.Accent), 1.2f);
		using GraphicsPath borderPath = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 18f);
		graphics.DrawPath(border, borderPath);
	}

	public static void ShowStartup(string title, string subtitle, int milliseconds = 1100)
	{
		using SplashForm splash = new SplashForm(title, subtitle, startup: true);
		splash.Report("Preparing editors and theme...", -1.0);
		splash.Show();
		Application.DoEvents();
		Stopwatch stopwatch = Stopwatch.StartNew();
		string[] phases = new string[4]
		{
			"Loading editor modules...",
			"Preparing WZ profiles (v83 / v95)...",
			"Checking String.wz name lookup...",
			"Ready."
		};
		int phase = 0;
		while (stopwatch.ElapsedMilliseconds < milliseconds)
		{
			double num = Math.Min(1.0, stopwatch.ElapsedMilliseconds / (double)milliseconds);
			int num2 = Math.Min(phases.Length - 1, (int)(num * phases.Length));
			if (num2 != phase)
			{
				phase = num2;
				splash.Report(phases[phase], num);
			}
			else if (num <= 0.01)
			{
				splash.Report(phases[0], num);
			}
			Application.DoEvents();
			Thread.Sleep(20);
		}
		splash.Report("Ready.", 1.0);
		Application.DoEvents();
	}
}

