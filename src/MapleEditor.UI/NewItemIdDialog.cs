using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class NewItemIdDialog : Form
{
	private readonly TextBox _idBox;

	private readonly Label _errorLabel;

	private readonly Func<string, string> _validate;

	public string SelectedId { get; private set; }

	private NewItemIdDialog(string title, string message, string suggestedId, string category, Func<string, string> validate)
	{
		_validate = validate;
		Icon = UiTheme.AppIcon;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		ClientSize = new Size(560, 306);
		BackColor = UiTheme.AppBackground;
		DoubleBuffered = true;
		Font = UiTheme.Font(10f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		Label titleLabel = new Label
		{
			Text = title,
			Left = 28,
			Top = 24,
			Width = 500,
			Height = 32,
			Font = UiTheme.Font(17f, FontStyle.Bold),
			ForeColor = UiTheme.Text
		};
		Controls.Add(titleLabel);
		Label messageLabel = new Label
		{
			Text = message,
			Left = 30,
			Top = 60,
			Width = 500,
			Height = 52,
			ForeColor = UiTheme.TextSubtle
		};
		Controls.Add(messageLabel);
		if (!string.IsNullOrWhiteSpace(category))
		{
			Label categoryLabel = new Label
			{
				Text = "Category: " + category,
				Left = 30,
				Top = 114,
				Width = 500,
				Height = 22,
				ForeColor = UiTheme.AccentHover
			};
			Controls.Add(categoryLabel);
		}
		Label idCaption = new Label
		{
			Text = "New ID",
			Left = 30,
			Top = 146,
			Width = 80,
			Height = 22,
			ForeColor = UiTheme.TextSubtle
		};
		Controls.Add(idCaption);
		_idBox = new TextBox
		{
			Left = 30,
			Top = 170,
			Width = 500,
			Height = 32,
			Text = suggestedId,
			BackColor = UiTheme.SurfaceRaised,
			ForeColor = UiTheme.Text,
			BorderStyle = BorderStyle.FixedSingle,
			Font = UiTheme.Font(12f)
		};
		Controls.Add(_idBox);
		_errorLabel = new Label
		{
			Left = 30,
			Top = 206,
			Width = 500,
			Height = 22,
			ForeColor = UiTheme.Danger,
			Text = string.Empty
		};
		Controls.Add(_errorLabel);
		RoundedButton ok = new RoundedButton
		{
			Text = "Create",
			Left = 340,
			Top = 248,
			Width = 90,
			Height = 36,
			BackColor = UiTheme.Accent,
			ForeColor = Color.White,
			CornerRadius = 9
		};
		ok.FlatAppearance.BorderColor = UiTheme.Accent;
		ok.Click += (object? _, EventArgs _) =>
		{
			TryAccept();
		};
		Controls.Add(ok);
		RoundedButton cancel = new RoundedButton
		{
			Text = "Cancel",
			Left = 440,
			Top = 248,
			Width = 90,
			Height = 36,
			BackColor = UiTheme.SurfaceRaised,
			ForeColor = UiTheme.Text,
			CornerRadius = 9
		};
		cancel.FlatAppearance.BorderColor = UiTheme.Border;
		cancel.Click += (object? _, EventArgs _) =>
		{
			DialogResult = DialogResult.Cancel;
			Close();
		};
		Controls.Add(cancel);
		AcceptButton = null;
		_idBox.KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Enter)
			{
				TryAccept();
				e.SuppressKeyPress = true;
			}
		};
		MouseDown += DragWindow;
		titleLabel.MouseDown += DragWindow;
		messageLabel.MouseDown += DragWindow;
	}

	private int _dragX;
	private int _dragY;
	private bool _dragging;

	private void DragWindow(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_dragging = true;
			_dragX = e.X;
			_dragY = e.Y;
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (_dragging)
		{
			Left += e.X - _dragX;
			Top += e.Y - _dragY;
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		_dragging = false;
		base.OnMouseUp(e);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		using GraphicsPath path = UiTheme.RoundedRect(new Rectangle(0, 0, Width, Height), 16f);
		Region = new Region(path);
		_idBox.Focus();
		_idBox.SelectAll();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using GraphicsPath path = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 16f);
		using Pen pen = new Pen(UiTheme.BorderStrong, 1f);
		e.Graphics.DrawPath(pen, path);
	}

	private void TryAccept()
	{
		string text = _idBox.Text.Trim();
		string text2 = _validate?.Invoke(text);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			_errorLabel.Text = text2;
			return;
		}
		SelectedId = text;
		DialogResult = DialogResult.OK;
		Close();
	}

	public static string Show(IWin32Window owner, string title, string message, string suggestedId, string category, Func<string, string> validate)
	{
		using NewItemIdDialog newItemIdDialog = new NewItemIdDialog(title, message, suggestedId, category, validate);
		if (newItemIdDialog.ShowDialog(owner) == DialogResult.OK)
		{
			return newItemIdDialog.SelectedId;
		}
		return null;
	}
}
