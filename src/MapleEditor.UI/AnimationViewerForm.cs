using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MapleLib.WzLib;

namespace MapleEditor.UI;

public sealed class AnimationViewerForm : Form
{
	private readonly ListBox _animationList;

	private readonly PictureBox _preview;

	private readonly Label _statusLabel;

	private readonly Button _playButton;

	private readonly SplitContainer _split;

	private IReadOnlyList<WzAnimationInfo> _animations = Array.Empty<WzAnimationInfo>();

	private WzAnimationClip? _clip;

	private AnimationPlayback? _playback;

	public AnimationViewerForm(IReadOnlyList<WzAnimationInfo> animations, string title)
	{
		Text = title;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterParent;
		ClientSize = new Size(980, 620);
		MinimumSize = new Size(800, 500);
		BackColor = UiTheme.AppBackground;
		ForeColor = UiTheme.Text;
		Font = UiTheme.Font(10f);
		SplitContainer split = new SplitContainer
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Border
		};
		_split = split;
		Controls.Add(split);
		Panel left = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Surface,
			Padding = new Padding(14)
		};
		split.Panel1.Controls.Add(left);
		Label listTitle = new Label
		{
			Dock = DockStyle.Top,
			Height = 34,
			Text = "Animations",
			Font = UiTheme.Font(14f, FontStyle.Bold),
			ForeColor = UiTheme.Text
		};
		_animationList = new ListBox
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.SurfaceRaised,
			ForeColor = UiTheme.Text,
			BorderStyle = BorderStyle.FixedSingle,
			IntegralHeight = false,
			ItemHeight = 26,
			DrawMode = DrawMode.OwnerDrawFixed
		};
		_animationList.DrawItem += (object? _, DrawItemEventArgs e) =>
		{
			if (e.Index < 0)
			{
				return;
			}
			e.DrawBackground();
			Color color = (((e.State & DrawItemState.Selected) != 0) ? UiTheme.Text : UiTheme.TextSubtle);
			TextRenderer.DrawText(e.Graphics, _animationList.Items[e.Index].ToString(), e.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		};
		_animationList.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			LoadSelectedAnimation();
		};
		left.Controls.Add(_animationList);
		left.Controls.Add(listTitle);
		Panel right = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.SurfaceRaised,
			Padding = new Padding(14)
		};
		split.Panel2.Controls.Add(right);
		_preview = new PictureBox
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Surface,
			BorderStyle = BorderStyle.FixedSingle,
			SizeMode = PictureBoxSizeMode.Zoom
		};
		right.Controls.Add(_preview);
		_statusLabel = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 26,
			ForeColor = UiTheme.TextSubtle,
			TextAlign = ContentAlignment.MiddleLeft,
			Text = "Select an animation."
		};
		right.Controls.Add(_statusLabel);
		TableLayoutPanel buttons = new TableLayoutPanel
		{
			Dock = DockStyle.Bottom,
			Height = 58,
			ColumnCount = 5,
			RowCount = 1,
			BackColor = UiTheme.SurfaceRaised,
			Margin = Padding.Empty,
			Padding = new Padding(0, 10, 0, 0)
		};
		buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
		buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
		buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
		right.Controls.Add(buttons);
		_playButton = CreateButton("Pause", 0);
		_playButton.Dock = DockStyle.Fill;
		_playButton.Margin = new Padding(0, 0, 6, 10);
		_playButton.Click += (object? _, EventArgs _) =>
		{
			if (_playback != null)
			{
				_playback.Toggle();
				_playButton.Text = (_playback.IsPlaying ? "Pause" : "Play");
			}
		};
		buttons.Controls.Add(_playButton, 0, 0);
		Button exportApng = CreateButton("Export APNG", 0);
		exportApng.Dock = DockStyle.Fill;
		exportApng.Margin = new Padding(0, 0, 6, 10);
		exportApng.Click += (object? _, EventArgs _) =>
		{
			Export("APNG");
		};
		buttons.Controls.Add(exportApng, 1, 0);
		Button exportGif = CreateButton("Export GIF", 0);
		exportGif.Dock = DockStyle.Fill;
		exportGif.Margin = new Padding(0, 0, 6, 10);
		exportGif.Click += (object? _, EventArgs _) =>
		{
			Export("GIF");
		};
		buttons.Controls.Add(exportGif, 2, 0);
		Button close = CreateButton("Close", 0);
		close.Dock = DockStyle.Fill;
		close.Margin = new Padding(0, 0, 0, 10);
		close.Click += (object? _, EventArgs _) =>
		{
			Close();
		};
		buttons.Controls.Add(close, 4, 0);
		_animations = animations ?? Array.Empty<WzAnimationInfo>();
		foreach (WzAnimationInfo animation in _animations)
		{
			_animationList.Items.Add(animation);
		}
		if (_animationList.Items.Count > 0)
		{
			_animationList.SelectedIndex = 0;
		}
	}

	public static void ShowFor(IWin32Window owner, IPropertyContainer root, string title)
	{
		IReadOnlyList<WzAnimationInfo> animations = WzAnimationReader.Enumerate(root);
		if (animations.Count == 0)
		{
			MessageBox.Show(owner, "No multi-frame animation nodes (0, 1, 2, ...) were found for this selection.", "Animation preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		using AnimationViewerForm form = new AnimationViewerForm(animations, title);
		form.ShowDialog(owner);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		try
		{
			_split.Panel1MinSize = 200;
			_split.Panel2MinSize = 400;
			_split.SplitterDistance = Math.Max(200, Math.Min(270, _split.Width - 400));
		}
		catch
		{
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		DisposePlayback();
		base.OnFormClosing(e);
	}

	private static Button CreateButton(string text, int left)
	{
		Button button = new Button
		{
			Left = left,
			Top = 12,
			Width = 130,
			Height = 34,
			Text = text,
			FlatStyle = FlatStyle.Flat,
			BackColor = UiTheme.AccentSoft,
			ForeColor = UiTheme.Text
		};
		button.FlatAppearance.BorderColor = UiTheme.Border;
		button.FlatAppearance.BorderSize = 1;
		return button;
	}

	private void LoadSelectedAnimation()
	{
		if (_animationList.SelectedItem is not WzAnimationInfo info)
		{
			return;
		}
		DisposePlayback();
		try
		{
			_clip = WzAnimationReader.Load(info);
			_playback = new AnimationPlayback(_preview, _clip.Frames, _clip.Delays);
			_playback.FrameChanged += (object? _, EventArgs _) =>
			{
				UpdateStatus(info);
			};
			_playButton.Text = "Pause";
			_playback.Play();
			UpdateStatus(info);
		}
		catch (Exception ex)
		{
			_statusLabel.Text = "Could not load animation: " + ex.Message;
		}
	}

	private void UpdateStatus(WzAnimationInfo info)
	{
		if (_clip == null || _playback == null)
		{
			return;
		}
		_statusLabel.Text = $"{info.Name} - {_clip.Width}x{_clip.Height}, {_clip.Frames.Count} frames, frame {_playback.FrameIndex + 1}/{_clip.Frames.Count}";
	}

	private void Export(string format)
	{
		if (_clip == null)
		{
			return;
		}
		using SaveFileDialog dialog = new SaveFileDialog
		{
			Title = "Export animation as " + format,
			Filter = (format == "APNG") ? "APNG files (*.png)|*.png" : "GIF files (*.gif)|*.gif",
			FileName = SanitizeName(_clip.Name) + ((format == "APNG") ? ".png" : ".gif")
		};
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			Cursor = Cursors.WaitCursor;
			if (format == "APNG")
			{
				AnimationExporter.SaveApng(dialog.FileName, _clip.Frames, _clip.Delays);
			}
			else
			{
				AnimationExporter.SaveGif(dialog.FileName, _clip.Frames, _clip.Delays);
			}
			_statusLabel.Text = "Exported " + dialog.FileName;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Animation export failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			Cursor = Cursors.Default;
		}
	}

	private static string SanitizeName(string name)
	{
		string result = (name ?? "animation").Replace('/', '_').Replace('\\', '_');
		foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
		{
			result = result.Replace(invalid, '_');
		}
		return result;
	}

	private void DisposePlayback()
	{
		_playback?.Dispose();
		_playback = null;
		_clip?.Dispose();
		_clip = null;
		_preview.Image = null;
	}
}
