using System;
using System.Drawing;
using System.Windows.Forms;
using MapleEditor.UI;
using MapleLib.Configuration;

namespace MapleMobEditor;

internal sealed class EditorLauncherForm : Form
{
	private readonly RoundedButton _v83Button;

	private readonly RoundedButton _v95Button;

	private readonly Label _versionHintLabel;

	private readonly BrandHeader _header;

	public EditorLauncherForm()
	{
		Text = "Maple Editor Suite";
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterScreen;
		ClientSize = new Size(1120, 670);
		MinimumSize = new Size(1000, 620);
		BackColor = UiTheme.AppBackground;
		Font = UiTheme.Font(10f);
		_header = new BrandHeader
		{
			Dock = DockStyle.Top,
			Height = 188,
			Title = "Maple Editor Suite",
			Subtitle = "WZ archives and extracted IMG/XML workflows for v83 and v95",
			VersionText = MapleEditorVersionSettings.Profile.DisplayName
		};
		Panel content = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(34, 18, 34, 26),
			BackColor = UiTheme.AppBackground
		};
		Controls.Add(content);
		Controls.Add(_header);
		RoundedPanel versionBar = new RoundedPanel
		{
			Dock = DockStyle.Top,
			Height = 76,
			Padding = new Padding(22, 12, 22, 12),
			BackColor = UiTheme.Surface,
			CornerRadius = 14
		};
		Label versionCaption = new Label
		{
			AutoSize = true,
			Text = "Data version",
			Font = UiTheme.Font(10.5f, FontStyle.Bold),
			ForeColor = UiTheme.Text,
			Location = new Point(22, 14)
		};
		versionBar.Controls.Add(versionCaption);
		_v83Button = CreateVersionButton("v83 (native)", 132, 10);
		_v83Button.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V83);
		};
		versionBar.Controls.Add(_v83Button);
		_v95Button = CreateVersionButton("v95", 258, 10);
		_v95Button.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V95);
		};
		versionBar.Controls.Add(_v95Button);
		_versionHintLabel = new Label
		{
			AutoSize = false,
			Text = string.Empty,
			ForeColor = UiTheme.TextSubtle,
			Location = new Point(396, 15),
			Width = 640,
			Height = 42
		};
		versionBar.Controls.Add(_versionHintLabel);
		TableLayoutPanel cards = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(0, 18, 0, 0),
			ColumnCount = 3,
			RowCount = 1
		};
		cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
		cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
		cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
		content.Controls.Add(cards);
		content.Controls.Add(versionBar);
		cards.Controls.Add(CreateCard("Open Mobs", "Browse Mob.wz archives or extracted .img/.img.xml folders. String.wz names load automatically.", "Mob Editor", OpenMobEditor), 0, 0);
		cards.Controls.Add(CreateCard("Open Items", "Browse any WZ archive or .img/.img.xml folder, then edit icons, rewards, IDs and nodes.", "Item Editor", OpenItemEditor), 1, 0);
		cards.Controls.Add(CreateCard("Open Codex", "Edit MonsterBook cards in Item.wz Consume/0238.img or 0238.img.xml with String.wz names.", "Codex Creator", OpenCodexEditor), 2, 0);
		LoadVersionSelection();
	}

	private void LoadVersionSelection()
	{
		UpdateVersionButtons();
		UpdateVersionHint();
	}

	private RoundedButton CreateVersionButton(string text, int left, int top)
	{
		RoundedButton roundedButton = new RoundedButton
		{
			Text = text,
			Left = left,
			Top = top,
			Width = 118,
			Height = 34,
			Font = UiTheme.Font(9.5f, FontStyle.Bold),
			CornerRadius = 9
		};
		roundedButton.FlatAppearance.BorderColor = UiTheme.Border;
		return roundedButton;
	}

	private void SetVersion(MapleEditorGameVersion version)
	{
		MapleEditorVersionSettings.Current = version;
		_header.VersionText = MapleEditorVersionSettings.Profile.DisplayName;
		UpdateVersionButtons();
		UpdateVersionHint();
	}

	private void UpdateVersionButtons()
	{
		bool flag = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V83;
		_v83Button.BackColor = (flag ? UiTheme.Accent : UiTheme.SurfaceRaised);
		_v83Button.ForeColor = (flag ? Color.White : UiTheme.Text);
		_v83Button.FlatAppearance.BorderColor = (flag ? UiTheme.Accent : UiTheme.Border);
		_v95Button.BackColor = (flag ? UiTheme.SurfaceRaised : UiTheme.Accent);
		_v95Button.ForeColor = (flag ? UiTheme.Text : Color.White);
		_v95Button.FlatAppearance.BorderColor = (flag ? UiTheme.Border : UiTheme.Accent);
	}

	private void UpdateVersionHint()
	{
		if (MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95)
		{
			_versionHintLabel.Text = "v95 WZ mode: reads and writes .wz archives and loads String.wz names automatically.";
		}
		else
		{
			_versionHintLabel.Text = "v83 native mode: reads and writes extracted .img/.img.xml files. WZ archives remain available from File > Open WZ Archive.";
		}
	}

	private RoundedPanel CreateCard(string actionLabel, string description, string editorName, EventHandler clickHandler)
	{
		RoundedPanel panel = new RoundedPanel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(10, 0, 10, 0),
			Padding = new Padding(22),
			BackColor = UiTheme.Surface,
			CornerRadius = 18
		};
		Label kicker = new Label
		{
			AutoSize = true,
			Text = editorName.ToUpperInvariant(),
			Font = UiTheme.Font(8.5f, FontStyle.Bold),
			ForeColor = UiTheme.Accent,
			Location = new Point(22, 24)
		};
		panel.Controls.Add(kicker);
		Label title = new Label
		{
			AutoSize = true,
			Text = actionLabel,
			Font = UiTheme.Font(21f, FontStyle.Bold),
			ForeColor = UiTheme.Text,
			Location = new Point(22, 48)
		};
		panel.Controls.Add(title);
		Label desc = new Label
		{
			AutoSize = false,
			Text = description,
			Font = UiTheme.Font(9.5f),
			ForeColor = UiTheme.TextSubtle,
			Location = new Point(24, 96),
			Width = 260,
			Height = 96
		};
		panel.Controls.Add(desc);
		RoundedButton button = new RoundedButton
		{
			Text = actionLabel,
			Font = UiTheme.Font(10.5f, FontStyle.Bold),
			Size = new Size(190, 44),
			Location = new Point(22, 218),
			BackColor = UiTheme.Accent,
			ForeColor = Color.White,
			CornerRadius = 11
		};
		button.FlatAppearance.BorderColor = UiTheme.Accent;
		button.Click += clickHandler;
		panel.Controls.Add(button);
		panel.Resize += (object? _, EventArgs _) =>
		{
			button.Top = Math.Max(170, panel.Height - button.Height - 24);
			desc.Width = Math.Max(180, panel.Width - 48);
		};
		return panel;
	}

	private void OpenMobEditor(object? sender, EventArgs e)
	{
		using MainForm mainForm = new MainForm();
		mainForm.ShowDialog(this);
	}

	private void OpenItemEditor(object? sender, EventArgs e)
	{
		using MapleImgEditor.MainForm mainForm = new MapleImgEditor.MainForm();
		mainForm.ShowDialog(this);
	}

	private void OpenCodexEditor(object? sender, EventArgs e)
	{
		using MapleCodexCreator.MainForm mainForm = new MapleCodexCreator.MainForm();
		mainForm.ShowDialog(this);
	}
}


