using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MapleEditor.UI;
using MapleLib.Configuration;
using MapleLib.WzLib;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleImgEditor;

public sealed class MainForm : Form
{
	private const int PreviewCardSize = 128;

	private const int HeaderSectionHeight = 168;

	private const int ActionsSectionHeight = 96;

	private const int ActionButtonWidth = 168;

	private const int ActionButtonHeight = 34;

	private const int SectionGap = 12;

	private static readonly Color AppBackColor = UiTheme.AppBackground;

	private static readonly Color SurfaceColor = UiTheme.Surface;

	private static readonly Color SurfaceRaisedColor = UiTheme.SurfaceRaised;

	private static readonly Color AccentColor = UiTheme.Accent;

	private static readonly Color AccentMutedColor = Color.FromArgb(66, UiTheme.Accent);

	private static readonly Color BorderColor = UiTheme.Border;

	private static readonly Color TextColor = UiTheme.Text;

	private static readonly Color SubtleTextColor = UiTheme.TextSubtle;

	private static readonly HashSet<string> IdReferenceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"item", "itemid", "itemid2", "linkeditem", "link", "target", "targetid", "reward", "rewardid", "replace", "consume", "mob", "mobid", "npc", "npcid"
	};

	private readonly AssetGridView _assetGrid;

	private readonly BrandHeader _header;

	private readonly Label _previewSizeLabel;

	private readonly Label _rewardPreviewSizeLabel;

	private readonly SplitContainer _splitContainer;

	private readonly TextBox _searchTextBox;

	private readonly ComboBox _cashFilterComboBox;

	private readonly ComboBox _categoryFilterComboBox;

	private readonly ComboBox _tradeLockFilterComboBox;

	private readonly NumericUpDown _minReqLevelFilterInput;

	private readonly NumericUpDown _maxReqLevelFilterInput;

	private readonly CheckBox _dirtyOnlyFilterCheckBox;

	private readonly Button _resetFiltersButton;

	private readonly PictureBox _iconPreview;

	private readonly PictureBox _iconRewardPreview;

	private readonly Label _iconRewardPreviewStatusLabel;

	private readonly HueSlider _rewardGlowSlider;

	private readonly Panel _rewardGlowSwatch;

	private readonly PictureBox _rewardGlowPreview;

	private AnimationPlayback? _rewardPreviewPlayback;

	private WzAnimationClip? _rewardPreviewClip;

	private readonly TextBox _itemNameLabel;

	private readonly TextBox _itemContextLabel;

	private readonly TextBox _itemEquipJobsLabel;

	private readonly TextBox _itemPathLabel;

	private readonly RoundedButton _editMenuButton;

	private readonly ContextMenuStrip _editMenu;

	private readonly ToolStripStatusLabel _statusLabel;

	private readonly NumericUpDown _strInput;

	private readonly NumericUpDown _dexInput;

	private readonly NumericUpDown _intInput;

	private readonly NumericUpDown _lukInput;

	private readonly NumericUpDown _padInput;

	private readonly NumericUpDown _madInput;

	private readonly NumericUpDown _speedInput;

	private readonly NumericUpDown _jumpInput;

	private readonly NumericUpDown _hpInput;

	private readonly NumericUpDown _mpInput;

	private readonly NumericUpDown _accInput;

	private readonly NumericUpDown _avoidInput;

	private readonly NumericUpDown _slotsInput;

	private readonly NumericUpDown _reqLevelInput;

	private readonly CheckBox _cashCheckBox;

	private readonly CheckBox _tradeLockCheckBox;

	private readonly Button _applyButton;

	private readonly Button _saveAsImgButton;

	private readonly Button _saveAsXmlButton;

	private readonly Button _saveCurrentButton;

	private readonly Button _saveAllButton;

	private readonly ToolStripMenuItem _cashOnlyMenuItem;

	private readonly ToolStripMenuItem _copyIdsMenuItem;

	private readonly ImgXmlDocumentService _xmlDocumentService = new ImgXmlDocumentService();

	private readonly List<ImgItemEntry> _items = new List<ImgItemEntry>();

	private string? _currentFolderPath;

	private bool _loadingSelection;

	private bool _updatingFilterControls;

	private WzArchiveSession? _archiveSession;

	private WzStringLookup? _stringLookup;

	private ToolStripMenuItem? _saveArchiveMenuItem;

	private ToolStripMenuItem? _v83VersionMenuItem;

	private ToolStripMenuItem? _v95VersionMenuItem;

	public MainForm()
	{
		Text = "Maple Item Editor - " + MapleEditorVersionSettings.Profile.DisplayName;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.Manual;
		BackColor = AppBackColor;
		MinimumSize = new Size(1500, 920);
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		Bounds = GetStartupBounds();
		MenuStrip value = (MainMenuStrip = BuildMenu(out _cashOnlyMenuItem, out _copyIdsMenuItem));
		UiTheme.StyleMenu(value);
		_header = new BrandHeader
		{
			Dock = DockStyle.Top,
			Height = 94,
			Compact = true,
			Title = "Maple Item Editor",
			Subtitle = "WZ archive item grid, icons, reward previews and node editing",
			VersionText = MapleEditorVersionSettings.Profile.DisplayName,
			RightText = "No archive loaded"
		};
		StatusStrip statusStrip = new StatusStrip();
		_statusLabel = new ToolStripStatusLabel("Open a WZ archive or a folder containing .img/.img.xml files");
		UiTheme.StyleStatus(statusStrip);
		statusStrip.Items.Add(_statusLabel);
		SplitContainer splitContainer = new SplitContainer
		{
			Dock = DockStyle.Fill,
			SplitterDistance = 560,
			BackColor = AppBackColor,
			Padding = new Padding(18, 6, 18, 6)
		};
		_splitContainer = splitContainer;
		Controls.Add(splitContainer);
		Controls.Add(statusStrip);
		Controls.Add(_header);
		Controls.Add(value);
		Panel panel = CreateCardPanel();
		panel.Padding = new Padding(14);
		splitContainer.Panel1.Controls.Add(panel);
		_assetGrid = new AssetGridView
		{
			Dock = DockStyle.Fill
		};
		_assetGrid.SelectionChanged += (object? _, EventArgs _) =>
		{
			if (!_loadingSelection)
			{
				LoadSelectedItem();
			}
		};
		_assetGrid.CopyRequested += (object? _, EventArgs _) =>
		{
			CopySelectedIds();
		};
		_assetGrid.RowActivated += (object? _, AssetGridRowEventArgs e) =>
		{
			CopyItemId(e.Row.Tag as ImgItemEntry);
		};
		Label value2 = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Item Browser",
			Font = new Font("Bahnschrift SemiBold", 16f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor,
			TextAlign = ContentAlignment.MiddleLeft
		};
		Label value3 = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Rows sorted by ID. Ctrl+C copies IDs; double-click copies one.",
			ForeColor = SubtleTextColor,
			TextAlign = ContentAlignment.TopLeft
		};
		_searchTextBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 0, 0, 8),
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor,
			PlaceholderText = "Search items by id, category, or name"
		};
		_searchTextBox.TextChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			WrapContents = true,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = new Padding(0, 0, 0, 8),
			Padding = new Padding(0, 4, 0, 0),
			BackColor = SurfaceColor
		};
		_cashFilterComboBox = new DarkComboBox
		{
			Width = 110,
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor
		};
		_cashFilterComboBox.Items.AddRange("All", "Cash only", "Non-cash only");
		_cashFilterComboBox.SelectedIndex = 0;
		_cashFilterComboBox.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			if (!_updatingFilterControls)
			{
				_updatingFilterControls = true;
				_cashOnlyMenuItem.Checked = _cashFilterComboBox.SelectedIndex == 1;
				_updatingFilterControls = false;
				RefreshItemBrowser();
			}
		};
		_categoryFilterComboBox = new DarkComboBox
		{
			Width = 160,
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor
		};
		_categoryFilterComboBox.Items.Add("All categories");
		_categoryFilterComboBox.SelectedIndex = 0;
		_categoryFilterComboBox.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			if (!_updatingFilterControls)
			{
				RefreshItemBrowser();
			}
		};
		_tradeLockFilterComboBox = new DarkComboBox
		{
			Width = 140,
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor
		};
		_tradeLockFilterComboBox.Items.AddRange("Trade: all", "Trade-locked", "Tradeable");
		_tradeLockFilterComboBox.SelectedIndex = 0;
		_tradeLockFilterComboBox.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		_minReqLevelFilterInput = new NumericUpDown
		{
			Width = 70,
			Minimum = 0m,
			Maximum = 300m,
			Value = 0m,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor
		};
		_minReqLevelFilterInput.ValueChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		_maxReqLevelFilterInput = new NumericUpDown
		{
			Width = 70,
			Minimum = 0m,
			Maximum = 300m,
			Value = 300m,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor
		};
		_maxReqLevelFilterInput.ValueChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		_dirtyOnlyFilterCheckBox = new CheckBox
		{
			AutoSize = true,
			Text = "Dirty only",
			ForeColor = TextColor,
			Margin = new Padding(4, 8, 8, 0)
		};
		_dirtyOnlyFilterCheckBox.CheckedChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		_resetFiltersButton = new RoundedButton
		{
			AutoSize = true,
			Text = "Reset",
			Margin = new Padding(4, 3, 0, 0)
		};
		_resetFiltersButton.Click += (object? _, EventArgs _) =>
		{
			ResetBrowserFilters();
		};
		ApplyButtonStyle(_resetFiltersButton, primary: false);
		_cashFilterComboBox.Margin = new Padding(0, 3, 0, 3);
		_categoryFilterComboBox.Margin = new Padding(0, 3, 0, 3);
		_minReqLevelFilterInput.Margin = new Padding(0, 3, 0, 3);
		_maxReqLevelFilterInput.Margin = new Padding(0, 3, 0, 3);
		_tradeLockFilterComboBox.Margin = new Padding(0, 3, 0, 3);
		_dirtyOnlyFilterCheckBox.Margin = new Padding(0, 8, 0, 0);
		_resetFiltersButton.Margin = new Padding(0, 4, 0, 0);
		Label levelCaptionTo = CreateFilterCaption("to");
		levelCaptionTo.Margin = new Padding(4, 8, 4, 0);
		flowLayoutPanel.Controls.Add(CreateFilterGroup(CreateFilterCaption("Cash"), _cashFilterComboBox));
		flowLayoutPanel.Controls.Add(CreateFilterGroup(CreateFilterCaption("Category"), _categoryFilterComboBox));
		flowLayoutPanel.Controls.Add(CreateFilterGroup(_resetFiltersButton));
		flowLayoutPanel.Controls.Add(CreateFilterGroup(CreateFilterCaption("Req Lv"), _minReqLevelFilterInput, levelCaptionTo, _maxReqLevelFilterInput));
		flowLayoutPanel.Controls.Add(CreateFilterGroup(_tradeLockFilterComboBox));
		flowLayoutPanel.Controls.Add(CreateFilterGroup(_dirtyOnlyFilterCheckBox));
		TableLayoutPanel browserLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 5,
			BackColor = SurfaceColor,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		browserLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		browserLayout.Controls.Add(value2, 0, 0);
		browserLayout.Controls.Add(value3, 0, 1);
		browserLayout.Controls.Add(_searchTextBox, 0, 2);
		browserLayout.Controls.Add(flowLayoutPanel, 0, 3);
		browserLayout.Controls.Add(_assetGrid, 0, 4);
		panel.Controls.Add(browserLayout);
		Panel panel2 = CreateCardPanel();
		panel2.Padding = new Padding(12);
		panel2.AutoScroll = true;
		splitContainer.Panel2.Controls.Add(panel2);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		panel2.Controls.Add(tableLayoutPanel);
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 190,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(16),
			Margin = new Padding(0, 0, 0, 12)
		};
		tableLayoutPanel.Controls.Add(panel3, 0, 0);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 3,
			RowCount = 1,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		panel3.Controls.Add(tableLayoutPanel2);
		Panel panel4 = CreatePreviewCard("Item Preview");
		Panel panel5 = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 128,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = SurfaceRaisedColor
		};
		panel4.Controls.Add(panel5);
		_iconPreview = new PictureBox
		{
			Dock = DockStyle.Fill,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = SurfaceRaisedColor
		};
		panel5.Controls.Add(_iconPreview);
		_previewSizeLabel = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 20,
			Text = "32 px",
			TextAlign = ContentAlignment.MiddleRight,
			ForeColor = UiTheme.TextMuted,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(0, 0, 8, 2),
			Font = UiTheme.Font(8f)
		};
		panel5.Controls.Add(_previewSizeLabel);
		tableLayoutPanel2.Controls.Add(panel4, 0, 0);
		Panel panel6 = CreatePreviewCard("Reward Preview");
		Panel panel7 = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 128,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = SurfaceRaisedColor
		};
		panel6.Controls.Add(panel7);
		_iconRewardPreview = new PictureBox
		{
			Dock = DockStyle.Fill,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = SurfaceRaisedColor
		};
		panel7.Controls.Add(_iconRewardPreview);
		_iconRewardPreviewStatusLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "No reward",
			TextAlign = ContentAlignment.MiddleCenter,
			ForeColor = SubtleTextColor,
			BackColor = Color.Transparent
		};
		panel7.Controls.Add(_iconRewardPreviewStatusLabel);
		_rewardPreviewSizeLabel = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 20,
			Text = "32 px",
			TextAlign = ContentAlignment.MiddleRight,
			ForeColor = UiTheme.TextMuted,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(0, 0, 8, 2),
			Font = UiTheme.Font(8f)
		};
		panel7.Controls.Add(_rewardPreviewSizeLabel);
		_iconRewardPreviewStatusLabel.BringToFront();
		tableLayoutPanel2.Controls.Add(panel6, 1, 0);
		Panel panel8 = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(14, 2, 0, 0),
			Margin = Padding.Empty
		};
		tableLayoutPanel2.Controls.Add(panel8, 2, 0);
		_itemNameLabel = CreateSelectableText(0, 0, 470, 40, new Font("Bahnschrift SemiBold", 20f, FontStyle.Bold, GraphicsUnit.Point), TextColor, multiline: false);
		_itemNameLabel.Text = "No item selected";
		panel8.Controls.Add(_itemNameLabel);
		_itemContextLabel = CreateSelectableText(2, 44, 470, 22, Font, SubtleTextColor, multiline: false);
		_itemContextLabel.Text = "Detected context";
		panel8.Controls.Add(_itemContextLabel);
		_itemEquipJobsLabel = CreateSelectableText(2, 68, 470, 22, Font, SubtleTextColor, multiline: false);
		_itemEquipJobsLabel.Text = "Equip Jobs: -";
		panel8.Controls.Add(_itemEquipJobsLabel);
		_itemPathLabel = CreateSelectableText(2, 94, 470, 50, Font, SubtleTextColor, multiline: true);
		_itemPathLabel.Text = string.Empty;
		panel8.Controls.Add(_itemPathLabel);
		Panel panel9 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 110,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(16, 14, 16, 12),
			Margin = new Padding(0, 0, 0, 12)
		};
		tableLayoutPanel.Controls.Add(panel9, 0, 1);
		_editMenuButton = new RoundedDropDownButton
		{
			Left = 16,
			Top = 38,
			Width = 132,
			Height = 34,
			Text = "Edit"
		};
		_editMenuButton.Click += (object? _, EventArgs _) =>
		{
			ShowEditMenu();
		};
		ApplyButtonStyle(_editMenuButton, primary: false);
		panel9.Controls.Add(_editMenuButton);
		_editMenu = BuildEditMenu();
		Label rewardGlowCaption = new Label
		{
			Left = 164,
			Top = 45,
			AutoSize = true,
			Text = "Reward glow",
			ForeColor = SubtleTextColor
		};
		panel9.Controls.Add(rewardGlowCaption);
		_rewardGlowSlider = new HueSlider
		{
			Left = 254,
			Top = 34,
			Width = 200,
			Height = 40,
			Minimum = 0,
			Maximum = 360,
			Value = (int)IconRewardRenderer.DefaultGlowHue,
			BackColor = SurfaceRaisedColor
		};
		_rewardGlowSlider.ValueChanged += (object? _, EventArgs _) =>
		{
			UpdateRewardGlowControls();
		};
		panel9.Controls.Add(_rewardGlowSlider);
		_rewardGlowSwatch = new Panel
		{
			Left = 468,
			Top = 45,
			Width = 30,
			Height = 30,
			BackColor = IconRewardRenderer.GetGlowColor(IconRewardRenderer.DefaultGlowHue),
			BorderStyle = BorderStyle.FixedSingle
		};
		panel9.Controls.Add(_rewardGlowSwatch);
		_rewardGlowPreview = new PictureBox
		{
			Left = 510,
			Top = 6,
			Width = 60,
			Height = 96,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = SurfaceColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		panel9.Controls.Add(_rewardGlowPreview);
		Panel panel10 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 484,
			BackColor = SurfaceRaisedColor,
			Margin = Padding.Empty
		};
		tableLayoutPanel.Controls.Add(panel10, 0, 2);
		Label value4 = new Label
		{
			Left = 16,
			Top = 16,
			AutoSize = true,
			Text = "Focused Stat Editor",
			Font = new Font("Bahnschrift SemiBold", 14f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor
		};
		panel10.Controls.Add(value4);
		Label value5 = new Label
		{
			Left = 16,
			Top = 44,
			Width = 760,
			Text = "Edits item stats in info and supports mass editing across selected items.",
			ForeColor = SubtleTextColor
		};
		panel10.Controls.Add(value5);
		int num = 64;
		_strInput = AddStatInput(panel10, "Bonus STR", 16, num);
		_dexInput = AddStatInput(panel10, "Bonus DEX", 200, num);
		_intInput = AddStatInput(panel10, "Bonus INT", 384, num);
		_lukInput = AddStatInput(panel10, "Bonus LUK", 568, num);
		_padInput = AddStatInput(panel10, "Weapon Attack", 16, num + 88);
		_madInput = AddStatInput(panel10, "Magic Attack", 200, num + 88);
		_speedInput = AddStatInput(panel10, "Speed", 384, num + 88);
		_jumpInput = AddStatInput(panel10, "Jump", 568, num + 88);
		_hpInput = AddStatInput(panel10, "HP", 16, num + 176);
		_mpInput = AddStatInput(panel10, "MP", 200, num + 176);
		_accInput = AddStatInput(panel10, "Accuracy", 384, num + 176);
		_avoidInput = AddStatInput(panel10, "Avoidability", 568, num + 176);
		_slotsInput = AddStatInput(panel10, "Upgrade Slots", 16, num + 264);
		_reqLevelInput = AddStatInput(panel10, "Req Level", 200, num + 264);
		_cashCheckBox = new CheckBox
		{
			Left = 384,
			Top = num + 292,
			Width = 190,
			Height = 28,
			Text = "Cash Item (0 / 1)",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel10.Controls.Add(_cashCheckBox);
		_tradeLockCheckBox = new CheckBox
		{
			Left = 568,
			Top = num + 292,
			Width = 190,
			Height = 28,
			Text = "Trade Lock (0 / 1)",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel10.Controls.Add(_tradeLockCheckBox);
		_applyButton = new RoundedButton
		{
			Left = 16,
			Top = num + 324,
			Width = 148,
			Height = 38,
			Text = "Apply Stats",
			Enabled = false
		};
		_applyButton.Click += (object? _, EventArgs _) =>
		{
			ApplySelectedStats();
		};
		ApplyButtonStyle(_applyButton, primary: true);
		panel10.Controls.Add(_applyButton);
		_saveAsImgButton = new RoundedButton
		{
			Left = 16,
			Top = num + 370,
			Width = 148,
			Height = 38,
			Text = "Save As IMG",
			Enabled = false
		};
		_saveAsImgButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedAsImg();
		};
		ApplyButtonStyle(_saveAsImgButton, primary: false);
		panel10.Controls.Add(_saveAsImgButton);
		_saveAsXmlButton = new RoundedButton
		{
			Left = 172,
			Top = num + 370,
			Width = 148,
			Height = 38,
			Text = "Save As XML",
			Enabled = false
		};
		_saveAsXmlButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedAsXml();
		};
		ApplyButtonStyle(_saveAsXmlButton, primary: false);
		panel10.Controls.Add(_saveAsXmlButton);
		_saveCurrentButton = new RoundedButton
		{
			Left = 172,
			Top = num + 324,
			Width = 148,
			Height = 38,
			Text = "Save Selected",
			Enabled = false
		};
		_saveCurrentButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedItem();
		};
		ApplyButtonStyle(_saveCurrentButton, primary: false);
		panel10.Controls.Add(_saveCurrentButton);
		_saveAllButton = new RoundedButton
		{
			Left = 328,
			Top = num + 324,
			Width = 148,
			Height = 38,
			Text = "Save All",
			Enabled = false
		};
		_saveAllButton.Click += (object? _, EventArgs _) =>
		{
			SaveAllItems();
		};
		ApplyButtonStyle(_saveAllButton, primary: false);
		panel10.Controls.Add(_saveAllButton);
		SetSelectionState(new List<ImgItemEntry>());
	}

	private MenuStrip BuildMenu(out ToolStripMenuItem cashOnlyMenuItem, out ToolStripMenuItem copyIdsMenuItem)
	{
		MenuStrip menuStrip = new MenuStrip
		{
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			RenderMode = ToolStripRenderMode.System
		};
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("File");
		toolStripMenuItem.DropDownItems.Add("Open Folder...", null, (object? _, EventArgs _) =>
		{
			OpenFolder();
		});
		toolStripMenuItem.DropDownItems.Add("Open WZ Archive...", null, (object? _, EventArgs _) =>
		{
			OpenWzArchive();
		});
		_saveArchiveMenuItem = new ToolStripMenuItem("Save WZ Archive", null, (object? _, EventArgs _) =>
		{
			SaveWzArchive(askConfirmation: true);
		})
		{
			Enabled = false
		};
		toolStripMenuItem.DropDownItems.Add(_saveArchiveMenuItem);
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		ToolStripMenuItem duplicateMenuItem = new ToolStripMenuItem("Duplicate Selected...")
		{
			ShortcutKeys = Keys.Control | Keys.D,
			ShowShortcutKeys = true
		};
		duplicateMenuItem.Click += (object? _, EventArgs _) =>
		{
			DuplicateSelectedItem();
		};
		toolStripMenuItem.DropDownItems.Add(duplicateMenuItem);
		toolStripMenuItem.DropDownItems.Add("New Item...", null, (object? _, EventArgs _) =>
		{
			AddNewItem();
		});
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Save Selected", null, (object? _, EventArgs _) =>
		{
			SaveSelectedItem();
		});
		toolStripMenuItem.DropDownItems.Add("Save All", null, (object? _, EventArgs _) =>
		{
			SaveAllItems();
		});
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Exit", null, (object? _, EventArgs _) =>
		{
			Close();
		});
		menuStrip.Items.Add(toolStripMenuItem);
		ToolStripMenuItem versionMenu = new ToolStripMenuItem("Version");
		_v83VersionMenuItem = new ToolStripMenuItem("v83 (native)");
		_v83VersionMenuItem.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V83);
		};
		_v95VersionMenuItem = new ToolStripMenuItem("v95");
		_v95VersionMenuItem.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V95);
		};
		versionMenu.DropDownItems.Add(_v83VersionMenuItem);
		versionMenu.DropDownItems.Add(_v95VersionMenuItem);
		menuStrip.Items.Add(versionMenu);
		ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
		viewMenu.DropDownItems.Add("Preview Animations...", null, (object? _, EventArgs _) =>
		{
			PreviewSelectedAnimations();
		});
		menuStrip.Items.Add(viewMenu);
		cashOnlyMenuItem = new ToolStripMenuItem("Cash Only")
		{
			CheckOnClick = true
		};
		cashOnlyMenuItem.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_updatingFilterControls)
			{
				_updatingFilterControls = true;
				_cashFilterComboBox.SelectedIndex = (_cashOnlyMenuItem.Checked ? 1 : 0);
				_updatingFilterControls = false;
				RefreshItemBrowser();
			}
		};
		menuStrip.Items.Add(cashOnlyMenuItem);
		copyIdsMenuItem = new ToolStripMenuItem("Copy IDs")
		{
			ShortcutKeys = (Keys.C | Keys.Control),
			ShowShortcutKeys = true
		};
		copyIdsMenuItem.Click += (object? _, EventArgs _) =>
		{
			CopySelectedIds();
		};
		menuStrip.Items.Add(copyIdsMenuItem);
		UpdateVersionChecks();
		return menuStrip;
	}

	private void SetVersion(MapleEditorGameVersion version)
	{
		MapleEditorVersionSettings.Current = version;
		Text = "Maple Item Editor - " + MapleEditorVersionSettings.Profile.DisplayName;
		_header.VersionText = MapleEditorVersionSettings.Profile.DisplayName;
		UpdateVersionChecks();
		SetStatus("Data version set to " + MapleEditorVersionSettings.Profile.DisplayName);
	}

	private void UpdateVersionChecks()
	{
		if (_v83VersionMenuItem != null)
		{
			_v83VersionMenuItem.Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V83;
		}
		if (_v95VersionMenuItem != null)
		{
			_v95VersionMenuItem.Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95;
		}
	}

	private void OpenWzArchive()
	{
		if (!ConfirmDiscardChanges())
		{
			return;
		}
		using OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Open a WZ archive (Item.wz, Character.wz, Pet.wz, TamingMob.wz, ...)",
			Filter = "WZ archives (*.wz)|*.wz|All files (*.*)|*.*",
			FileName = string.Empty,
			InitialDirectory = ((MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95) ? MapleEditorVersionSettings.GetDefaultWzFolder() : MapleEditorVersionSettings.GetDefaultOpenFolder())
		};
		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			LoadCharacterArchive(dialog.FileName);
		}
	}

	private void LoadCharacterArchive(string path)
	{
		LoadCharacterArchive(path, null);
	}

	private void LoadCharacterArchive(string path, string? forcedCategory)
	{
		ClearLoadedItems();
		bool keepSession = false;
		SplashForm splash = new SplashForm("Opening " + Path.GetFileName(path), "Reading WZ directory and preparing item previews");
		splash.Show(this);
		splash.Report("Reading WZ header...", -1.0);
		try
		{
			Cursor = Cursors.WaitCursor;
			SetStatus("Reading " + Path.GetFileName(path) + "...");
			WzArchiveSession session = WzArchiveSession.Open(path);
			_archiveSession = session;
			if (_saveArchiveMenuItem != null)
			{
				_saveArchiveMenuItem.Enabled = true;
			}
			_stringLookup = WzStringLookup.TryOpen(MapleEditorVersionSettings.FindSiblingWz(path, "String.wz"));
			string archiveFileName = Path.GetFileName(path);
			if (string.Equals(archiveFileName, "Item.wz", StringComparison.OrdinalIgnoreCase))
			{
				MapleEditorVersionSettings.Settings.LastItemWz = path;
			}
			else
			{
				MapleEditorVersionSettings.Settings.LastCharacterWz = path;
			}
			if (MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95)
			{
				MapleEditorVersionSettings.Settings.V95WzFolder = Path.GetDirectoryName(path) ?? MapleEditorVersionSettings.Settings.V95WzFolder;
			}
			MapleEditorVersionSettings.Save();
			string? selectedCategory = forcedCategory;
			if (selectedCategory == null)
			{
				using ArchiveCategoryForm picker = new ArchiveCategoryForm(session, Path.GetFileName(path));
				if (picker.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				selectedCategory = picker.SelectedCategory;
			}
			if (string.IsNullOrWhiteSpace(selectedCategory))
			{
				return;
			}
			string category = selectedCategory;
			bool root = string.Equals(category, "(root images)", StringComparison.OrdinalIgnoreCase);
			WzDirectory? directory = (root ? session.File.WzDirectory : session.GetDirectory(category));
			if (directory == null)
			{
				throw new InvalidOperationException("Could not find category " + category + " in " + Path.GetFileName(path) + ".");
			}
			string loadCategory = (root ? Path.GetFileNameWithoutExtension(path) : category);
			_loadingSelection = true;
			int count = 0;
			splash.Report("Loading item images...", 0.0);
			foreach (WzImage image in directory.WzImages.OrderBy((WzImage i) => i.Name))
			{
				foreach (ImgItemEntry entry in TryLoadArchiveItems(path, session, image, loadCategory, root))
				{
					_items.Add(entry);
					count++;
					if (count % 40 == 0)
					{
						splash.Report($"Loading items {count} from {directory.WzImages.Count} images...", -1.0);
					}
				}
			}
			_currentFolderPath = path + "\\" + (root ? "(root)" : category);
			_saveAllButton.Enabled = count > 0;
			if (count == 0)
			{
				throw new InvalidOperationException("No readable item nodes were found in that archive category. Try another folder, or use the '(root images)' entry.");
			}
			RefreshCategoryFilterOptions();
			RefreshItemBrowser();
			_header.RightText = Path.GetFileName(path) + " - " + category;
			splash.Report("Ready.", 1.0);
			keepSession = true;
			SetStatus($"Loaded {count} items from {Path.GetFileName(path)}\\{category} (v{session.GameVersion})");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "WZ load failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			splash.Close();
			splash.Dispose();
			_loadingSelection = false;
			Cursor = Cursors.Default;
			LoadSelectedItem();
			if (!keepSession)
			{
				CloseArchiveSession();
			}
		}
	}

	private IEnumerable<ImgItemEntry> TryLoadArchiveItems(string archivePath, WzArchiveSession session, WzImage image, string category, bool isRoot)
	{
		List<ImgItemEntry> entries = new List<ImgItemEntry>();
		try
		{
			image.ParseImage();
			string sourceRoot = Path.GetFileName(archivePath);
			string categoryPath = string.IsNullOrEmpty(category) ? sourceRoot : (sourceRoot + "\\" + category);
			byte[] iv = WzTool.GetIvByMapleVersion(WzMapleVersion.GMS);
			string ivLabel = $"WZ v{session.GameVersion}";
			if (image["info"] is WzSubProperty rootInfo)
			{
				string rootItemId = Path.GetFileNameWithoutExtension(image.Name);
				string rootPath = Path.Combine(archivePath, isRoot ? "Root" : category, image.Name);
				GameFileInfo rootDetection = new GameFileInfo(IsGameAsset: true, sourceRoot, category, rootItemId, categoryPath);
				entries.Add(new ImgItemEntry(rootPath, image, rootInfo, iv, ivLabel, rootDetection, rootItemId, GetDisplayName(rootInfo, rootDetection), CreateThumbnail(rootInfo), delegate(ImgItemEntry entry)
				{
					entry.Image.Changed = true;
					session.MarkDirty();
				}, null, null, ownsImage: false));
				return entries;
			}
			foreach (WzImageProperty property in image.WzProperties)
			{
				if (!(property is WzSubProperty itemNode))
				{
					continue;
				}
				WzSubProperty? info = itemNode["info"] as WzSubProperty;
				WzSubProperty itemInfo = info ?? itemNode;
				bool hasName = itemInfo["name"] is WzStringProperty;
				bool hasIcon = ResolveCanvas(itemInfo, "icon") != null || ResolveCanvas(itemInfo, "iconRaw") != null;
				if (info == null && !hasName && !hasIcon)
				{
					continue;
				}
				string itemId = itemNode.Name;
				string itemPath = Path.Combine(archivePath, isRoot ? "Root" : category, image.Name, itemId);
				GameFileInfo detection = new GameFileInfo(IsGameAsset: true, sourceRoot, category, itemId, categoryPath + "\\" + image.Name);
				entries.Add(new ImgItemEntry(itemPath, image, itemInfo, iv, ivLabel, detection, itemId, GetDisplayName(itemInfo, detection), CreateThumbnail(itemInfo), delegate(ImgItemEntry entry)
				{
					entry.Image.Changed = true;
					session.MarkDirty();
				}, null, itemNode, ownsImage: false));
			}
		}
		catch
		{
		}
		return entries;
	}

	private void SaveWzArchive(bool askConfirmation)
	{
		bool itemDirty = _archiveSession != null && _archiveSession.IsDirty;
		bool stringDirty = _stringLookup?.IsDirty == true;
		if (!itemDirty && !stringDirty)
		{
			SetStatus("No staged WZ changes to save.");
			return;
		}
		string stringFileName = ((_stringLookup == null) ? null : Path.GetFileName(_stringLookup.Path));
		List<string> lockedReports = new List<string>();
		if (itemDirty)
		{
			IReadOnlyList<WzFileLockInfo> itemLockers = WzFileLockInspector.GetLockingProcesses(_archiveSession!.Path);
			if (itemLockers.Count > 0)
			{
				lockedReports.Add(FormatLockedFile(_archiveSession.FileName, itemLockers));
			}
		}
		if (stringDirty && _stringLookup != null)
		{
			IReadOnlyList<WzFileLockInfo> stringLockers = WzFileLockInspector.GetLockingProcesses(_stringLookup.Path);
			if (stringLockers.Count > 0)
			{
				lockedReports.Add(FormatLockedFile(stringFileName ?? "String.wz", stringLockers));
			}
		}
		if (lockedReports.Count > 0)
		{
			ShowArchiveLockedMessage(string.Join(Environment.NewLine + Environment.NewLine, lockedReports));
			return;
		}
		string files = (itemDirty && stringDirty) ? (_archiveSession!.FileName + " and " + stringFileName) : (itemDirty ? _archiveSession!.FileName : stringFileName);
		if (askConfirmation)
		{
			DialogResult result = MessageBox.Show(this, $"Write changes to {files}?\n\nThis repacks the archive(s). Large archives (such as Character.wz or Map.wz) can take several minutes and need free disk space for a .TEMP file plus a .bak backup.", "Save WZ archive", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (result != DialogResult.Yes)
			{
				return;
			}
		}
		Cursor = Cursors.WaitCursor;
		SplashForm splash = new SplashForm("Saving " + files, "Repacking the archive(s). Large WZ files can take several minutes.");
		splash.Show(this);
		splash.Report("Preparing save...", -1.0);
		try
		{
			if (itemDirty)
			{
				splash.Report("Repacking " + _archiveSession!.FileName + "...", -1.0);
				SetStatus("Repacking " + _archiveSession.FileName + "...");
				_archiveSession.Save();
				foreach (ImgItemEntry item in _items)
				{
					item.IsDirty = false;
				}
			}
			if (stringDirty && _stringLookup != null)
			{
				splash.Report("Saving " + stringFileName + "...", -1.0);
				SetStatus("Saving " + stringFileName + "...");
				_stringLookup.Save();
			}
			splash.Report("Saved.", 1.0);
			SetStatus("Saved " + files);
		}
		catch (Exception ex)
		{
			lockedReports.Clear();
			if (itemDirty && _archiveSession != null)
			{
				IReadOnlyList<WzFileLockInfo> itemLockers = WzFileLockInspector.GetLockingProcesses(_archiveSession.Path);
				if (itemLockers.Count > 0)
				{
					lockedReports.Add(FormatLockedFile(_archiveSession.FileName, itemLockers));
				}
			}
			if (stringDirty && _stringLookup != null)
			{
				IReadOnlyList<WzFileLockInfo> stringLockers = WzFileLockInspector.GetLockingProcesses(_stringLookup.Path);
				if (stringLockers.Count > 0)
				{
					lockedReports.Add(FormatLockedFile(stringFileName ?? "String.wz", stringLockers));
				}
			}
			if (lockedReports.Count > 0)
			{
				ShowArchiveLockedMessage(string.Join(Environment.NewLine + Environment.NewLine, lockedReports));
			}
			else
			{
				MessageBox.Show(this, ex.Message, "WZ save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			splash.Close();
			splash.Dispose();
			Cursor = Cursors.Default;
		}
	}

	private static string FormatLockedFile(string fileName, IReadOnlyList<WzFileLockInfo> lockers)
	{
		string details = string.Join(Environment.NewLine, lockers.Select((WzFileLockInfo locker) => $"- {locker.ApplicationName} (PID {locker.ProcessId})"));
		return fileName + " is in use by:" + Environment.NewLine + details;
	}

	private void ShowArchiveLockedMessage(string details)
	{
		SetStatus("Cannot save: an archive is in use");
		MessageBox.Show(this, details + Environment.NewLine + Environment.NewLine + "Close these programs and try saving again. The archive cannot be replaced while another process has it open.", "WZ archive is in use", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}

	private void CloseArchiveSession()
	{
		if (_saveArchiveMenuItem != null)
		{
			_saveArchiveMenuItem.Enabled = false;
		}
		_stringLookup?.Dispose();
		_stringLookup = null;
		_archiveSession?.Dispose();
		_archiveSession = null;
	}

	private static Panel CreateCardPanel()
	{
		return new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(8),
			BackColor = SurfaceColor
		};
	}

	private static Panel CreatePreviewCard(string title)
	{
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			BackColor = SurfaceRaisedColor
		};
		Label value = new Label
		{
			Dock = DockStyle.Top,
			Height = 24,
			Text = title,
			Font = new Font("Bahnschrift SemiBold", 11f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor
		};
		panel.Controls.Add(value);
		return panel;
	}

	private static TextBox CreateSelectableText(int left, int top, int width, int height, Font font, Color foreColor, bool multiline)
	{
		return new TextBox
		{
			Left = left,
			Top = top,
			Width = width,
			Multiline = multiline,
			WordWrap = multiline,
			ScrollBars = ScrollBars.None,
			Height = height,
			ReadOnly = true,
			BorderStyle = BorderStyle.None,
			BackColor = SurfaceRaisedColor,
			ForeColor = foreColor,
			Font = font,
			ShortcutsEnabled = true,
			TabStop = false,
			Cursor = Cursors.IBeam,
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
	}

	private ContextMenuStrip BuildEditMenu()
	{
		ContextMenuStrip menu = new ContextMenuStrip
		{
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor,
			Renderer = new ModernMenuRenderer(),
			ShowImageMargin = false,
			Font = UiTheme.Font(10f)
		};
		menu.Items.Add(CreateEditMenuItem("Change icon", () =>
		{
			ReplaceCanvasForSelection("icon");
		}));
		menu.Items.Add(CreateEditMenuItem("Change iconRaw", () =>
		{
			ReplaceCanvasForSelection("iconRaw");
		}));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add(CreateEditMenuItem("Add iconReward", AddIconRewardNodesForSelection));
		menu.Items.Add(CreateEditMenuItem("Change iconReward", ReplaceIconRewardForSelection));
		menu.Items.Add(CreateEditMenuItem("Delete iconReward", DeleteIconRewardForSelection));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add(CreateEditMenuItem("Export icon", () =>
		{
			ExportCanvasForSelection("icon");
		}));
		menu.Items.Add(CreateEditMenuItem("Export iconRaw", () =>
		{
			ExportCanvasForSelection("iconRaw");
		}));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add(CreateEditMenuItem("Change ID...", ChangeSelectedItemId));
		menu.Items.Add(CreateEditMenuItem("Change Job...", ChangeSelectedItemJob));
		menu.Items.Add(CreateEditMenuItem("Change iSlot...", ChangeSelectedItemISlot));
		menu.Items.Add(CreateEditMenuItem("Change vSlot...", ChangeSelectedItemVSlot));
		menu.Items.Add(CreateEditMenuItem("Edit info nodes...", EditSelectedItemNodes));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add(CreateEditMenuItem("Change name...", ChangeSelectedItemName));
		menu.Items.Add(CreateEditMenuItem("Copy item ID", CopySelectedIds));
		menu.Items.Add(new ToolStripSeparator());
		ToolStripMenuItem duplicateMenuItem = CreateEditMenuItem("Duplicate item", DuplicateSelectedItem);
		duplicateMenuItem.ShortcutKeyDisplayString = "Ctrl+D";
		menu.Items.Add(duplicateMenuItem);
		menu.Items.Add(CreateEditMenuItem("New item...", AddNewItem));
		return menu;
	}

	private static ToolStripMenuItem CreateEditMenuItem(string text, Action onClick)
	{
		ToolStripMenuItem item = new ToolStripMenuItem(text);
		item.Click += (object? _, EventArgs _) =>
		{
			onClick();
		};
		return item;
	}

	private void ShowEditMenu()
	{
		_editMenu.Show(_editMenuButton, new Point(0, _editMenuButton.Height + 4));
	}

	private void UpdateRewardGlowControls()
	{
		_rewardGlowSwatch.BackColor = IconRewardRenderer.GetGlowColor(_rewardGlowSlider.Value);
		UpdateRewardGlowPreview();
	}

	private void UpdateRewardGlowPreview()
	{
		ImgItemEntry? entry = GetSelectedEntry();
		Bitmap? inner = ((entry == null) ? null : GetItemIconBitmap(entry.Info));
		try
		{
			Bitmap frame = IconRewardRenderer.Render(inner, _rewardGlowSlider.Value, out Point _);
			Image? previous = _rewardGlowPreview.Image;
			_rewardGlowPreview.Image = frame;
			previous?.Dispose();
		}
		catch
		{
		}
		finally
		{
			inner?.Dispose();
		}
	}

	private static Bitmap? GetItemIconBitmap(WzSubProperty info)
	{
		WzCanvasProperty? canvas = ResolveCanvas(info, "iconRaw") ?? ResolveCanvas(info, "icon");
		if (canvas == null)
		{
			return null;
		}
		return WzAnimationReader.GetCanvasBitmap(canvas);
	}

	private static Label CreateFilterCaption(string text)
	{
		return new Label
		{
			AutoSize = true,
			Margin = new Padding(0, 8, 6, 0),
			ForeColor = SubtleTextColor,
			Text = text
		};
	}

	private static FlowLayoutPanel CreateFilterGroup(params Control[] controls)
	{
		FlowLayoutPanel group = new FlowLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			WrapContents = false,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = new Padding(0, 0, 12, 6),
			Padding = Padding.Empty,
			BackColor = SurfaceColor
		};
		group.Controls.AddRange(controls);
		return group;
	}

	private static NumericUpDown AddStatInput(Control parent, string labelText, int left, int top)
	{
		Label value = new Label
		{
			Left = left,
			Top = top,
			AutoSize = true,
			Text = labelText,
			ForeColor = SubtleTextColor
		};
		parent.Controls.Add(value);
		NumericUpDown numericUpDown = new NumericUpDown
		{
			Left = left,
			Top = top + 24,
			Width = 150,
			Minimum = -999999m,
			Maximum = 999999m,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = SurfaceColor,
			ForeColor = TextColor
		};
		parent.Controls.Add(numericUpDown);
		return numericUpDown;
	}

	private static void ApplyButtonStyle(Button button, bool primary)
	{
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 1;
		button.FlatAppearance.BorderColor = (primary ? AccentColor : BorderColor);
		button.BackColor = (primary ? AccentColor : AccentMutedColor);
		button.ForeColor = (primary ? Color.White : TextColor);
		button.Font = new Font("Bahnschrift SemiBold", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
		button.Cursor = Cursors.Hand;
	}

	private static Rectangle GetStartupBounds()
	{
		Rectangle workingArea = Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
		return new Rectangle(workingArea.Left, workingArea.Top, Math.Max(workingArea.Width, 1500), Math.Max(workingArea.Height, 920));
	}

	private void OpenFolder()
	{
		if (!ConfirmDiscardChanges())
		{
			return;
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select a folder that contains .img files"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
			{
				LoadFolder(folderBrowserDialog.SelectedPath);
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private void LoadFolder(string folderPath)
	{
		ClearLoadedItems();
		try
		{
			string[] array = (from path in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
				where path.EndsWith(".img", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase)
				select path).ToArray();
			if (array.Length == 0)
			{
				throw new InvalidOperationException("No .img or .img.xml files were found in that folder.");
			}
			_loadingSelection = true;
			int num = 0;
			foreach (string item in array.OrderBy(Path.GetFileName))
			{
				foreach (ImgItemEntry item2 in TryLoadItems(item))
				{
					_items.Add(item2);
					num++;
				}
			}
			_currentFolderPath = folderPath;
			if (MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95)
			{
				MapleEditorVersionSettings.Settings.V95WzFolder = folderPath;
			}
			else
			{
				MapleEditorVersionSettings.Settings.V83DataFolder = folderPath;
			}
			MapleEditorVersionSettings.Save();
			string? stringWz = Path.Combine(folderPath, "String.wz");
			if (!File.Exists(stringWz))
			{
				string? parent = Path.GetDirectoryName(folderPath);
				stringWz = ((parent == null) ? null : Path.Combine(parent, "String.wz"));
			}
			if (stringWz != null && File.Exists(stringWz))
			{
				_stringLookup = WzStringLookup.TryOpen(stringWz);
			}
			_saveAllButton.Enabled = num > 0;
			if (num == 0)
			{
				throw new InvalidOperationException("No readable item .img or .img.xml files with an info section were found in that folder.");
			}
			RefreshCategoryFilterOptions();
			RefreshItemBrowser();
			_header.RightText = folderPath;
			SetStatus($"Loaded {num} item files from {folderPath}");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Folder load failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_loadingSelection = false;
			LoadSelectedItem();
		}
	}

	private IEnumerable<ImgItemEntry> TryLoadItems(string path)
	{
		try
		{
			return path.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) ? TryLoadXmlItems(path) : TryLoadBinaryItems(path);
		}
		catch
		{
			return Array.Empty<ImgItemEntry>();
		}
	}

	private IEnumerable<ImgItemEntry> TryLoadBinaryItems(string path)
	{
		WzImage wzImage = LoadImgWithDetectedIv(path, out byte[] iv, out string ivLabel);
		if (!(wzImage["info"] is WzSubProperty info))
		{
			List<ImgItemEntry> list = TryLoadGroupedBinaryItems(path, wzImage, iv, ivLabel);
			if (list.Count == 0)
			{
				wzImage.Dispose();
			}
			return list;
		}
		GameFileInfo gameFileInfo = GameFileDetection.Detect(path);
		string itemId = gameFileInfo.ItemId;
		string displayName = GetDisplayName(info, gameFileInfo);
		Bitmap thumbnail = CreateThumbnail(info);
		return new _003C_003Ez__ReadOnlySingleElementList<ImgItemEntry>(new ImgItemEntry(path, wzImage, info, iv, ivLabel, gameFileInfo, itemId, displayName, thumbnail, (ImgItemEntry entry) =>
		{
			SaveBinaryItem(entry);
		}));
	}

	private List<ImgItemEntry> TryLoadGroupedBinaryItems(string path, WzImage rootImage, byte[] iv, string ivLabel)
	{
		List<ImgItemEntry> list = new List<ImgItemEntry>();
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		SharedImageLifetime lifetime = new SharedImageLifetime(rootImage);
		foreach (WzImageProperty wzProperty in rootImage.WzProperties)
		{
			if (wzProperty is WzSubProperty wzSubProperty && wzSubProperty["info"] is WzSubProperty wzSubProperty2)
			{
				string name = wzSubProperty.Name;
				WzSubProperty info = (wzSubProperty["info"] as WzSubProperty) ?? wzSubProperty2;
				GameFileInfo detection = new GameFileInfo(IsGameAsset: false, string.Empty, fileNameWithoutExtension, name, "Grouped IMG " + Path.GetFileName(path));
				Bitmap thumbnail = CreateThumbnail(info);
				lifetime.AddReference();
				list.Add(new ImgItemEntry(path, rootImage, info, iv, ivLabel, detection, name, GetDisplayName(info, detection), thumbnail, (ImgItemEntry entry) =>
				{
					SaveGroupedBinaryItem(entry, rootImage, path, iv);
				}, () =>
				{
					lifetime.Release();
				}, wzSubProperty, ownsImage: false, isGroupedEntry: true));
			}
		}
		return list;
	}

	private IEnumerable<ImgItemEntry> TryLoadXmlItems(string path)
	{
		WzImage rootImage = _xmlDocumentService.LoadFromXml(path, out var _);
		List<ImgItemEntry> list = new List<ImgItemEntry>();
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		if (rootImage["info"] is WzSubProperty info)
		{
			GameFileInfo gameFileInfo = GameFileDetection.Detect(path);
			Bitmap thumbnail = CreateThumbnail(info);
			list.Add(new ImgItemEntry(path, rootImage, info, Array.Empty<byte>(), "XML", gameFileInfo, gameFileInfo.ItemId, GetDisplayName(info, gameFileInfo), thumbnail, (ImgItemEntry entry) =>
			{
				SaveXmlItem(entry);
			}));
			return list;
		}
		SharedImageLifetime lifetime = new SharedImageLifetime(rootImage);
		foreach (WzImageProperty wzProperty in rootImage.WzProperties)
		{
			if (wzProperty is WzSubProperty wzSubProperty && wzSubProperty["info"] is WzSubProperty info2)
			{
				string name = wzSubProperty.Name;
				GameFileInfo detection = new GameFileInfo(IsGameAsset: false, string.Empty, fileNameWithoutExtension, name, "Grouped XML " + Path.GetFileName(path));
				Bitmap thumbnail2 = CreateThumbnail(info2);
				lifetime.AddReference();
				list.Add(new ImgItemEntry(path, rootImage, info2, Array.Empty<byte>(), "XML", detection, name, GetDisplayName(info2, detection), thumbnail2, (ImgItemEntry entry) =>
				{
					SaveGroupedXmlItem(entry, rootImage, path);
				}, () =>
				{
					lifetime.Release();
				}, wzSubProperty, ownsImage: false, isGroupedEntry: true));
			}
		}
		if (list.Count == 0)
		{
			rootImage.Dispose();
		}
		return list;
	}

	private static WzImage LoadImgWithDetectedIv(string path, out byte[] iv, out string ivLabel)
	{
		foreach (var candidateVersion in GetCandidateVersions())
		{
			WzImgDeserializer wzImgDeserializer = new WzImgDeserializer(freeResources: true);
			WzImage wzImage = wzImgDeserializer.WzImageFromIMGFile(path, WzTool.GetIvByMapleVersion(candidateVersion.version), Path.GetFileName(path), out var successfullyParsedImage);
			if (!successfullyParsedImage)
			{
				wzImage.Dispose();
				continue;
			}
			iv = WzTool.GetIvByMapleVersion(candidateVersion.version);
			ivLabel = candidateVersion.label;
			return wzImage;
		}
		throw new InvalidOperationException("Could not parse IMG with known IV profiles.");
	}

	private static IEnumerable<(WzMapleVersion version, string label)> GetCandidateVersions()
	{
		yield return (version: WzMapleVersion.GMS, label: "GMS");
		yield return (version: WzMapleVersion.EMS, label: "EMS/MSEA");
		yield return (version: WzMapleVersion.BMS, label: "BMS/Classic");
	}

	private string GetDisplayName(WzSubProperty info, GameFileInfo detection)
	{
		string text = (info["name"] as WzStringProperty)?.Value;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (_stringLookup != null && !string.IsNullOrWhiteSpace(detection.ItemId))
		{
			string? stringName = _stringLookup.GetItemName(detection.Category, detection.ItemId);
			if (!string.IsNullOrWhiteSpace(stringName))
			{
				return stringName;
			}
		}
		if (detection.IsGameAsset)
		{
			return string.IsNullOrWhiteSpace(detection.Category) ? detection.ItemId : (detection.Category + " " + detection.ItemId);
		}
		return detection.ItemId;
	}

	private static Bitmap CreateThumbnail(WzSubProperty info)
	{
		WzCanvasProperty wzCanvasProperty = ResolveCanvas(info, "iconRaw") ?? ResolveCanvas(info, "icon");
		if (wzCanvasProperty == null)
		{
			Bitmap bitmap = new Bitmap(48, 48);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Transparent);
			return bitmap;
		}
		using Bitmap? original = WzAnimationReader.GetCanvasBitmap(wzCanvasProperty);
		if (original == null)
		{
			Bitmap empty = new Bitmap(48, 48);
			using (Graphics graphics = Graphics.FromImage(empty))
			{
				graphics.Clear(Color.Transparent);
			}
			return empty;
		}
		return new Bitmap(original, new Size(48, 48));
	}

	private void LoadSelectedItem()
	{
		SetSelectionState(GetSelectedEntries());
	}

	private ImgItemEntry? GetSelectedEntry()
	{
		return _assetGrid.PrimaryRow?.Tag as ImgItemEntry;
	}

	private List<ImgItemEntry> GetSelectedEntries()
	{
		return _assetGrid.SelectedRows.Select((AssetGridRow row) => row.Tag).OfType<ImgItemEntry>().ToList();
	}

	private void SetSelectionState(List<ImgItemEntry> entries)
	{
		ImgItemEntry imgItemEntry = entries.FirstOrDefault();
		bool enabled = imgItemEntry != null;
		_applyButton.Enabled = enabled;
		_saveAsImgButton.Enabled = enabled;
		_saveAsXmlButton.Enabled = enabled;
		_saveCurrentButton.Enabled = enabled;
		_editMenuButton.Enabled = enabled;
		foreach (ToolStripItem item in _editMenu.Items)
		{
			item.Enabled = enabled;
		}
		_copyIdsMenuItem.Enabled = enabled;
		if (imgItemEntry == null)
		{
			_itemNameLabel.Text = "No item selected";
			_itemContextLabel.Text = ((_currentFolderPath == null) ? "Detected context" : _currentFolderPath);
			_itemEquipJobsLabel.Text = "Equip Jobs: -";
			_itemPathLabel.Text = string.Empty;
			SetPreview(null);
			SetRewardPreviewForInfo(null);
			SetStatInputs(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, isCash: false, isTradeLocked: false);
			UpdateRewardGlowPreview();
			return;
		}
		_itemNameLabel.Text = ((entries.Count == 1) ? imgItemEntry.DisplayName : $"{entries.Count} items selected");
		_itemContextLabel.Text = ((entries.Count == 1) ? $"{imgItemEntry.Detection.DisplayContext} | Item ID {imgItemEntry.ItemId} | {imgItemEntry.IvLabel}" : "Mass edit mode | changes apply to every selected item");
		if (entries.Count == 1)
		{
			_itemEquipJobsLabel.Text = "Equip Jobs: " + GetEquipJobDescription(imgItemEntry.Info);
		}
		else
		{
			string[] array = entries.Select((ImgItemEntry i) => GetEquipJobDescription(i.Info)).Distinct(StringComparer.Ordinal).Take(2)
				.ToArray();
			_itemEquipJobsLabel.Text = ((array.Length == 1) ? ("Equip Jobs: " + array[0]) : "Equip Jobs: Mixed");
		}
		_itemPathLabel.Text = ((entries.Count == 1) ? imgItemEntry.FilePath : (string.Join(Environment.NewLine, from i in entries.Take(3)
			select i.FilePath) + ((entries.Count > 3) ? (Environment.NewLine + "...") : string.Empty)));
		SetPreview(imgItemEntry.Thumbnail);
		SetRewardPreviewForInfo(imgItemEntry.Info);
		SetStatInputs(GetInfoInt(imgItemEntry.Info, "incSTR"), GetInfoInt(imgItemEntry.Info, "incDEX"), GetInfoInt(imgItemEntry.Info, "incINT"), GetInfoInt(imgItemEntry.Info, "incLUK"), GetInfoInt(imgItemEntry.Info, "incPAD"), GetInfoInt(imgItemEntry.Info, "incMAD"), GetInfoInt(imgItemEntry.Info, "incSpeed"), GetInfoInt(imgItemEntry.Info, "incJump"), GetInfoInt(imgItemEntry.Info, "incMHP"), GetInfoInt(imgItemEntry.Info, "incMMP"), GetInfoInt(imgItemEntry.Info, "incACC"), GetInfoInt(imgItemEntry.Info, "incEVA"), GetInfoInt(imgItemEntry.Info, "tuc"), GetInfoInt(imgItemEntry.Info, "reqLevel"), GetInfoInt(imgItemEntry.Info, "cash") != 0, GetInfoInt(imgItemEntry.Info, "tradeBlock") != 0);
		UpdateRewardGlowPreview();
	}

	private static int GetInfoInt(WzSubProperty info, string name)
	{
		WzImageProperty wzImageProperty = info[name];
		if (1 == 0)
		{
		}
		int result;
		if (!(wzImageProperty is WzIntProperty wzIntProperty))
		{
			if (!(wzImageProperty is WzShortProperty wzShortProperty))
			{
				if (wzImageProperty is WzLongProperty wzLongProperty)
				{
					if (wzLongProperty.Value > int.MaxValue)
					{
						result = int.MaxValue;
					}
					else
					{
						WzLongProperty wzLongProperty2 = wzLongProperty;
						if (wzLongProperty2.Value < int.MinValue)
						{
							result = int.MinValue;
						}
						else
						{
							WzLongProperty wzLongProperty3 = wzLongProperty;
							result = (int)wzLongProperty3.Value;
						}
					}
				}
				else
				{
					result = 0;
				}
			}
			else
			{
				result = wzShortProperty.Value;
			}
		}
		else
		{
			result = wzIntProperty.Value;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetEquipJobDescription(WzSubProperty info)
	{
		int infoInt = GetInfoInt(info, "reqJob");
		int infoInt2 = GetInfoInt(info, "reqSpecJob");
		List<string> list = new List<string>();
		if ((infoInt & 1) != 0)
		{
			list.Add("Warrior");
		}
		if ((infoInt & 2) != 0)
		{
			list.Add("Magician");
		}
		if ((infoInt & 4) != 0)
		{
			list.Add("Bowman");
		}
		if ((infoInt & 8) != 0)
		{
			list.Add("Thief");
		}
		if ((infoInt & 0x10) != 0)
		{
			list.Add("Pirate");
		}
		int num = infoInt & -32;
		if (num != 0)
		{
			list.Add($"ExtraMask 0x{num:X}");
		}
		string text;
		if (infoInt != 0)
		{
			text = ((list.Count > 0) ? string.Join(", ", list) : $"Mask {infoInt}");
		}
		else
		{
			text = "All jobs";
		}
		if (infoInt2 != 0)
		{
			return $"{text} | Spec {infoInt2}";
		}
		return text;
	}

	private void SetStatInputs(int str, int dex, int intel, int luk, int pad, int mad, int speed, int jump, int hp, int mp, int acc, int avoid, int slots, int reqLevel, bool isCash, bool isTradeLocked)
	{
		_strInput.Value = str;
		_dexInput.Value = dex;
		_intInput.Value = intel;
		_lukInput.Value = luk;
		_padInput.Value = pad;
		_madInput.Value = mad;
		_speedInput.Value = speed;
		_jumpInput.Value = jump;
		_hpInput.Value = hp;
		_mpInput.Value = mp;
		_accInput.Value = acc;
		_avoidInput.Value = avoid;
		_slotsInput.Value = slots;
		_reqLevelInput.Value = reqLevel;
		_cashCheckBox.Checked = isCash;
		_tradeLockCheckBox.Checked = isTradeLocked;
	}

	private void ApplySelectedStats()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		foreach (ImgItemEntry item in selectedEntries)
		{
			SetOrCreateInfoInt(item.Info, "incSTR", decimal.ToInt32(_strInput.Value));
			SetOrCreateInfoInt(item.Info, "incDEX", decimal.ToInt32(_dexInput.Value));
			SetOrCreateInfoInt(item.Info, "incINT", decimal.ToInt32(_intInput.Value));
			SetOrCreateInfoInt(item.Info, "incLUK", decimal.ToInt32(_lukInput.Value));
			SetOrCreateInfoInt(item.Info, "incPAD", decimal.ToInt32(_padInput.Value));
			SetOrCreateInfoInt(item.Info, "incMAD", decimal.ToInt32(_madInput.Value));
			SetOrCreateInfoInt(item.Info, "incSpeed", decimal.ToInt32(_speedInput.Value));
			SetOrCreateInfoInt(item.Info, "incJump", decimal.ToInt32(_jumpInput.Value));
			SetOrCreateInfoInt(item.Info, "incMHP", decimal.ToInt32(_hpInput.Value));
			SetOrCreateInfoInt(item.Info, "incMMP", decimal.ToInt32(_mpInput.Value));
			SetOrCreateInfoInt(item.Info, "incACC", decimal.ToInt32(_accInput.Value));
			SetOrCreateInfoInt(item.Info, "incEVA", decimal.ToInt32(_avoidInput.Value));
			SetOrCreateInfoInt(item.Info, "tuc", decimal.ToInt32(_slotsInput.Value));
			SetOrCreateInfoInt(item.Info, "reqLevel", decimal.ToInt32(_reqLevelInput.Value));
			SetOrCreateInfoInt(item.Info, "cash", _cashCheckBox.Checked ? 1 : 0);
			SetOrCreateInfoInt(item.Info, "tradeBlock", _tradeLockCheckBox.Checked ? 1 : 0);
			item.Image.Changed = true;
			item.IsDirty = true;
		}
		SetStatus((selectedEntries.Count == 1) ? ("Updated bonus stats for " + selectedEntries[0].DisplayName) : $"Updated {selectedEntries.Count} selected items");
	}

	private static void SetOrCreateInfoInt(WzSubProperty info, string name, int value)
	{
		if (info[name] is WzIntProperty wzIntProperty)
		{
			wzIntProperty.Value = value;
			return;
		}
		info.RemoveProperty(name);
		info.AddProperty(new WzIntProperty(name, value));
	}

	private static void SetOrCreateInfoString(WzSubProperty info, string name, string value)
	{
		if (info[name] is WzStringProperty wzStringProperty)
		{
			wzStringProperty.Value = value;
			return;
		}
		info.RemoveProperty(name);
		info.AddProperty(new WzStringProperty(name, value));
	}

	private void ChangeSelectedItemName()
	{
		ImgItemEntry? entry = GetSelectedEntry();
		if (entry == null)
		{
			MessageBox.Show(this, "Select an item to rename first.", "Change name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		string currentName = (entry.Info["name"] as WzStringProperty)?.Value ?? entry.DisplayName;
		using InputDialog dialog = new InputDialog("Change name", "Item name", currentName);
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		string newName = dialog.Value.Trim();
		if (string.IsNullOrWhiteSpace(newName))
		{
			MessageBox.Show(this, "Item name cannot be empty.", "Change name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		SetOrCreateInfoString(entry.Info, "name", newName);
		entry.DisplayName = newName;
		entry.Image.Changed = true;
		entry.IsDirty = true;
		RefreshItemBrowser();
		SetStatus("Renamed item to " + newName);
	}

	private void ChangeSelectedItemId()
	{
		ImgItemEntry? entry = GetSelectedEntry();
		if (entry == null)
		{
			MessageBox.Show(this, "Select an item to change the ID first.", "Change ID", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		string oldId = entry.ItemId;
		string oldDisplayName = entry.DisplayName;
		using InputDialog dialog = new InputDialog("Change ID", "Item ID (use the same digit format as the current ID)", entry.ItemId);
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		string newId = dialog.Value.Trim();
		if (string.IsNullOrWhiteSpace(newId) || newId.IndexOfAny(new char[3] { '/', '\\', ':' }) >= 0)
		{
			MessageBox.Show(this, "Item ID cannot be empty or contain path characters.", "Change ID", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (string.Equals(newId, entry.ItemId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (_items.Any((ImgItemEntry item) => !ReferenceEquals(item, entry) && string.Equals(item.ItemId, newId, StringComparison.OrdinalIgnoreCase)))
		{
			MessageBox.Show(this, "Another loaded item already uses ID " + newId + ".", "Change ID", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		try
		{
			string folder = Path.GetDirectoryName(entry.FilePath) ?? string.Empty;
			if (entry.ItemProperty != null)
			{
				if (entry.Image[newId] != null)
				{
					throw new InvalidOperationException("The image already contains a node named " + newId + ".");
				}
				entry.ItemProperty.Name = newId;
			}
			else
			{
				string imageExtension = (entry.Image.Name.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) ? ".img.xml" : ".img");
				string fileExtension = (entry.FilePath.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) ? ".img.xml" : ".img");
				string newImageName = newId + imageExtension;
				if (_archiveSession != null && entry.Image.Parent is WzDirectory parentDirectory && parentDirectory.WzImages.Any((WzImage existing) => !ReferenceEquals(existing, entry.Image) && string.Equals(existing.Name, newImageName, StringComparison.OrdinalIgnoreCase)))
				{
					throw new InvalidOperationException("The archive folder already contains " + newImageName + ".");
				}
				else
				{
					string newFilePath = Path.Combine(folder, newId + fileExtension);
					if (File.Exists(newFilePath))
					{
						throw new InvalidOperationException("A file already exists at " + newFilePath + ".");
					}
					if (File.Exists(entry.FilePath))
					{
						File.Move(entry.FilePath, newFilePath);
					}
				}
				entry.Image.Name = newImageName;
				entry.FilePath = Path.Combine(folder, newId + fileExtension);
			}
			UpdateItemReferences(entry, oldId, newId);
			bool stringUpdated = UpdateStringOnRename(entry.Detection.Category, oldId, newId, oldDisplayName);
			entry.ItemId = newId;
			entry.Detection = new GameFileInfo(IsGameAsset: true, entry.Detection.SourceRoot, entry.Detection.Category, newId, entry.Detection.DisplayContext);
			entry.DisplayName = GetDisplayName(entry.Info, entry.Detection);
			entry.Image.Changed = true;
			entry.IsDirty = true;
			_archiveSession?.MarkDirty();
			RefreshItemBrowser();
			SetStatus("Changed item ID to " + newId + (stringUpdated ? " and updated String.wz" : string.Empty));
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Change ID failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private bool UpdateStringOnRename(string category, string oldId, string newId, string fallbackName)
	{
		if (_stringLookup == null)
		{
			return false;
		}
		if (_stringLookup.MoveItemText(category, oldId, newId))
		{
			return true;
		}
		return _stringLookup.SetItemText(category, newId, fallbackName);
	}

	private bool UpdateStringOnCreate(string category, string sourceId, string newId, string fallbackName)
	{
		if (_stringLookup == null)
		{
			return false;
		}
		if (_stringLookup.CopyItemText(category, sourceId, newId, removeSource: false))
		{
			return true;
		}
		return _stringLookup.SetItemText(category, newId, fallbackName);
	}

	private static void UpdateItemReferences(ImgItemEntry entry, string oldId, string newId)
	{
		if (string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		IPropertyContainer? container = entry.ItemProperty ?? (IPropertyContainer?)entry.Image;
		if (container != null)
		{
			UpdatePropertyReferences(container, oldId, newId);
		}
	}

	private static void UpdatePropertyReferences(IPropertyContainer container, string oldId, string newId)
	{
		int? oldValue = (int.TryParse(oldId, out int parsedOld) ? parsedOld : ((int?)null));
		int? newValue = (int.TryParse(newId, out int parsedNew) ? parsedNew : ((int?)null));
		foreach (WzImageProperty property in container.WzProperties)
		{
			switch (property)
			{
			case WzStringProperty stringProperty:
				stringProperty.Value = ReplaceIdReference(stringProperty.Value, oldId, newId);
				break;
			case WzUOLProperty uolProperty:
				uolProperty.Value = ReplaceIdReference(uolProperty.Value, oldId, newId);
				break;
			case WzIntProperty intProperty when oldValue.HasValue && newValue.HasValue && IsIdReferenceName(intProperty.Name) && intProperty.Value == oldValue.Value:
				intProperty.Value = newValue.Value;
				break;
			case WzShortProperty shortProperty when oldValue.HasValue && newValue.HasValue && IsIdReferenceName(shortProperty.Name) && shortProperty.Value == oldValue.Value:
				shortProperty.Value = (short)newValue.Value;
				break;
			case WzLongProperty longProperty when oldValue.HasValue && newValue.HasValue && IsIdReferenceName(longProperty.Name) && longProperty.Value == oldValue.Value:
				longProperty.Value = newValue.Value;
				break;
			}
			if (property is IPropertyContainer child)
			{
				UpdatePropertyReferences(child, oldId, newId);
			}
		}
	}

	private static bool IsIdReferenceName(string name)
	{
		return IdReferenceNames.Contains(name);
	}

	private static string ReplaceIdReference(string value, string oldId, string newId)
	{
		if (string.IsNullOrEmpty(value) || !value.Contains(oldId, StringComparison.Ordinal))
		{
			return value;
		}
		string pattern = "(?<![0-9])" + Regex.Escape(oldId) + "(?![0-9])";
		return Regex.Replace(value, pattern, newId);
	}

	private void ChangeSelectedItemJob()
	{
		PromptSelectedInfoInt("Change Job", "Job mask: 0 = all jobs, 1 = Warrior, 2 = Magician, 4 = Bowman, 8 = Thief, 16 = Pirate. Add values together.", "reqJob");
	}

	private void ChangeSelectedItemISlot()
	{
		PromptSelectedInfoInt("Change iSlot", "iSlot value", "iSlot");
	}

	private void ChangeSelectedItemVSlot()
	{
		PromptSelectedInfoInt("Change vSlot", "vSlot value", "vSlot");
	}

	private void PreviewSelectedAnimations()
	{
		ImgItemEntry? entry = GetSelectedEntry();
		if (entry == null)
		{
			MessageBox.Show(this, "Select an item first.", "Animation preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		IPropertyContainer root = entry.ItemProperty ?? (IPropertyContainer)entry.Image;
		AnimationViewerForm.ShowFor(this, root, "Animations - " + entry.ItemId);
	}

	private void PromptSelectedInfoInt(string title, string label, string nodeName)
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			MessageBox.Show(this, "Select one or more items first.", title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		int current = GetInfoInt(selectedEntries[0].Info, nodeName);
		using InputDialog dialog = new InputDialog(title, label, current.ToString());
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		if (!int.TryParse(dialog.Value.Trim(), out int value))
		{
			MessageBox.Show(this, "Enter a whole number.", title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		foreach (ImgItemEntry entry in selectedEntries)
		{
			SetOrCreateInfoInt(entry.Info, nodeName, value);
			entry.Image.Changed = true;
			entry.IsDirty = true;
		}
		_archiveSession?.MarkDirty();
		RefreshItemBrowser();
		SetStatus((selectedEntries.Count == 1) ? $"Set {nodeName} to {value}" : $"Set {nodeName} for {selectedEntries.Count} items");
	}

	private void EditSelectedItemNodes()
	{
		ImgItemEntry? entry = GetSelectedEntry();
		if (entry == null)
		{
			MessageBox.Show(this, "Select an item first.", "Edit WZ nodes", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		using InfoNodeEditorForm dialog = new InfoNodeEditorForm(entry.Info, entry.ItemId);
		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			entry.Image.Changed = true;
			entry.IsDirty = true;
			_archiveSession?.MarkDirty();
			entry.DisplayName = GetDisplayName(entry.Info, entry.Detection);
			RefreshItemBrowser();
			SetStatus("Updated WZ nodes for " + entry.DisplayName);
		}
	}

	private void DuplicateSelectedItem()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			MessageBox.Show(this, "Select an item to duplicate first.", "Duplicate item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		CreateItemFromTemplate(selectedEntries[0], blank: false);
	}

	private void AddNewItem()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			MessageBox.Show(this, "Select an existing item first so its category and ID range can be used.", "New item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		CreateItemFromTemplate(selectedEntries[0], blank: true);
	}

	private void CreateItemFromTemplate(ImgItemEntry template, bool blank)
	{
		string category = template.Detection.Category;
		string suggestedId = SuggestNewItemId(category, template.ItemId);
		Func<string, string> validate = ((template.ItemProperty != null) ? (Func<string, string>)((string text) => ValidateNewGroupedItemId(text, template)) : (Func<string, string>)((string text) => ValidateNewItemId(text, category, Path.GetDirectoryName(template.FilePath))));
		string text2 = NewItemIdDialog.Show(this, blank ? "New item" : "Duplicate item", blank ? "Create a new item with blank icons in the same category. Multiple items are supported." : "Create a copy of the selected item with a new ID. All icons and stats are cloned.", suggestedId, category, validate);
		if (text2 == null)
		{
			return;
		}
		string newId = ((template.ItemProperty != null) ? text2.Trim() : NormalizeItemId(text2));
		try
		{
			if (template.ItemProperty != null)
			{
				CreateGroupedItemFromTemplate(template, newId, blank);
				return;
			}
			WzImage newImage = (blank ? CreateBlankItemImage(newId) : CloneItemImage(template, newId));
			if (!(newImage["info"] is WzSubProperty info))
			{
				throw new InvalidOperationException("The new item does not contain an info section.");
			}
			string sourceRoot = _archiveSession?.FileName ?? (string.IsNullOrWhiteSpace(template.Detection.SourceRoot) ? "Character.wz" : template.Detection.SourceRoot);
			GameFileInfo detection = new GameFileInfo(IsGameAsset: true, sourceRoot, category, newId, string.IsNullOrEmpty(category) ? sourceRoot : (sourceRoot + "\\" + category));
			string displayName = GetDisplayName(info, detection);
			Bitmap thumbnail = CreateThumbnail(info);
			ImgItemEntry entry;
			if (_archiveSession != null)
			{
				WzDirectory? directory = (string.IsNullOrEmpty(category) ? _archiveSession.File.WzDirectory : _archiveSession.GetDirectory(category));
				if (directory == null)
				{
					throw new InvalidOperationException("Could not find category " + category + " in the archive.");
				}
				directory.AddImage(newImage);
				_archiveSession.MarkDirty();
				string syntheticPath = Path.Combine(_archiveSession.Path, string.IsNullOrEmpty(category) ? "Root" : category, newId + ".img");
				entry = new ImgItemEntry(syntheticPath, newImage, info, WzTool.GetIvByMapleVersion(WzMapleVersion.GMS), $"WZ v{_archiveSession.GameVersion}", detection, newId, displayName, thumbnail, delegate(ImgItemEntry e)
				{
					e.Image.Changed = true;
					_archiveSession.MarkDirty();
				}, null, null, ownsImage: false);
				entry.IsDirty = true;
			}
			else
			{
				string folder = Path.GetDirectoryName(template.FilePath) ?? throw new InvalidOperationException("Could not resolve the item folder.");
				bool isXml = template.FilePath.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) || template.Iv.Length == 0;
				string filePath = Path.Combine(folder, newId + (isXml ? ".img.xml" : ".img"));
				newImage.MarkWzImageAsParsed();
				newImage.Changed = true;
				if (isXml)
				{
					_xmlDocumentService.SaveToXml(newImage, filePath);
				}
				else
				{
					new WzImgSerializer(template.Iv).SerializeImage(newImage, filePath);
				}
				entry = new ImgItemEntry(filePath, newImage, info, (isXml ? Array.Empty<byte>() : template.Iv), (isXml ? "XML" : template.IvLabel), detection, newId, displayName, thumbnail, delegate(ImgItemEntry e)
				{
					if (isXml)
					{
						SaveXmlItem(e);
					}
					else
					{
						SaveBinaryItem(e);
					}
				});
				entry.IsDirty = false;
			}
			UpdateItemReferences(entry, template.ItemId, newId);
			bool stringUpdated = UpdateStringOnCreate(detection.Category, template.ItemId, newId, template.DisplayName);
			entry.DisplayName = GetDisplayName(info, detection);
			_items.Add(entry);
			RefreshCategoryFilterOptions();
			RefreshItemBrowser();
			_assetGrid.SelectRow(entry);
			SetStatus((blank ? "Created new item " : "Duplicated item as ") + newId + (stringUpdated ? " and copied String.wz text" : string.Empty));
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Could not create item", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CreateGroupedItemFromTemplate(ImgItemEntry template, string newId, bool blank)
	{
		WzSubProperty itemNode = (blank ? CreateBlankItemNode(newId) : ((WzSubProperty)template.ItemProperty!.DeepClone()));
		itemNode.Name = newId;
		template.Image.AddProperty(itemNode);
		WzSubProperty info = (itemNode["info"] as WzSubProperty) ?? itemNode;
		string sourceRoot = _archiveSession?.FileName ?? template.Detection.SourceRoot;
		GameFileInfo detection = new GameFileInfo(IsGameAsset: true, sourceRoot, template.Detection.Category, newId, template.Detection.DisplayContext);
		bool isXml = template.Iv.Length == 0;
		ImgItemEntry entry = new ImgItemEntry(template.FilePath, template.Image, info, template.Iv, template.IvLabel, detection, newId, GetDisplayName(info, detection), CreateThumbnail(info), delegate(ImgItemEntry e)
		{
			if (_archiveSession != null)
			{
				e.Image.Changed = true;
				_archiveSession.MarkDirty();
			}
			else if (isXml)
			{
				SaveGroupedXmlItem(e, template.Image, template.FilePath);
			}
			else
			{
				SaveGroupedBinaryItem(e, template.Image, template.FilePath, template.Iv);
			}
		}, null, itemNode, ownsImage: false);
		entry.Image.Changed = true;
		_archiveSession?.MarkDirty();
		entry.IsDirty = true;
		UpdateItemReferences(entry, template.ItemId, newId);
		bool stringUpdated = UpdateStringOnCreate(template.Detection.Category, template.ItemId, newId, template.DisplayName);
		entry.DisplayName = GetDisplayName(entry.Info, detection);
		_items.Add(entry);
		RefreshCategoryFilterOptions();
		RefreshItemBrowser();
		_assetGrid.SelectRow(entry);
		SetStatus((blank ? "Created new item " : "Duplicated item as ") + newId + (stringUpdated ? " and copied String.wz text" : string.Empty));
	}

	private static WzSubProperty CreateBlankItemNode(string newId)
	{
		WzSubProperty node = new WzSubProperty(newId);
		WzSubProperty info = new WzSubProperty("info");
		info.AddProperty(new WzIntProperty("reqLevel", 0));
		info.AddProperty(new WzIntProperty("reqJob", 0));
		info.AddProperty(new WzIntProperty("tuc", 0));
		info.AddProperty(new WzIntProperty("cash", 0));
		info.AddProperty(new WzIntProperty("tradeBlock", 0));
		info.AddProperty(CreateTransparentCanvas("icon", 32, 32));
		info.AddProperty(CreateTransparentCanvas("iconRaw", 32, 32));
		node.AddProperty(info);
		return node;
	}

	private static WzImage CloneItemImage(ImgItemEntry template, string newId)
	{
		WzImage wzImage = template.Image.DeepClone();
		wzImage.Name = newId + ".img";
		wzImage.Changed = true;
		return wzImage;
	}

	private static WzImage CreateBlankItemImage(string newId)
	{
		WzImage wzImage = new WzImage(newId + ".img");
		WzSubProperty wzSubProperty = new WzSubProperty("info");
		wzSubProperty.AddProperty(new WzIntProperty("reqLevel", 0));
		wzSubProperty.AddProperty(new WzIntProperty("reqJob", 0));
		wzSubProperty.AddProperty(new WzIntProperty("tuc", 0));
		wzSubProperty.AddProperty(new WzIntProperty("cash", 0));
		wzSubProperty.AddProperty(new WzIntProperty("tradeBlock", 0));
		wzSubProperty.AddProperty(CreateTransparentCanvas("icon", 32, 32));
		wzSubProperty.AddProperty(CreateTransparentCanvas("iconRaw", 32, 32));
		wzImage.AddProperty(wzSubProperty);
		wzImage.MarkWzImageAsParsed();
		wzImage.Changed = true;
		return wzImage;
	}

	private static WzCanvasProperty CreateTransparentCanvas(string name, int width, int height)
	{
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(name);
		WzPngProperty wzPngProperty = new WzPngProperty();
		using (Bitmap bitmap = new Bitmap(width, height))
		{
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.Transparent);
			}
			wzPngProperty.PNG = (Bitmap)bitmap.Clone();
		}
		wzCanvasProperty.PngProperty = wzPngProperty;
		wzCanvasProperty.AddProperty(new WzVectorProperty("origin", width / 2, height));
		return wzCanvasProperty;
	}

	private string SuggestNewItemId(string category, string templateId)
	{
		string text = null;
		if (!string.IsNullOrWhiteSpace(templateId) && templateId.Length >= 8)
		{
			text = templateId.Substring(0, 4);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = category switch
			{
				"Face" => "0002", 
				"Hair" => "0003", 
				"Cap" => "0100", 
				"Accessory" => "0101", 
				"Coat" => "0104", 
				"Longcoat" => "0105", 
				"Pants" => "0106", 
				"Shoes" => "0107", 
				"Glove" => "0108", 
				"Shield" => "0109", 
				"Cape" => "0110", 
				"Ring" => "0111", 
				"Weapon" => "0130", 
				"PetEquip" => "0180", 
				"TamingMob" => "0190", 
				_ => "0100"
			};
		}
		int num = -1;
		HashSet<int> hashSet = new HashSet<int>();
		foreach (ImgItemEntry item in _items)
		{
			if (item.ItemId.Length >= 8 && item.ItemId.StartsWith(text, StringComparison.Ordinal) && int.TryParse(item.ItemId.Substring(4), out int result))
			{
				num = Math.Max(num, result);
				hashSet.Add(result);
			}
		}
		int num2 = num + 1;
		while (num2 <= 9999 && hashSet.Contains(num2))
		{
			num2++;
		}
		if (num2 > 9999)
		{
			num2 = 0;
			while (num2 <= 9999 && hashSet.Contains(num2))
			{
				num2++;
			}
		}
		if (num2 > 9999)
		{
			num2 = 9999;
		}
		return text + num2.ToString("D4");
	}

	private string ValidateNewItemId(string text, string category, string folder)
	{
		if (!int.TryParse(text, out int value) || value < 0)
		{
			return "ID must be a non-negative number.";
		}
		string text2 = value.ToString("D8");
		if (text2.Length != 8)
		{
			return "Equipment IDs must fit in 8 digits.";
		}
		if (_items.Any((ImgItemEntry i) => string.Equals(i.ItemId, text2, StringComparison.OrdinalIgnoreCase)))
		{
			return "That ID is already loaded.";
		}
		if (_archiveSession != null && _archiveSession.FindImage(text2 + ".img") != null)
		{
			return "That image already exists in the archive.";
		}
		if (folder != null)
		{
			if (File.Exists(Path.Combine(folder, text2 + ".img")) || File.Exists(Path.Combine(folder, text2 + ".img.xml")))
			{
				return "That item file already exists.";
			}
		}
		return null;
	}

	private string ValidateNewGroupedItemId(string text, ImgItemEntry template)
	{
		string value = text.Trim();
		if (value.Length == 0 || value.IndexOfAny(new char[3] { '/', '\\', ':' }) >= 0)
		{
			return "ID cannot be empty or contain path characters.";
		}
		if (_items.Any((ImgItemEntry item) => string.Equals(item.ItemId, value, StringComparison.OrdinalIgnoreCase)))
		{
			return "That ID is already loaded.";
		}
		if (template.Image[value] != null)
		{
			return "That ID already exists in " + template.Image.Name + ".";
		}
		return null;
	}

	private static string NormalizeItemId(string text)
	{
		if (int.TryParse(text, out int value))
		{
			return value.ToString("D8");
		}
		return text;
	}

	private void SaveSelectedItem()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		try
		{
			foreach (ImgItemEntry item in selectedEntries)
			{
				item.Save();
			}
			if (_archiveSession != null)
			{
				SetStatus((selectedEntries.Count == 1) ? ("Staged " + selectedEntries[0].DisplayName + " for WZ save") : $"Staged {selectedEntries.Count} selected items for WZ save");
				SaveWzArchive(askConfirmation: true);
			}
			else if (_stringLookup?.IsDirty == true)
			{
				SaveWzArchive(askConfirmation: false);
			}
			else
			{
				SetStatus((selectedEntries.Count == 1) ? ("Saved " + selectedEntries[0].DisplayName) : $"Saved {selectedEntries.Count} selected items");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void SaveSelectedAsImg()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		if (selectedEntries.Count == 1)
		{
			ImgItemEntry imgItemEntry = selectedEntries[0];
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "IMG files (*.img)|*.img",
				FileName = Path.GetFileName(imgItemEntry.FilePath)
			};
			try
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				try
				{
					ExportEntryAsImg(imgItemEntry, saveFileDialog.FileName);
					SetStatus("Exported IMG to " + saveFileDialog.FileName);
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			finally
			{
				((IDisposable)(object)saveFileDialog)?.Dispose();
			}
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select a folder to export selected IMG files"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			try
			{
				foreach (ImgItemEntry item in selectedEntries)
				{
					string outputPath = Path.Combine(folderBrowserDialog.SelectedPath, Path.GetFileName(item.FilePath));
					ExportEntryAsImg(item, outputPath);
				}
				SetStatus($"Exported {selectedEntries.Count} IMG files to {folderBrowserDialog.SelectedPath}");
			}
			catch (Exception ex2)
			{
				MessageBox.Show(this, ex2.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private void SaveSelectedAsXml()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		if (selectedEntries.Count == 1)
		{
			ImgItemEntry imgItemEntry = selectedEntries[0];
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "IMG XML files (*.img.xml)|*.img.xml",
				FileName = Path.GetFileName(imgItemEntry.FilePath) + ".xml"
			};
			try
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				try
				{
					string text = (saveFileDialog.FileName.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) ? saveFileDialog.FileName : (saveFileDialog.FileName.TrimEnd('.') + ".img.xml"));
					ExportEntryAsXml(imgItemEntry, text);
					SetStatus("Exported XML to " + text);
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			finally
			{
				((IDisposable)(object)saveFileDialog)?.Dispose();
			}
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select a folder to export selected IMG XML files"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			try
			{
				foreach (ImgItemEntry item in selectedEntries)
				{
					string outputPath = Path.Combine(folderBrowserDialog.SelectedPath, Path.GetFileName(item.FilePath) + ".xml");
					ExportEntryAsXml(item, outputPath);
				}
				SetStatus($"Exported {selectedEntries.Count} XML files to {folderBrowserDialog.SelectedPath}");
			}
			catch (Exception ex2)
			{
				MessageBox.Show(this, ex2.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private void ExportCanvasForSelection(string canvasName)
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		List<ImgItemEntry> list = selectedEntries.Where((ImgItemEntry entry) => GetCanvas(entry.Info, canvasName) != null).ToList();
		if (list.Count == 0)
		{
			MessageBox.Show(this, "No selected items contain '" + canvasName + "'.", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (list.Count == 1)
		{
			ImgItemEntry imgItemEntry = list[0];
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "PNG files (*.png)|*.png",
				FileName = Path.GetFileNameWithoutExtension(imgItemEntry.FilePath) + "." + canvasName + ".png"
			};
			try
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				try
				{
					ExportCanvas(imgItemEntry, canvasName, saveFileDialog.FileName);
					SetStatus("Exported " + canvasName + " to " + saveFileDialog.FileName);
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			finally
			{
				((IDisposable)(object)saveFileDialog)?.Dispose();
			}
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select a folder to export selected " + canvasName + " PNG files"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			try
			{
				foreach (ImgItemEntry item in list)
				{
					string outputPath = Path.Combine(folderBrowserDialog.SelectedPath, Path.GetFileNameWithoutExtension(item.FilePath) + "." + canvasName + ".png");
					ExportCanvas(item, canvasName, outputPath);
				}
				string status = ((list.Count == selectedEntries.Count) ? $"Exported {list.Count} {canvasName} PNG files to {folderBrowserDialog.SelectedPath}" : $"Exported {list.Count} {canvasName} PNG files to {folderBrowserDialog.SelectedPath} ({selectedEntries.Count - list.Count} skipped)");
				SetStatus(status);
			}
			catch (Exception ex2)
			{
				MessageBox.Show(this, ex2.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private void SaveAllItems()
	{
		if (_items.Count == 0)
		{
			return;
		}
		try
		{
			foreach (ImgItemEntry item in _items.Where((ImgItemEntry i) => i.IsDirty))
			{
				item.Save();
			}
			if (_archiveSession != null)
			{
				SetStatus("Staged all modified items for WZ save");
				SaveWzArchive(askConfirmation: true);
			}
			else if (_stringLookup?.IsDirty == true)
			{
				SaveWzArchive(askConfirmation: false);
			}
			else
			{
				SetStatus("Saved all modified items");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CopyItemId(ImgItemEntry? entry)
	{
		if (entry == null)
		{
			return;
		}
		string text = NormalizeItemIdForCopy(entry.ItemId);
		try
		{
			Clipboard.SetText(text);
			SetStatus("Copied ID " + text);
		}
		catch (Exception ex)
		{
			SetStatus("Could not copy ID: " + ex.Message);
		}
	}

	private void CopySelectedIds()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count != 0)
		{
			string text = string.Join(Environment.NewLine, selectedEntries.Select((ImgItemEntry entry) => NormalizeItemIdForCopy(entry.ItemId)).Distinct());
			Clipboard.SetText(text);
			SetStatus((selectedEntries.Count == 1) ? ("Copied ID " + text) : $"Copied {selectedEntries.Count} item IDs");
		}
	}

	private static string NormalizeItemIdForCopy(string itemId)
	{
		return (!string.IsNullOrEmpty(itemId) && itemId[0] == '0') ? itemId.Substring(1) : itemId;
	}

	private void ReplaceCanvasForSelection(string canvasName)
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			try
			{
				using Bitmap bitmap = new Bitmap(openFileDialog.FileName);
				foreach (ImgItemEntry item in selectedEntries)
				{
					ReplaceOrCreateCanvas(item.Info, canvasName, bitmap);
					item.Image.Changed = true;
					item.IsDirty = true;
					item.RefreshThumbnail();
				}
				SetSelectionState(selectedEntries);
				RefreshItemBrowser();
				SetStatus((selectedEntries.Count == 1) ? ("Updated " + canvasName + " for " + selectedEntries[0].DisplayName) : $"Updated {canvasName} for {selectedEntries.Count} selected items");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Icon update failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private static WzCanvasProperty? GetCanvas(WzSubProperty info, string canvasName)
	{
		return ResolveCanvas(info, canvasName);
	}

	private static void ExportCanvas(ImgItemEntry entry, string canvasName, string outputPath)
	{
		WzCanvasProperty wzCanvasProperty = GetCanvas(entry.Info, canvasName) ?? throw new InvalidOperationException($"Item {entry.ItemId} does not contain '{canvasName}'.");
		string filename = (outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? outputPath : (outputPath.TrimEnd('.') + ".png"));
		using Bitmap? bitmap = WzAnimationReader.GetCanvasBitmap(wzCanvasProperty) ?? throw new InvalidOperationException($"Item {entry.ItemId} does not contain a readable '{canvasName}'.");
		bitmap.Save(filename);
	}

	private void AddIconRewardNodesForSelection()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		if (selectedEntries.Count == 1)
		{
			try
			{
				if (!TryApplyIconReward(selectedEntries[0]))
				{
					SetStatus("The selected item has no iconRaw/icon that can match its ID");
					return;
				}
				SetSelectionState(selectedEntries);
				RefreshItemBrowser();
				SetStatus("Added iconReward for item " + selectedEntries[0].ItemId);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "iconReward update failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		int applied = 0;
		List<string> skipped = new List<string>();
		SplashForm splash = new SplashForm("Batch adding iconReward", $"Building reward icons for {selectedEntries.Count} items");
		splash.Show(this);
		splash.Report("Preparing batch...", 0.0);
		Cursor = Cursors.WaitCursor;
		string status;
		try
		{
			for (int i = 0; i < selectedEntries.Count; i++)
			{
				ImgItemEntry item = selectedEntries[i];
				splash.Report($"iconReward {i + 1}/{selectedEntries.Count}: item {item.ItemId}", (double)i / selectedEntries.Count);
				try
				{
					if (TryApplyIconReward(item))
					{
						applied++;
					}
					else
					{
						skipped.Add(item.ItemId);
					}
				}
				catch
				{
					skipped.Add(item.ItemId);
				}
			}
			splash.Report("Finalizing batch...", 1.0);
			status = $"Added iconReward to {applied} of {selectedEntries.Count} items";
			if (skipped.Count > 0)
			{
				status += $" ({skipped.Count} skipped: {string.Join(", ", skipped.Take(5))}{((skipped.Count > 5) ? ", ..." : string.Empty)})";
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "iconReward update failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			status = null;
		}
		finally
		{
			splash.Close();
			splash.Dispose();
			Cursor = Cursors.Default;
		}
		SetSelectionState(selectedEntries);
		RefreshItemBrowser();
		if (status != null)
		{
			SetStatus(status);
		}
	}

	private bool TryApplyIconReward(ImgItemEntry item)
	{
		if (!InfoMatchesEntry(item))
		{
			return false;
		}
		WzCanvasProperty? source = ResolveCanvas(item.Info, "iconRaw") ?? ResolveCanvas(item.Info, "icon");
		if (source == null)
		{
			return false;
		}
		using Bitmap? inner = WzAnimationReader.GetCanvasBitmap(source);
		if (inner == null)
		{
			return false;
		}
		using Bitmap frame = IconRewardRenderer.Render(inner, _rewardGlowSlider.Value, out Point innerOffset);
		ApplyIconRewardStructure(item, frame, source, innerOffset);
		item.Image.Changed = true;
		item.IsDirty = true;
		_archiveSession?.MarkDirty();
		return true;
	}

	private static bool InfoMatchesEntry(ImgItemEntry item)
	{
		if (item.ItemProperty != null)
		{
			return ReferenceEquals(item.ItemProperty["info"], item.Info) || ReferenceEquals(item.ItemProperty, item.Info);
		}
		return ReferenceEquals(item.Image["info"], item.Info);
	}

	private void ReplaceIconRewardForSelection()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			try
			{
				using Bitmap image = new Bitmap(openFileDialog.FileName);
				foreach (ImgItemEntry item in selectedEntries)
				{
					WzCanvasProperty? originSource = ResolveCanvas(item.Info, "iconRaw") ?? ResolveCanvas(item.Info, "icon");
					using Bitmap frame = IconRewardRenderer.Render(image, _rewardGlowSlider.Value, out Point innerOffset);
					ApplyIconRewardStructure(item, frame, originSource, innerOffset);
					item.Image.Changed = true;
					item.IsDirty = true;
				}
				SetSelectionState(selectedEntries);
				RefreshItemBrowser();
				SetStatus((selectedEntries.Count == 1) ? ("Updated iconReward for " + selectedEntries[0].DisplayName) : $"Updated iconReward for {selectedEntries.Count} selected items");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "iconReward update failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private void DeleteIconRewardForSelection()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (ImgItemEntry item in selectedEntries)
		{
			if (item.Info["iconReward"] != null)
			{
				item.Info.RemoveProperty("iconReward");
				item.Image.Changed = true;
				item.IsDirty = true;
				num++;
			}
		}
		if (num == 0)
		{
			SetStatus("No selected items had iconReward to delete");
			return;
		}
		SetSelectionState(selectedEntries);
		SetStatus((num == 1) ? "Deleted iconReward" : $"Deleted iconReward for {num} selected items");
	}

	private static WzSubProperty CreateSubProperty(IPropertyContainer parent, string name)
	{
		parent.RemoveProperty(name);
		WzSubProperty wzSubProperty = new WzSubProperty(name);
		parent.AddProperty(wzSubProperty);
		return wzSubProperty;
	}

	private static void ApplyIconRewardStructure(ImgItemEntry entry, Bitmap bitmap, WzCanvasProperty? originSource, Point innerOffset)
	{
		WzSubProperty wzSubProperty = (entry.Info["iconReward"] as WzSubProperty) ?? CreateSubProperty(entry.Info, "iconReward");
		WzCanvasProperty wzCanvasProperty = ReplaceOrCreateCanvas(wzSubProperty, "0", bitmap, WzPngFormat.Format2);
		int originX = 28;
		int originY = 27;
		if (originSource?["origin"] is WzVectorProperty origin)
		{
			originX = origin.X.Value + innerOffset.X;
			originY = origin.Y.Value + innerOffset.Y;
		}
		SetOrCreateVector(wzCanvasProperty, "origin", originX, originY);
		SetOrCreateInt(wzCanvasProperty, "delay", 120);
		wzSubProperty.SortProperties();
	}

	private static WzCanvasProperty ReplaceOrCreateCanvas(IPropertyContainer container, string name, Bitmap bitmap)
	{
		return ReplaceOrCreateCanvas(container, name, bitmap, null);
	}

	private static WzCanvasProperty ReplaceOrCreateCanvas(IPropertyContainer container, string name, Bitmap bitmap, WzPngFormat? forcedFormat)
	{
		WzCanvasProperty wzCanvasProperty = (container[name] as WzCanvasProperty) ?? CreateCanvas(container, name);
		UpdateCanvasBitmap(wzCanvasProperty, bitmap, forcedFormat);
		return wzCanvasProperty;
	}

	private static WzCanvasProperty CreateCanvas(IPropertyContainer container, string name)
	{
		container.RemoveProperty(name);
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(name)
		{
			PngProperty = new WzPngProperty()
		};
		container.AddProperty(wzCanvasProperty);
		return wzCanvasProperty;
	}

	private static WzCanvasProperty? GetIconRewardFrame(WzSubProperty info)
	{
		if (info["iconReward"] is WzSubProperty container)
		{
			return ResolveCanvas(container, "0");
		}
		return ResolveCanvas(info, "iconReward");
	}

	private static Bitmap? GetIconRewardPreviewBitmap(WzSubProperty info)
	{
		WzCanvasProperty iconRewardFrame = GetIconRewardFrame(info);
		if (iconRewardFrame == null)
		{
			return null;
		}
		using Bitmap? bitmap = WzAnimationReader.GetCanvasBitmap(iconRewardFrame);
		if (bitmap == null)
		{
			return null;
		}
		return (Bitmap)bitmap.Clone();
	}

	internal static WzCanvasProperty? ResolveCanvas(IPropertyContainer container, string name)
	{
		if (container[name] is WzCanvasProperty result)
		{
			return result;
		}
		if (container[name] is WzUOLProperty { LinkValue: WzCanvasProperty linkValue })
		{
			return linkValue;
		}
		return null;
	}

	private static void UpdateCanvasBitmap(WzCanvasProperty canvas, Bitmap bitmap, WzPngFormat? forcedFormat = null)
	{
		if (canvas.PngProperty == null)
		{
			WzPngProperty wzPngProperty = (canvas.PngProperty = new WzPngProperty());
		}
		canvas.RemoveProperty(WzCanvasProperty.InlinkPropertyName);
		canvas.RemoveProperty(WzCanvasProperty.OutlinkPropertyName);
		WzPngFormat? targetFormat = forcedFormat ?? ((canvas.PngProperty.Width > 0 && canvas.PngProperty.Height > 0) ? new WzPngFormat?(canvas.PngProperty.Format) : ((WzPngFormat?)null));
		using Bitmap bitmap2 = (Bitmap)bitmap.Clone();
		canvas.PngProperty.SetBitmapPreservingFormat(bitmap2, targetFormat);
	}

	private static void CopyOrCreateCanvasOrigin(WzCanvasProperty? sourceCanvas, WzCanvasProperty targetCanvas)
	{
		int x = 0;
		int y = 0;
		if (sourceCanvas?["origin"] is WzVectorProperty wzVectorProperty)
		{
			x = wzVectorProperty.X.Value;
			y = wzVectorProperty.Y.Value;
		}
		SetOrCreateVector(targetCanvas, "origin", x, y);
	}

	private static void SetOrCreateVector(IPropertyContainer container, string name, int x, int y)
	{
		if (container[name] is WzVectorProperty wzVectorProperty)
		{
			wzVectorProperty.X.Value = x;
			wzVectorProperty.Y.Value = y;
		}
		else
		{
			container.RemoveProperty(name);
			container.AddProperty(new WzVectorProperty(name, x, y));
		}
	}

	private static void SetOrCreateInt(IPropertyContainer container, string name, int value)
	{
		if (container[name] is WzIntProperty wzIntProperty)
		{
			wzIntProperty.Value = value;
			return;
		}
		container.RemoveProperty(name);
		container.AddProperty(new WzIntProperty(name, value));
	}

	private static void SaveBinaryItem(ImgItemEntry entry)
	{
		WzImgSerializer wzImgSerializer = new WzImgSerializer(entry.Iv);
		wzImgSerializer.SerializeImage(entry.Image, entry.FilePath);
		entry.IsDirty = false;
	}

	private void SaveXmlItem(ImgItemEntry entry)
	{
		_xmlDocumentService.SaveToXml(entry.Image, entry.FilePath);
		entry.IsDirty = false;
	}

	private void SaveGroupedXmlItem(ImgItemEntry entry, WzImage rootImage, string sourcePath)
	{
		_xmlDocumentService.SaveToXml(rootImage, sourcePath);
		entry.IsDirty = false;
	}

	private static void SaveGroupedBinaryItem(ImgItemEntry entry, WzImage rootImage, string sourcePath, byte[] iv)
	{
		WzImgSerializer wzImgSerializer = new WzImgSerializer(iv);
		wzImgSerializer.SerializeImage(rootImage, sourcePath);
		entry.IsDirty = false;
	}

	private static void ExportEntryAsImg(ImgItemEntry entry, string outputPath)
	{
		using WzImage img = CreateExportImage(entry);
		WzImgSerializer wzImgSerializer = new WzImgSerializer(entry.Iv);
		wzImgSerializer.SerializeImage(img, outputPath);
	}

	private static void ExportEntryAsXml(ImgItemEntry entry, string outputPath)
	{
		using WzImage img = CreateExportImage(entry);
		WzClassicXmlSerializer wzClassicXmlSerializer = new WzClassicXmlSerializer(1, LineBreak.None, exportbase64: true);
		wzClassicXmlSerializer.SerializeImage(img, outputPath);
	}

	private static WzImage CreateExportImage(ImgItemEntry entry)
	{
		if (entry.ItemProperty == null || entry.IsGroupedEntry)
		{
			WzImage wzImage = new WzImage(Path.GetFileName(entry.FilePath));
			foreach (WzImageProperty wzProperty in entry.Image.WzProperties)
			{
				wzImage.AddProperty(wzProperty.DeepClone());
			}
			wzImage.MarkWzImageAsParsed();
			wzImage.Changed = true;
			return wzImage;
		}
		WzImage wzImage2 = new WzImage(entry.ItemId + ".img");
		foreach (WzImageProperty wzProperty2 in entry.ItemProperty.WzProperties)
		{
			wzImage2.AddProperty(wzProperty2.DeepClone());
		}
		wzImage2.MarkWzImageAsParsed();
		wzImage2.Changed = true;
		return wzImage2;
	}

	private bool ConfirmDiscardChanges()
	{
		bool sessionDirty = (_archiveSession != null && _archiveSession.IsDirty) || _stringLookup?.IsDirty == true;
		if (sessionDirty)
		{
			string fileName = ((_archiveSession != null) ? _archiveSession.FileName : Path.GetFileName(_stringLookup?.Path ?? "String.wz"));
			DialogResult result = MessageBox.Show(this, "Save staged changes to " + fileName + " before continuing? Choose No to discard the staged changes.", "Unsaved WZ changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.Cancel)
			{
				return false;
			}
			if (result == DialogResult.Yes)
			{
				SaveWzArchive(askConfirmation: false);
				if ((_archiveSession != null && _archiveSession.IsDirty) || _stringLookup?.IsDirty == true)
				{
					return false;
				}
			}
		}
		if (_items.All((ImgItemEntry i) => !i.IsDirty))
		{
			return true;
		}
		return MessageBox.Show(this, "Discard unsaved changes?", "Unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes;
	}

	private void ClearLoadedItems()
	{
		CloseArchiveSession();
		foreach (ImgItemEntry item in _items)
		{
			item.Dispose();
		}
		_items.Clear();
		_assetGrid.ClearRows();
		_searchTextBox.Text = string.Empty;
		_header.RightText = "No archive loaded";
		RefreshCategoryFilterOptions();
		SetSelectionState(new List<ImgItemEntry>());
		_saveAllButton.Enabled = false;
	}

	private void ResetBrowserFilters()
	{
		_updatingFilterControls = true;
		_searchTextBox.Text = string.Empty;
		_cashFilterComboBox.SelectedIndex = 0;
		_cashOnlyMenuItem.Checked = false;
		_categoryFilterComboBox.SelectedIndex = 0;
		_tradeLockFilterComboBox.SelectedIndex = 0;
		_minReqLevelFilterInput.Value = 0m;
		_maxReqLevelFilterInput.Value = _maxReqLevelFilterInput.Maximum;
		_dirtyOnlyFilterCheckBox.Checked = false;
		_updatingFilterControls = false;
		RefreshItemBrowser();
	}

	private void RefreshCategoryFilterOptions()
	{
		string text = _categoryFilterComboBox.SelectedItem as string;
		List<string> list = (from category in (from item in _items
				select item.Detection.Category into category
				where !string.IsNullOrWhiteSpace(category)
				select category).Distinct(StringComparer.OrdinalIgnoreCase)
			orderby category
			select category).ToList();
		_updatingFilterControls = true;
		_categoryFilterComboBox.Items.Clear();
		_categoryFilterComboBox.Items.Add("All categories");
		foreach (string item in list)
		{
			_categoryFilterComboBox.Items.Add(item);
		}
		int selectedIndex = 0;
		if (!string.IsNullOrWhiteSpace(text))
		{
			int num = _categoryFilterComboBox.FindStringExact(text);
			if (num >= 0)
			{
				selectedIndex = num;
			}
		}
		_categoryFilterComboBox.SelectedIndex = selectedIndex;
		_updatingFilterControls = false;
	}

	private void RefreshItemBrowser()
	{
		if (_updatingFilterControls)
		{
			return;
		}
		_loadingSelection = true;
		try
		{
			string query = _searchTextBox.Text.Trim();
			string selectedCategory = (_categoryFilterComboBox.SelectedItem as string) ?? "All categories";
			int minReqLevel = decimal.ToInt32(_minReqLevelFilterInput.Value);
			int maxReqLevel = decimal.ToInt32(_maxReqLevelFilterInput.Value);
			bool dirtyOnly = _dirtyOnlyFilterCheckBox.Checked;
			int cashMode = _cashFilterComboBox.SelectedIndex;
			int tradeLockMode = _tradeLockFilterComboBox.SelectedIndex;
			List<object> preserve = _assetGrid.SelectedRows.Select((AssetGridRow r) => r.Tag).ToList();
			if (maxReqLevel < minReqLevel)
			{
				int num = maxReqLevel;
				int num2 = minReqLevel;
				maxReqLevel = num2;
				minReqLevel = num;
			}
			IEnumerable<ImgItemEntry> source = _items;
			if (!string.IsNullOrWhiteSpace(query))
			{
				source = source.Where((ImgItemEntry item) => item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || item.ItemId.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Detection.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
			}
			if (!string.Equals(selectedCategory, "All categories", StringComparison.OrdinalIgnoreCase))
			{
				source = source.Where((ImgItemEntry item) => string.Equals(item.Detection.Category, selectedCategory, StringComparison.OrdinalIgnoreCase));
			}
			source = source.Where((ImgItemEntry item) =>
			{
				int infoInt = GetInfoInt(item.Info, "reqLevel");
				bool flag = GetInfoInt(item.Info, "cash") != 0;
				bool flag2 = GetInfoInt(item.Info, "tradeBlock") != 0;
				bool flag3 = cashMode switch
				{
					1 => flag, 
					2 => !flag, 
					_ => true
				};
				bool flag4 = tradeLockMode switch
				{
					1 => flag2, 
					2 => !flag2, 
					_ => true
				};
				return flag3 && flag4 && infoInt >= minReqLevel && infoInt <= maxReqLevel && (!dirtyOnly || item.IsDirty);
			});
			List<AssetGridRow> rows = source.Select((ImgItemEntry item) => new AssetGridRow
			{
				Tag = item,
				Id = item.ItemId,
				Name = item.DisplayName,
				Category = item.Detection.Category,
				Level = GetInfoInt(item.Info, "reqLevel").ToString(),
				Flags = BuildItemFlags(item),
				IsDirty = item.IsDirty,
				Icon = item.Thumbnail
			}).ToList();
			_assetGrid.SetRows(rows, preserve);
		}
		finally
		{
			_loadingSelection = false;
			LoadSelectedItem();
		}
	}

	private static string BuildItemFlags(ImgItemEntry item)
	{
		List<string> list = new List<string>();
		if (GetInfoInt(item.Info, "cash") != 0)
		{
			list.Add("Cash");
		}
		if (GetInfoInt(item.Info, "tradeBlock") != 0)
		{
			list.Add("Trade Lock");
		}
		return string.Join(", ", list);
	}

	private void SetPreview(Bitmap? bitmap)
	{
		Image image = _iconPreview.Image;
		_iconPreview.Image = ((bitmap == null) ? null : ((Bitmap)bitmap.Clone()));
		image?.Dispose();
		if (_previewSizeLabel != null)
		{
			_previewSizeLabel.Text = ((bitmap == null) ? "32 px" : $"{bitmap.Width} x {bitmap.Height} px");
		}
	}

	private void SetRewardPreviewForInfo(WzSubProperty? info)
	{
		StopRewardPreviewAnimation();
		if (info == null)
		{
			SetRewardPreview(null, hasReward: false);
			return;
		}
		WzAnimationInfo? animation = WzAnimationReader.Enumerate(info).FirstOrDefault((WzAnimationInfo item) => string.Equals(item.Name, "iconReward", StringComparison.OrdinalIgnoreCase) || item.Name.EndsWith("/iconReward", StringComparison.OrdinalIgnoreCase));
		if (animation != null)
		{
			try
			{
				_rewardPreviewClip = WzAnimationReader.Load(animation);
				_rewardPreviewPlayback = new AnimationPlayback(_iconRewardPreview, _rewardPreviewClip.Frames, _rewardPreviewClip.Delays);
				_iconRewardPreviewStatusLabel.Visible = false;
				if (_rewardPreviewSizeLabel != null)
				{
					_rewardPreviewSizeLabel.Text = $"{_rewardPreviewClip.Width} x {_rewardPreviewClip.Height} px";
				}
				_rewardPreviewPlayback.Play();
				return;
			}
			catch
			{
				StopRewardPreviewAnimation();
			}
		}
		SetRewardPreview(GetIconRewardPreviewBitmap(info), GetIconRewardFrame(info) != null);
	}

	private void StopRewardPreviewAnimation()
	{
		_rewardPreviewPlayback?.Dispose();
		_rewardPreviewPlayback = null;
		_rewardPreviewClip?.Dispose();
		_rewardPreviewClip = null;
		_iconRewardPreview.Image = null;
	}

	private void SetRewardPreview(Bitmap? bitmap, bool hasReward)
	{
		Image image = _iconRewardPreview.Image;
		_iconRewardPreview.Image = ((bitmap == null) ? null : ((Bitmap)bitmap.Clone()));
		image?.Dispose();
		_iconRewardPreviewStatusLabel.Visible = !hasReward;
		if (_rewardPreviewSizeLabel != null)
		{
			_rewardPreviewSizeLabel.Text = ((bitmap == null) ? "32 px" : $"{bitmap.Width} x {bitmap.Height} px");
		}
	}

	private void SetStatus(string message)
	{
		_statusLabel.Text = message;
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		ApplyResponsiveLayout();
	}

	private void ApplyResponsiveLayout()
	{
		if (_splitContainer == null || _splitContainer.Width <= 0)
		{
			return;
		}
		try
		{
			int width = _splitContainer.Width;
			_splitContainer.Panel1MinSize = Math.Max(380, (int)(width * 0.28));
			_splitContainer.Panel2MinSize = Math.Max(720, (int)(width * 0.52));
			int num = Math.Max(_splitContainer.Panel1MinSize, (int)(width * 0.35));
			num = Math.Min(num, width - _splitContainer.Panel2MinSize);
			if (num > _splitContainer.Panel1MinSize)
			{
				_splitContainer.SplitterDistance = num;
			}
		}
		catch
		{
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (!ConfirmDiscardChanges())
		{
			e.Cancel = true;
			return;
		}
		ClearLoadedItems();
		SetPreview(null);
		StopRewardPreviewAnimation();
		SetRewardPreview(null, hasReward: false);
		base.OnFormClosing(e);
	}
}















