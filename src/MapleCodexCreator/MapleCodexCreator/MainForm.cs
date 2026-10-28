using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using MapleEditor.UI;
using MapleLib.Configuration;
using MapleLib.WzLib;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleCodexCreator;

public sealed class MainForm : Form
{
	private static readonly Color AppBackColor = UiTheme.AppBackground;

	private static readonly Color SurfaceColor = UiTheme.Surface;

	private static readonly Color SurfaceRaisedColor = UiTheme.SurfaceRaised;

	private static readonly Color AccentColor = UiTheme.Accent;

	private static readonly Color BorderColor = UiTheme.Border;

	private static readonly Color TextColor = UiTheme.Text;

	private static readonly Color SubtleTextColor = UiTheme.TextSubtle;

	private readonly ImgXmlDocumentService _xmlDocumentService = new ImgXmlDocumentService();

	private readonly List<CodexCardEntry> _cards = new List<CodexCardEntry>();

	private readonly ImageList _images = new ImageList
	{
		ImageSize = new Size(64, 64),
		ColorDepth = ColorDepth.Depth32Bit
	};

	private readonly ListView _cardList;

	private readonly PictureBox _iconPreview;

	private readonly PictureBox _iconRawPreview;

	private readonly TextBox _searchBox;

	private readonly TextBox _newCardIdBox;

	private readonly TextBox _mobIdBox;

	private readonly TextBox _nameBox;

	private readonly TextBox _descBox;

	private readonly TextBox _consumeStringPathBox;

	private readonly Label _selectedCardLabel;

	private readonly Label _selectedFileLabel;

	private readonly ToolStripStatusLabel _statusLabel;

	private readonly Button _replaceIconButton;

	private readonly Button _replaceIconRawButton;

	private readonly Button _exportIconRawButton;

	private readonly Button _saveSelectedButton;

	private readonly Button _saveAllButton;

	private readonly Button _saveAsImgButton;

	private readonly Button _saveAsXmlButton;

	private readonly Button _saveBothButton;

	private readonly Button _massAddButton;

	private readonly Button _deleteButton;

	private readonly BrandHeader _header;

	private readonly SplitContainer _splitContainer;

	private string? _loadedPath;

	private bool _loadingSelection;

	private WzArchiveSession? _itemArchive;

	private WzArchiveSession? _stringArchive;

	private WzImage? _stringConsumeImage;

	public MainForm()
	{
		Text = "Maple Codex Creator - " + MapleEditorVersionSettings.Profile.DisplayName;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.Manual;
		BackColor = AppBackColor;
		MinimumSize = new Size(1500, 920);
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		Bounds = GetStartupBounds();
		MenuStrip value = (MainMenuStrip = BuildMenu());
		UiTheme.StyleMenu(value);
		_header = new BrandHeader
		{
			Dock = DockStyle.Top,
			Height = 94,
			Compact = true,
			Title = "Maple Codex Creator",
			Subtitle = "MonsterBook cards, icons and String.wz name/description text",
			VersionText = MapleEditorVersionSettings.Profile.DisplayName,
			RightText = "0238.img / Consume.img"
		};
		StatusStrip statusStrip = new StatusStrip();
		_statusLabel = new ToolStripStatusLabel("Open Item.wz, 0238.img, or 0238.img.xml to begin.");
		UiTheme.StyleStatus(statusStrip);
		statusStrip.Items.Add(_statusLabel);
		SplitContainer splitContainer = new SplitContainer
		{
			Dock = DockStyle.Fill,
			SplitterDistance = 560,
			BackColor = AppBackColor,
			Padding = new Padding(18, 10, 18, 10)
		};
		_splitContainer = splitContainer;
		Controls.Add(splitContainer);
		Controls.Add(statusStrip);
		Controls.Add(_header);
		Controls.Add(value);
		Panel panel = CreatePanel();
		panel.Padding = new Padding(12);
		splitContainer.Panel1.Controls.Add(panel);
		Label value2 = new Label
		{
			Text = "Codex Cards",
			Dock = DockStyle.Top,
			Height = 32,
			Font = new Font("Bahnschrift SemiBold", 15f, FontStyle.Bold),
			ForeColor = TextColor
		};
		_searchBox = new TextBox
		{
			Dock = DockStyle.Top,
			Height = 32,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle,
			PlaceholderText = "Search by card id or mob id"
		};
		_searchBox.TextChanged += (object? _, EventArgs _) =>
		{
			RefreshList();
		};
		_cardList = new ListView
		{
			Dock = DockStyle.Fill,
			View = View.LargeIcon,
			LargeImageList = _images,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle,
			HideSelection = false,
			MultiSelect = true
		};
		_cardList.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			if (!_loadingSelection)
			{
				UpdateSelectionState();
			}
		};
		_cardList.KeyDown += CardList_KeyDown;
		panel.Controls.Add(_cardList);
		panel.Controls.Add(_searchBox);
		panel.Controls.Add(value2);
		Panel panel2 = CreatePanel();
		panel2.Padding = new Padding(16);
		splitContainer.Panel2.Controls.Add(panel2);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 5
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		panel2.Controls.Add(tableLayoutPanel);
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(12)
		};
		tableLayoutPanel.Controls.Add(panel3, 0, 0);
		_selectedCardLabel = new Label
		{
			Left = 0,
			Top = 0,
			Width = 620,
			Height = 36,
			Font = new Font("Bahnschrift SemiBold", 20f, FontStyle.Bold),
			ForeColor = TextColor,
			Text = "No card selected"
		};
		_selectedFileLabel = new Label
		{
			Left = 2,
			Top = 42,
			Width = 620,
			Height = 44,
			ForeColor = SubtleTextColor,
			Text = "-"
		};
		panel3.Controls.Add(_selectedCardLabel);
		panel3.Controls.Add(_selectedFileLabel);
		_iconPreview = CreatePreview("icon", 0, 92, panel3);
		_iconRawPreview = CreatePreview("iconRaw", 150, 92, panel3);
		Panel panel4 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(12)
		};
		tableLayoutPanel.Controls.Add(panel4, 0, 1);
		panel4.Controls.Add(new Label
		{
			Left = 0,
			Top = 0,
			Width = 230,
			Height = 22,
			Text = "Consume String",
			ForeColor = TextColor
		});
		_consumeStringPathBox = new TextBox
		{
			Left = 0,
			Top = 24,
			Width = 470,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		panel4.Controls.Add(_consumeStringPathBox);
		Button button = CreateButton("Browse", 480, 22, 95, 30, primary: false);
		button.Click += (object? _, EventArgs _) =>
		{
			BrowseConsumeString();
		};
		panel4.Controls.Add(button);
		Button button2 = CreateButton("Save Consume As IMG", 0, 56, 170, 30, primary: false);
		Button button3 = CreateButton("Save Consume As XML", 180, 56, 170, 30, primary: false);
		Button button4 = CreateButton("Save Consume Both", 360, 56, 170, 30, primary: true);
		button2.Click += (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsImg();
		};
		button3.Click += (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsXml();
		};
		button4.Click += (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsBoth();
		};
		panel4.Controls.Add(button2);
		panel4.Controls.Add(button3);
		panel4.Controls.Add(button4);
		Panel panel5 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(12)
		};
		tableLayoutPanel.Controls.Add(panel5, 0, 2);
		panel5.Controls.Add(new Label
		{
			Left = 0,
			Top = 0,
			Width = 130,
			Height = 22,
			Text = "New Card ID",
			ForeColor = TextColor
		});
		_newCardIdBox = new TextBox
		{
			Left = 0,
			Top = 24,
			Width = 150,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle,
			PlaceholderText = "auto if empty"
		};
		panel5.Controls.Add(_newCardIdBox);
		panel5.Controls.Add(new Label
		{
			Left = 170,
			Top = 0,
			Width = 80,
			Height = 22,
			Text = "Mob ID",
			ForeColor = TextColor
		});
		_mobIdBox = new TextBox
		{
			Left = 170,
			Top = 24,
			Width = 140,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		panel5.Controls.Add(_mobIdBox);
		panel5.Controls.Add(new Label
		{
			Left = 0,
			Top = 64,
			Width = 130,
			Height = 22,
			Text = "Monster Name",
			ForeColor = TextColor
		});
		_nameBox = new TextBox
		{
			Left = 0,
			Top = 86,
			Width = 310,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		panel5.Controls.Add(_nameBox);
		panel5.Controls.Add(new Label
		{
			Left = 0,
			Top = 116,
			Width = 130,
			Height = 22,
			Text = "Description",
			ForeColor = TextColor
		});
		_descBox = new TextBox
		{
			Left = 0,
			Top = 138,
			Width = 510,
			Height = 34,
			Multiline = true,
			BackColor = SurfaceColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		panel5.Controls.Add(_descBox);
		Button button5 = CreateButton("Add New Card", 330, 22, 180, 34, primary: true);
		button5.Click += (object? _, EventArgs _) =>
		{
			AddNewCard();
		};
		panel5.Controls.Add(button5);
		_massAddButton = CreateButton("Mass Add...", 330, 62, 180, 34, primary: false);
		_massAddButton.Click += (object? _, EventArgs _) =>
		{
			OpenMassAddDialog();
		};
		panel5.Controls.Add(_massAddButton);
		Panel panel6 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(12)
		};
		tableLayoutPanel.Controls.Add(panel6, 0, 3);
		_replaceIconButton = CreateButton("Change icon", 0, 12, 140, 34, primary: false);
		_replaceIconRawButton = CreateButton("Change iconRaw", 150, 12, 140, 34, primary: false);
		_exportIconRawButton = CreateButton("Export iconRaw", 300, 12, 140, 34, primary: false);
		_saveSelectedButton = CreateButton("Save Selected", 450, 12, 140, 34, primary: false);
		_saveAllButton = CreateButton("Save All", 600, 12, 140, 34, primary: false);
		_saveAsImgButton = CreateButton("Save As IMG", 0, 54, 140, 34, primary: false);
		_saveAsXmlButton = CreateButton("Save As XML", 150, 54, 140, 34, primary: false);
		_saveBothButton = CreateButton("Save Both", 300, 54, 140, 34, primary: true);
		_deleteButton = CreateButton("Delete Selected", 450, 54, 140, 34, primary: false);
		_replaceIconButton.Click += (object? _, EventArgs _) =>
		{
			ReplaceCanvas("icon");
		};
		_replaceIconRawButton.Click += (object? _, EventArgs _) =>
		{
			ReplaceCanvas("iconRaw");
		};
		_exportIconRawButton.Click += (object? _, EventArgs _) =>
		{
			ExportSelectedIconRaw();
		};
		_saveSelectedButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelected();
		};
		_saveAllButton.Click += (object? _, EventArgs _) =>
		{
			SaveAll();
		};
		_saveAsImgButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedAsImg();
		};
		_saveAsXmlButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedAsXml();
		};
		_saveBothButton.Click += (object? _, EventArgs _) =>
		{
			SaveSelectedAsBoth();
		};
		_deleteButton.Click += (object? _, EventArgs _) =>
		{
			DeleteSelectedCards();
		};
		panel6.Controls.Add(_replaceIconButton);
		panel6.Controls.Add(_replaceIconRawButton);
		panel6.Controls.Add(_exportIconRawButton);
		panel6.Controls.Add(_saveSelectedButton);
		panel6.Controls.Add(_saveAllButton);
		panel6.Controls.Add(_saveAsImgButton);
		panel6.Controls.Add(_saveAsXmlButton);
		panel6.Controls.Add(_saveBothButton);
		panel6.Controls.Add(_deleteButton);
		Panel panel7 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(12)
		};
		tableLayoutPanel.Controls.Add(panel7, 0, 4);
		panel7.Controls.Add(new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = SubtleTextColor,
			Text = "Codex-only editor: creates cards, edits mob id, and edits icon/iconRaw only."
		});
		UpdateSelectionState();
		_consumeStringPathBox.Text = Path.Combine(AppContext.BaseDirectory, "Consume.img");
	}

	private static Rectangle GetStartupBounds()
	{
		Rectangle workingArea = Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
		return new Rectangle(workingArea.Left, workingArea.Top, Math.Max(workingArea.Width, 1500), Math.Max(workingArea.Height, 920));
	}

	private MenuStrip BuildMenu()
	{
		MenuStrip menuStrip = new MenuStrip
		{
			BackColor = SurfaceColor,
			ForeColor = TextColor
		};
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("File");
		toolStripMenuItem.DropDownItems.Add("Open Data Folder...", null, (object? _, EventArgs _) =>
		{
			OpenDataFolder();
		});
		toolStripMenuItem.DropDownItems.Add("Open 0238.img Directly...", null, (object? _, EventArgs _) =>
		{
			OpenDirect();
		});
		toolStripMenuItem.DropDownItems.Add("Open Folder...", null, (object? _, EventArgs _) =>
		{
			OpenFolder();
		});
		toolStripMenuItem.DropDownItems.Add("Open Item.wz...", null, (object? _, EventArgs _) =>
		{
			OpenWzArchives();
		});
		toolStripMenuItem.DropDownItems.Add("Save WZ Archives", null, (object? _, EventArgs _) =>
		{
			SaveWzArchives(askConfirmation: true);
		});
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Save Selected", null, (object? _, EventArgs _) =>
		{
			SaveSelected();
		});
		toolStripMenuItem.DropDownItems.Add("Save All", null, (object? _, EventArgs _) =>
		{
			SaveAll();
		});
		toolStripMenuItem.DropDownItems.Add("Save 0238 As IMG...", null, (object? _, EventArgs _) =>
		{
			SaveSelectedAsImg();
		});
		toolStripMenuItem.DropDownItems.Add("Save 0238 As XML...", null, (object? _, EventArgs _) =>
		{
			SaveSelectedAsXml();
		});
		toolStripMenuItem.DropDownItems.Add("Save 0238 As Both...", null, (object? _, EventArgs _) =>
		{
			SaveSelectedAsBoth();
		});
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Save Consume As IMG...", null, (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsImg();
		});
		toolStripMenuItem.DropDownItems.Add("Save Consume As XML...", null, (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsXml();
		});
		toolStripMenuItem.DropDownItems.Add("Save Consume As Both...", null, (object? _, EventArgs _) =>
		{
			SaveConsumeSideAsBoth();
		});
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Exit", null, (object? _, EventArgs _) =>
		{
			Close();
		});
		menuStrip.Items.Add(toolStripMenuItem);
		ToolStripMenuItem versionMenu = new ToolStripMenuItem("Version");
		ToolStripMenuItem v83 = new ToolStripMenuItem("v83 (native)")
		{
			Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V83
		};
		v83.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V83);
		};
		ToolStripMenuItem v95 = new ToolStripMenuItem("v95")
		{
			Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95
		};
		v95.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V95);
		};
		versionMenu.DropDownItems.Add(v83);
		versionMenu.DropDownItems.Add(v95);
		versionMenu.DropDownOpening += (object? _, EventArgs _) =>
		{
			v83.Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V83;
			v95.Checked = MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95;
		};
		menuStrip.Items.Add(versionMenu);
		ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
		viewMenu.DropDownItems.Add("Preview Animations...", null, (object? _, EventArgs _) =>
		{
			PreviewSelectedAnimations();
		});
		menuStrip.Items.Add(viewMenu);
		return menuStrip;
	}

	private void PreviewSelectedAnimations()
	{
		CodexCardEntry? card = SelectedCard();
		if (card == null)
		{
			MessageBox.Show(this, "Select a codex card first.", "Animation preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		AnimationViewerForm.ShowFor(this, card.CardProperty, "Animations - " + card.CardId);
	}

	private void SetVersion(MapleEditorGameVersion version)
	{
		MapleEditorVersionSettings.Current = version;
		Text = "Maple Codex Creator - " + MapleEditorVersionSettings.Profile.DisplayName;
		_header.VersionText = MapleEditorVersionSettings.Profile.DisplayName;
		SetStatus("Data version set to " + MapleEditorVersionSettings.Profile.DisplayName);
	}

	private static Panel CreatePanel()
	{
		return new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = SurfaceColor,
			BorderStyle = BorderStyle.FixedSingle
		};
	}

	private PictureBox CreatePreview(string label, int x, int y, Control parent)
	{
		Label value = new Label
		{
			Left = x,
			Top = y - 20,
			Width = 120,
			Height = 18,
			Text = label,
			ForeColor = SubtleTextColor
		};
		parent.Controls.Add(value);
		PictureBox pictureBox = new PictureBox
		{
			Left = x,
			Top = y,
			Width = 128,
			Height = 88,
			BackColor = SurfaceColor,
			BorderStyle = BorderStyle.FixedSingle,
			SizeMode = PictureBoxSizeMode.Zoom
		};
		parent.Controls.Add(pictureBox);
		return pictureBox;
	}

	private Button CreateButton(string text, int left, int top, int width, int height, bool primary)
	{
		Button button = new RoundedButton
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = height
		};
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 1;
		button.FlatAppearance.BorderColor = (primary ? AccentColor : BorderColor);
		button.BackColor = (primary ? AccentColor : SurfaceColor);
		button.ForeColor = TextColor;
		button.Font = new Font("Bahnschrift SemiBold", 10.5f, FontStyle.Bold);
		return button;
	}

	private bool ConfirmDiscardWzChanges()
	{
		if ((_itemArchive?.IsDirty != true) && (_stringArchive?.IsDirty != true))
		{
			return true;
		}
		DialogResult result = MessageBox.Show(this, "Save staged WZ changes before continuing? Choose No to discard them.", "Unsaved WZ changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
		if (result == DialogResult.Cancel)
		{
			return false;
		}
		if (result == DialogResult.Yes)
		{
			SaveWzArchives(askConfirmation: false);
			return (_itemArchive?.IsDirty != true) && (_stringArchive?.IsDirty != true);
		}
		return true;
	}

	private void OpenWzArchives()
	{
		if (!ConfirmDiscardWzChanges())
		{
			return;
		}
		using OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Open Item.wz (v95). String.wz is loaded automatically from the same folder.",
			Filter = "WZ archives (*.wz)|*.wz|All files (*.*)|*.*",
			FileName = "Item.wz",
			InitialDirectory = MapleEditorVersionSettings.GetDefaultWzFolder()
		};
		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			LoadWzArchives(dialog.FileName);
		}
	}

	private void LoadWzArchives(string itemPath)
	{
		ClearLoaded();
		bool loaded = false;
		SplashForm splash = new SplashForm("Opening " + Path.GetFileName(itemPath), "Reading 0238.img and String.wz");
		splash.Show(this);
		splash.Report("Reading Item.wz...", -1.0);
		try
		{
			Cursor = Cursors.WaitCursor;
			SetStatus("Reading " + Path.GetFileName(itemPath) + "...");
			WzArchiveSession itemSession = WzArchiveSession.Open(itemPath);
			_itemArchive = itemSession;
			MapleEditorVersionSettings.Settings.LastItemWz = itemPath;
			MapleEditorVersionSettings.Settings.V95WzFolder = Path.GetDirectoryName(itemPath) ?? MapleEditorVersionSettings.Settings.V95WzFolder;
			string? stringPath = MapleEditorVersionSettings.FindSiblingWz(itemPath, "String.wz");
			if (stringPath != null)
			{
				try
				{
					_stringArchive = WzArchiveSession.Open(stringPath);
					_stringConsumeImage = _stringArchive.FindImage("Consume.img", recursive: false);
					_stringConsumeImage?.ParseImage();
					MapleEditorVersionSettings.Settings.LastStringWz = stringPath;
				}
				catch
				{
					_stringArchive?.Dispose();
					_stringArchive = null;
				}
			}
			MapleEditorVersionSettings.Save();
			WzImage? root = itemSession.FindImageByPath("Consume/0238.img") ?? itemSession.FindImage("0238.img");
			if (root == null)
			{
				throw new InvalidOperationException("Could not find Consume/0238.img in " + Path.GetFileName(itemPath) + ".");
			}
			root.ParseImage();
			_loadingSelection = true;
			_loadedPath = itemPath + "\\Consume\\0238.img";
			byte[] iv = WzTool.GetIvByMapleVersion(WzMapleVersion.GMS);
			int loadedCards = 0;
			splash.Report("Loading codex cards...", 0.0);
			foreach (CodexCardEntry item in LoadCardsFromRoot(_loadedPath, root, iv, $"WZ v{itemSession.GameVersion}", isXml: false))
			{
				item.ImageIndex = _images.Images.Count;
				_images.Images.Add(item.Thumbnail);
				_cards.Add(item);
				loadedCards++;
				if (loadedCards % 20 == 0)
				{
					splash.Report($"Loading codex cards {loadedCards}...", -1.0);
				}
			}
			RefreshList();
			if (_cardList.Items.Count > 0)
			{
				_cardList.Items[0].Selected = true;
			}
			_saveAllButton.Enabled = _cards.Count > 0;
			_consumeStringPathBox.Text = ((stringPath != null) ? (stringPath + "\\Consume.img") : string.Empty);
			_header.RightText = Path.GetFileName(itemPath) + " � 0238";
			splash.Report("Ready.", 1.0);
			SetStatus($"Loaded {_cards.Count} cards from {Path.GetFileName(itemPath)}\\Consume\\0238.img" + ((_stringArchive != null) ? " + String.wz" : " (String.wz not found)"));
			loaded = true;
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
			UpdateSelectionState();
			if (!loaded)
			{
				CloseArchiveSessions();
			}
		}
	}

	private void SaveWzArchives(bool askConfirmation)
	{
		bool itemDirty = _itemArchive?.IsDirty == true;
		bool stringDirty = _stringArchive?.IsDirty == true;
		if (!itemDirty && !stringDirty)
		{
			SetStatus("No staged WZ changes to save.");
			return;
		}
		if (askConfirmation)
		{
			string files = itemDirty ? _itemArchive!.FileName : string.Empty;
			if (stringDirty)
			{
				files = (files.Length == 0) ? _stringArchive!.FileName : (files + " and " + _stringArchive!.FileName);
			}
			if (MessageBox.Show(this, "Write changes to " + files + "?\n\nThis repacks the archive(s). Large WZ files can take a while. A .bak backup is created next to each archive.", "Save WZ archives", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
			{
				return;
			}
		}
		Cursor = Cursors.WaitCursor;
		SplashForm splash = new SplashForm("Saving codex WZ archives", "Repacking Item.wz and String.wz. This can take a few minutes.");
		splash.Show(this);
		splash.Report("Repacking WZ archives...", -1.0);
		try
		{
			if (itemDirty)
			{
				SetStatus("Repacking " + _itemArchive!.FileName + "...");
				_itemArchive.Save();
				foreach (CodexCardEntry card in _cards)
				{
					card.IsDirty = false;
				}
			}
			if (stringDirty)
			{
				SetStatus("Repacking " + _stringArchive!.FileName + "...");
				_stringArchive.Save();
			}
			splash.Report("Saved.", 1.0);
			SetStatus("Saved WZ archives.");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "WZ save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			splash.Close();
			splash.Dispose();
			Cursor = Cursors.Default;
		}
	}

	private void CloseArchiveSessions()
	{
		_itemArchive?.Dispose();
		_itemArchive = null;
		_stringArchive?.Dispose();
		_stringArchive = null;
		_stringConsumeImage = null;
	}

	private void OpenDirect()
	{
		if (!ConfirmDiscardWzChanges())
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Codex|0238.img;0238.img.xml|All|*.img;*.img.xml",
			Title = "Open 0238.img or 0238.img.xml"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				LoadSingleFile(openFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private void BrowseConsumeString()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Consume files|*.img;*.xml|IMG/XML files|*.img;*.xml|All files (*.*)|*.*"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				_consumeStringPathBox.Text = openFileDialog.FileName;
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private void OpenFolder()
	{
		if (!ConfirmDiscardWzChanges())
		{
			return;
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select folder containing 0238.img or 0238.img.xml"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
			{
				string text = Directory.GetFiles(folderBrowserDialog.SelectedPath, "0238.img", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? Directory.GetFiles(folderBrowserDialog.SelectedPath, "0238.img.xml", SearchOption.TopDirectoryOnly).FirstOrDefault();
				if (text == null)
				{
					MessageBox.Show(this, "Could not find 0238.img or 0238.img.xml in folder.", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					LoadSingleFile(text);
				}
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private void OpenDataFolder()
	{
		if (!ConfirmDiscardWzChanges())
		{
			return;
		}
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select Data folder (or folder containing Data)"
		};
		try
		{
			if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			string selectedPath = folderBrowserDialog.SelectedPath;
			string path = (string.Equals(Path.GetFileName(selectedPath), "Data", StringComparison.OrdinalIgnoreCase) ? selectedPath : Path.Combine(selectedPath, "Data"));
			string text = FirstExisting(Path.Combine(path, "Item", "Consume", "0238.img"), Path.Combine(path, "Item", "Consume", "0238.img.xml"));
			string text2 = FirstExisting(Path.Combine(path, "String", "Consume.img"), Path.Combine(path, "String", "Consume.img.xml"));
			if (text.Length == 0)
			{
				MessageBox.Show(this, "Could not find Data\\Item\\Consume\\0238.img(.xml).", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (text2.Length > 0)
			{
				_consumeStringPathBox.Text = text2;
			}
			LoadSingleFile(text);
			SetStatus("Auto-loaded 0238 + Consume from Data.");
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private static string FirstExisting(params string[] paths)
	{
		foreach (string text in paths)
		{
			if (File.Exists(text))
			{
				return text;
			}
		}
		return string.Empty;
	}

	private void LoadSingleFile(string path)
	{
		ClearLoaded();
		try
		{
			_loadingSelection = true;
			_loadedPath = path;
			foreach (CodexCardEntry item in LoadCards(path))
			{
				item.ImageIndex = _images.Images.Count;
				_images.Images.Add(item.Thumbnail);
				_cards.Add(item);
			}
			RefreshList();
			if (_cardList.Items.Count > 0)
			{
				_cardList.Items[0].Selected = true;
			}
			_saveAllButton.Enabled = _cards.Count > 0;
			SetStatus($"Loaded {_cards.Count} cards from {Path.GetFileName(path)}");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_loadingSelection = false;
			UpdateSelectionState();
		}
	}

	private IEnumerable<CodexCardEntry> LoadCards(string path)
	{
		bool isXml = path.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase);
		WzImage root;
		byte[] iv;
		string ivLabel;
		if (isXml)
		{
			root = _xmlDocumentService.LoadFromXml(path, out var _);
			iv = Array.Empty<byte>();
			ivLabel = "XML";
		}
		else
		{
			root = LoadImgWithDetectedIv(path, out iv, out ivLabel);
		}
		return LoadCardsFromRoot(path, root, iv, ivLabel, isXml);
	}

	private IEnumerable<CodexCardEntry> LoadCardsFromRoot(string path, WzImage root, byte[] iv, string ivLabel, bool isXml)
	{
		WzSubProperty info = default;
		foreach (WzImageProperty property in root.WzProperties)
		{
			WzSubProperty card = property as WzSubProperty;
			int num;
			if (card != null)
			{
				WzImageProperty wzImageProperty = card["info"];
				info = wzImageProperty as WzSubProperty;
				num = ((info == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num == 0)
			{
				yield return new CodexCardEntry(path, root, card, info, iv, ivLabel, isXml);
				info = null;
			}
		}
	}

	private static WzImage LoadImgWithDetectedIv(string path, out byte[] iv, out string ivLabel)
	{
		(WzMapleVersion, string)[] array = new (WzMapleVersion, string)[3]
		{
			(WzMapleVersion.GMS, "GMS"),
			(WzMapleVersion.EMS, "EMS/MSEA"),
			(WzMapleVersion.BMS, "BMS/Classic")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(WzMapleVersion, string) tuple = array[i];
			WzImgDeserializer wzImgDeserializer = new WzImgDeserializer(freeResources: true);
			WzImage wzImage = wzImgDeserializer.WzImageFromIMGFile(path, WzTool.GetIvByMapleVersion(tuple.Item1), Path.GetFileName(path), out var successfullyParsedImage);
			if (!successfullyParsedImage)
			{
				wzImage.Dispose();
				continue;
			}
			iv = WzTool.GetIvByMapleVersion(tuple.Item1);
			ivLabel = tuple.Item2;
			return wzImage;
		}
		throw new InvalidOperationException("Unable to parse IMG with known IV profiles.");
	}

	private void RefreshList()
	{
		string q = _searchBox.Text.Trim();
		_loadingSelection = true;
		_cardList.BeginUpdate();
		_cardList.Items.Clear();
		IEnumerable<CodexCardEntry> source = _cards;
		if (!string.IsNullOrWhiteSpace(q))
		{
			source = source.Where((CodexCardEntry c) => c.CardId.Contains(q, StringComparison.OrdinalIgnoreCase) || c.MobId.ToString(CultureInfo.InvariantCulture).Contains(q, StringComparison.OrdinalIgnoreCase));
		}
		foreach (CodexCardEntry item in source.OrderBy((CodexCardEntry c) => c.CardId))
		{
			ListViewItem value = new ListViewItem($"{item.CardId}  mob:{item.MobId}", item.ImageIndex)
			{
				Tag = item
			};
			_cardList.Items.Add(value);
		}
		_cardList.EndUpdate();
		_loadingSelection = false;
	}

	private CodexCardEntry? SelectedCard()
	{
		return (_cardList.SelectedItems.Count == 0) ? null : (_cardList.SelectedItems[0].Tag as CodexCardEntry);
	}

	private void CardList_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Control && e.KeyCode == Keys.C)
		{
			CopySelectedCardIds();
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
	}

	private void CopySelectedCardIds()
	{
		if (_cardList.SelectedItems.Count == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (ListViewItem selectedItem in _cardList.SelectedItems)
		{
			if (selectedItem.Tag is CodexCardEntry codexCardEntry)
			{
				list.Add(codexCardEntry.CardId);
			}
		}
		if (list.Count != 0)
		{
			Clipboard.SetText(string.Join(Environment.NewLine, list));
			SetStatus($"Copied {list.Count} card ID{((list.Count == 1) ? string.Empty : "s")}.");
		}
	}

	private void UpdateSelectionState()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		bool flag = codexCardEntry != null;
		_replaceIconButton.Enabled = flag;
		_replaceIconRawButton.Enabled = flag;
		_exportIconRawButton.Enabled = flag;
		_saveSelectedButton.Enabled = flag;
		_saveAsImgButton.Enabled = flag;
		_saveAsXmlButton.Enabled = flag;
		_saveBothButton.Enabled = flag;
		_deleteButton.Enabled = flag;
		if (!flag || codexCardEntry == null)
		{
			_selectedCardLabel.Text = "No card selected";
			_selectedFileLabel.Text = _loadedPath ?? "-";
			_mobIdBox.Text = "";
			_nameBox.Text = "";
			_descBox.Text = "";
			SetPreview(_iconPreview, null);
			SetPreview(_iconRawPreview, null);
			return;
		}
		if (_cardList.SelectedItems.Count > 1)
		{
			_selectedCardLabel.Text = $"{_cardList.SelectedItems.Count} cards selected";
		}
		else
		{
			_selectedCardLabel.Text = "Card " + codexCardEntry.CardId;
		}
		_selectedFileLabel.Text = codexCardEntry.FilePath + " | " + codexCardEntry.IvLabel;
		_mobIdBox.Text = codexCardEntry.MobId.ToString(CultureInfo.InvariantCulture);
		var (text, text2) = TryReadConsumeText(codexCardEntry.CardId);
		_nameBox.Text = text ?? string.Empty;
		_descBox.Text = text2 ?? string.Empty;
		SetPreview(_iconPreview, ResolveCanvas(codexCardEntry.Info, "icon"));
		SetPreview(_iconRawPreview, ResolveCanvas(codexCardEntry.Info, "iconRaw"));
	}

	private void SetPreview(PictureBox box, WzCanvasProperty? canvas)
	{
		box.Image?.Dispose();
		box.Image = null;
		if (canvas == null)
		{
			return;
		}
		using Bitmap? bitmap = WzAnimationReader.GetCanvasBitmap(canvas);
		if (bitmap == null)
		{
			return;
		}
		box.Image = (Bitmap)bitmap.Clone();
	}

	private void AddNewCard()
	{
		CodexCardEntry codexCardEntry = SelectedCard() ?? _cards.FirstOrDefault();
		if (codexCardEntry == null)
		{
			MessageBox.Show(this, "Open 0238 first.", "No file loaded", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (!int.TryParse(_mobIdBox.Text.Trim(), out var result))
		{
			MessageBox.Show(this, "Mob ID must be numeric.", "Invalid Mob ID", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		string text = ResolveNextCardId(codexCardEntry.RootImage, _newCardIdBox.Text.Trim());
		if (codexCardEntry.RootImage[text] != null)
		{
			MessageBox.Show(this, "Card " + text + " already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		WzSubProperty wzSubProperty = new WzSubProperty(text);
		WzSubProperty wzSubProperty2 = new WzSubProperty("info");
		WzSubProperty wzSubProperty3 = new WzSubProperty("spec");
		wzSubProperty.AddProperty(wzSubProperty2);
		wzSubProperty.AddProperty(wzSubProperty3);
		wzSubProperty2.AddProperty(CloneOrCreateCanvas(codexCardEntry.Info, "icon", 32, 32, 0, 32));
		wzSubProperty2.AddProperty(CloneOrCreateCanvas(codexCardEntry.Info, "iconRaw", 27, 38, 0, 38));
		wzSubProperty2.AddProperty(new WzIntProperty("price", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("tradeBlock", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("bigSize", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("only", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("monsterBook", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("mob", result));
		wzSubProperty3.AddProperty(new WzIntProperty("consumeOnPickup", 1));
		codexCardEntry.RootImage.AddProperty(wzSubProperty);
		codexCardEntry.RootImage.Changed = true;
		CodexCardEntry codexCardEntry2 = new CodexCardEntry(codexCardEntry.FilePath, codexCardEntry.RootImage, wzSubProperty, wzSubProperty2, codexCardEntry.Iv, codexCardEntry.IvLabel, codexCardEntry.IsXml)
		{
			IsDirty = true
		};
		codexCardEntry2.ImageIndex = _images.Images.Count;
		_images.Images.Add(codexCardEntry2.Thumbnail);
		_cards.Add(codexCardEntry2);
		RefreshList();
		SelectCardById(text);
		if (!string.IsNullOrWhiteSpace(_nameBox.Text))
		{
			UpsertConsumeText(text, _nameBox.Text.Trim(), _descBox.Text.Trim());
		}
		SetStatus($"Added card {text} (mob {result}).");
	}

	private void OpenMassAddDialog()
	{
		CodexCardEntry codexCardEntry = SelectedCard() ?? _cards.FirstOrDefault();
		if (codexCardEntry == null)
		{
			MessageBox.Show(this, "Open 0238 first.", "No file loaded", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		using MassAddCardsForm massAddCardsForm = new MassAddCardsForm();
		if (massAddCardsForm.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		IReadOnlyList<MassAddCardInput> rows = massAddCardsForm.GetRows();
		if (rows.Count == 0)
		{
			return;
		}
		List<string> list = ValidateMassAddRows(codexCardEntry.RootImage, rows);
		if (list.Count > 0)
		{
			MessageBox.Show(this, string.Join(Environment.NewLine, list), "Mass add validation failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		HashSet<string> hashSet = _cards.Select((CodexCardEntry c) => c.CardId).ToHashSet(StringComparer.Ordinal);
		List<string> list2 = new List<string>();
		foreach (MassAddCardInput item in rows)
		{
			string text = ResolveRowCardId(codexCardEntry.RootImage, item.CardId, hashSet);
			hashSet.Add(text);
			AddCardFromInput(codexCardEntry, item, text);
			list2.Add(text);
		}
		RefreshList();
		SelectCardsByIds(list2);
		SetStatus($"Added {list2.Count} cards.");
	}

	private List<string> ValidateMassAddRows(WzImage rootImage, IReadOnlyList<MassAddCardInput> rows)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = _cards.Select((CodexCardEntry c) => c.CardId).ToHashSet(StringComparer.Ordinal);
		int num = 0;
		for (int num2 = 0; num2 < rows.Count; num2++)
		{
			int value = num2 + 1;
			MassAddCardInput massAddCardInput = rows[num2];
			if (!int.TryParse(massAddCardInput.MobId, out var _))
			{
				list.Add($"Row {value}: Mob ID must be numeric.");
			}
			if (string.IsNullOrWhiteSpace(massAddCardInput.IconPath) || !File.Exists(massAddCardInput.IconPath))
			{
				list.Add($"Row {value}: Icon file is missing.");
			}
			if (string.IsNullOrWhiteSpace(massAddCardInput.IconRawPath) || !File.Exists(massAddCardInput.IconRawPath))
			{
				list.Add($"Row {value}: IconRaw file is missing.");
			}
			string text = massAddCardInput.CardId.Trim();
			if (text.Length == 0)
			{
				num++;
				continue;
			}
			if (!int.TryParse(text, out var result2))
			{
				list.Add($"Row {value}: Card ID must be numeric.");
				continue;
			}
			string text2 = result2.ToString("D8", CultureInfo.InvariantCulture);
			if (!hashSet.Add(text2) || rootImage[text2] != null)
			{
				list.Add($"Row {value}: Card ID {text2} already exists.");
			}
		}
		if (list.Count > 0)
		{
			return list;
		}
		if (num > 0)
		{
			int num3 = 2380000;
			foreach (WzImageProperty wzProperty in rootImage.WzProperties)
			{
				if (int.TryParse(wzProperty.Name, out var result3) && result3 > num3)
				{
					num3 = result3;
				}
			}
			for (int num4 = 0; num4 < num; num4++)
			{
				string item = (++num3).ToString("D8", CultureInfo.InvariantCulture);
				if (!hashSet.Add(item))
				{
					list.Add("Unable to generate unique card IDs for all rows.");
					break;
				}
			}
		}
		return list;
	}

	private static string ResolveRowCardId(WzImage rootImage, string requestedCardId, HashSet<string> usedIds)
	{
		string text = requestedCardId.Trim();
		if (text.Length > 0)
		{
			return int.Parse(text, CultureInfo.InvariantCulture).ToString("D8", CultureInfo.InvariantCulture);
		}
		int num = 2380000;
		foreach (WzImageProperty wzProperty in rootImage.WzProperties)
		{
			if (int.TryParse(wzProperty.Name, out var result) && result > num)
			{
				num = result;
			}
		}
		string text2;
		do
		{
			text2 = (++num).ToString("D8", CultureInfo.InvariantCulture);
		}
		while (usedIds.Contains(text2));
		return text2;
	}

	private void AddCardFromInput(CodexCardEntry baseCard, MassAddCardInput row, string cardId)
	{
		int value = int.Parse(row.MobId, CultureInfo.InvariantCulture);
		WzSubProperty wzSubProperty = new WzSubProperty(cardId);
		WzSubProperty wzSubProperty2 = new WzSubProperty("info");
		WzSubProperty wzSubProperty3 = new WzSubProperty("spec");
		wzSubProperty.AddProperty(wzSubProperty2);
		wzSubProperty.AddProperty(wzSubProperty3);
		wzSubProperty2.AddProperty(CloneOrCreateCanvas(baseCard.Info, "icon", 32, 32, 0, 32));
		wzSubProperty2.AddProperty(CloneOrCreateCanvas(baseCard.Info, "iconRaw", 27, 38, 0, 38));
		wzSubProperty2.AddProperty(new WzIntProperty("price", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("tradeBlock", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("bigSize", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("only", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("monsterBook", 1));
		wzSubProperty2.AddProperty(new WzIntProperty("mob", value));
		wzSubProperty3.AddProperty(new WzIntProperty("consumeOnPickup", 1));
		baseCard.RootImage.AddProperty(wzSubProperty);
		baseCard.RootImage.Changed = true;
		WzCanvasProperty canvas = ResolveCanvas(wzSubProperty2, "icon");
		using (Bitmap bitmap = new Bitmap(row.IconPath))
		{
			UpdateCanvasBitmap(canvas, bitmap);
		}
		WzCanvasProperty canvas2 = ResolveCanvas(wzSubProperty2, "iconRaw");
		using (Bitmap bitmap2 = new Bitmap(row.IconRawPath))
		{
			UpdateCanvasBitmap(canvas2, bitmap2, WzPngFormat.Format2);
		}
		CodexCardEntry codexCardEntry = new CodexCardEntry(baseCard.FilePath, baseCard.RootImage, wzSubProperty, wzSubProperty2, baseCard.Iv, baseCard.IvLabel, baseCard.IsXml)
		{
			IsDirty = true
		};
		codexCardEntry.ImageIndex = _images.Images.Count;
		_images.Images.Add(codexCardEntry.Thumbnail);
		_cards.Add(codexCardEntry);
		if (!string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Description))
		{
			UpsertConsumeText(cardId, row.Name.Trim(), row.Description.Trim());
		}
	}

	private static string ResolveNextCardId(WzImage root, string requested)
	{
		if (!string.IsNullOrWhiteSpace(requested))
		{
			if (!int.TryParse(requested, out var result))
			{
				throw new InvalidOperationException("Card ID must be numeric.");
			}
			return result.ToString("D8", CultureInfo.InvariantCulture);
		}
		int num = 2380000;
		foreach (WzImageProperty wzProperty in root.WzProperties)
		{
			if (int.TryParse(wzProperty.Name, out var result2) && result2 > num)
			{
				num = result2;
			}
		}
		return (num + 1).ToString("D8", CultureInfo.InvariantCulture);
	}

	private static WzCanvasProperty CloneOrCreateCanvas(WzSubProperty templateInfo, string name, int width, int height, int ox, int oy)
	{
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(name)
		{
			PngProperty = new WzPngProperty()
		};
		WzCanvasProperty wzCanvasProperty2 = ResolveCanvas(templateInfo, name);
		if (wzCanvasProperty2 != null)
		{
			using Bitmap? bitmap = WzAnimationReader.GetCanvasBitmap(wzCanvasProperty2);
			if (bitmap != null)
			{
				wzCanvasProperty.PngProperty.SetBitmapPreservingFormat(bitmap);
			}
			else
			{
				using Bitmap empty = new Bitmap(width, height);
				using Graphics graphics2 = Graphics.FromImage(empty);
				graphics2.Clear(Color.Transparent);
				wzCanvasProperty.PngProperty.SetBitmapPreservingFormat(empty);
			}
		}
		else
		{
			using Bitmap bitmap2 = new Bitmap(width, height);
			using Graphics graphics = Graphics.FromImage(bitmap2);
			graphics.Clear(Color.Transparent);
			wzCanvasProperty.PngProperty.SetBitmapPreservingFormat(bitmap2);
		}
		wzCanvasProperty.AddProperty(new WzVectorProperty("origin", ox, oy));
		return wzCanvasProperty;
	}

	private void ReplaceCanvas(string canvasName)
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry == null)
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "PNG (*.png)|*.png|All files (*.*)|*.*"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			WzCanvasProperty wzCanvasProperty = ResolveCanvas(codexCardEntry.Info, canvasName) ?? CloneOrCreateCanvas(codexCardEntry.Info, canvasName, (canvasName == "iconRaw") ? 27 : 32, (canvasName == "iconRaw") ? 38 : 32, 0, (canvasName == "iconRaw") ? 38 : 32);
			if (codexCardEntry.Info[canvasName] == null)
			{
				codexCardEntry.Info.AddProperty(wzCanvasProperty);
			}
			using Bitmap bitmap = new Bitmap(openFileDialog.FileName);
			WzPngFormat? forcedFormat = ((canvasName == "iconRaw") ? new WzPngFormat?(WzPngFormat.Format2) : ((WzPngFormat?)null));
			UpdateCanvasBitmap(wzCanvasProperty, bitmap, forcedFormat);
			codexCardEntry.RootImage.Changed = true;
			codexCardEntry.IsDirty = true;
			codexCardEntry.RefreshThumbnail();
			_images.Images[codexCardEntry.ImageIndex] = codexCardEntry.Thumbnail;
			SetPreview((canvasName == "icon") ? _iconPreview : _iconRawPreview, wzCanvasProperty);
			SetStatus($"Updated {canvasName} for {codexCardEntry.CardId}.");
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private static void UpdateCanvasBitmap(WzCanvasProperty canvas, Bitmap bitmap, WzPngFormat? forcedFormat = null)
	{
		if (canvas.PngProperty == null)
		{
			WzPngProperty wzPngProperty = (canvas.PngProperty = new WzPngProperty());
		}
		canvas.RemoveProperty(WzCanvasProperty.InlinkPropertyName);
		canvas.RemoveProperty(WzCanvasProperty.OutlinkPropertyName);
		WzPngFormat? targetFormat = forcedFormat;
		if (!targetFormat.HasValue && canvas.PngProperty.Width > 0 && canvas.PngProperty.Height > 0)
		{
			targetFormat = canvas.PngProperty.Format;
		}
		using Bitmap bitmap2 = (Bitmap)bitmap.Clone();
		canvas.PngProperty.SetBitmapPreservingFormat(bitmap2, targetFormat);
	}

	private void SaveSelected()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry != null)
		{
			TryApplyMobId(codexCardEntry);
			UpsertConsumeText(codexCardEntry.CardId, _nameBox.Text.Trim(), _descBox.Text.Trim());
			SaveEntry(codexCardEntry);
			SetStatus("Saved " + codexCardEntry.CardId);
			if (_itemArchive != null || _stringArchive != null)
			{
				SaveWzArchives(askConfirmation: true);
			}
		}
	}

	private void DeleteSelectedCards()
	{
		List<CodexCardEntry> list = new List<CodexCardEntry>();
		foreach (ListViewItem selectedItem in _cardList.SelectedItems)
		{
			if (selectedItem.Tag is CodexCardEntry item)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		DialogResult dialogResult = MessageBox.Show(this, (list.Count == 1) ? ("Delete card " + list[0].CardId + "?") : $"Delete {list.Count} selected cards?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
		if (dialogResult != DialogResult.Yes)
		{
			return;
		}
		foreach (CodexCardEntry item2 in list)
		{
			item2.RootImage.RemoveProperty(item2.CardId);
			item2.RootImage.Changed = true;
			_cards.Remove(item2);
			item2.Dispose();
		}
		RefreshList();
		UpdateSelectionState();
		SetStatus((list.Count == 1) ? "Deleted 1 card." : $"Deleted {list.Count} cards.");
	}

	private void ExportSelectedIconRaw()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry == null)
		{
			return;
		}
		WzCanvasProperty wzCanvasProperty = ResolveCanvas(codexCardEntry.Info, "iconRaw");
		if (wzCanvasProperty == null)
		{
			MessageBox.Show(this, "Card " + codexCardEntry.CardId + " does not contain 'iconRaw'.", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "PNG files (*.png)|*.png",
			FileName = codexCardEntry.CardId + ".iconRaw.png"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			string text = (saveFileDialog.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? saveFileDialog.FileName : (saveFileDialog.FileName.TrimEnd('.') + ".png"));
			try
			{
				using Bitmap? bitmap = WzAnimationReader.GetCanvasBitmap(wzCanvasProperty) ?? throw new InvalidOperationException("The selected canvas has no readable image.");
				bitmap.Save(text);
				SetStatus("Exported iconRaw: " + text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveAll()
	{
		if (_cards.Count == 0)
		{
			return;
		}
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry != null)
		{
			TryApplyMobId(codexCardEntry);
			UpsertConsumeText(codexCardEntry.CardId, _nameBox.Text.Trim(), _descBox.Text.Trim());
		}
		foreach (CodexCardEntry item in _cards.Where((CodexCardEntry c) => c.IsDirty))
		{
			SaveEntry(item);
		}
		SetStatus("Saved all codex changes.");
		if (_itemArchive != null || _stringArchive != null)
		{
			SaveWzArchives(askConfirmation: true);
		}
	}

	private void SaveSelectedAsImg()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry == null)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "IMG files (*.img)|*.img",
			FileName = "0238.img"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				ExportRootAsImg(codexCardEntry, saveFileDialog.FileName);
				SetStatus("Saved IMG: " + saveFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveSelectedAsXml()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry == null)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "XML files (*.xml)|*.xml",
			FileName = "0238.img.xml"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				ExportRootAsXml(codexCardEntry, saveFileDialog.FileName);
				SetStatus("Saved XML: " + saveFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveSelectedAsBoth()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry == null)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "IMG files (*.img)|*.img",
			FileName = "0238.img"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				string fileName = saveFileDialog.FileName;
				string text = fileName + ".xml";
				ExportRootAsImg(codexCardEntry, fileName);
				ExportRootAsXml(codexCardEntry, text);
				SetStatus("Saved both: " + fileName + " and " + text);
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveConsumeSideAsImg()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry != null)
		{
			UpsertConsumeText(codexCardEntry.CardId, _nameBox.Text.Trim(), _descBox.Text.Trim());
		}
		string path = _consumeStringPathBox.Text.Trim();
		if (!File.Exists(path) && _stringConsumeImage == null)
		{
			MessageBox.Show(this, "String-side source file does not exist.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "IMG files (*.img)|*.img",
			FileName = "Consume.img"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			WzImage wzImage = LoadConsumeSideImage(path, out byte[] iv);
			try
			{
				WzImgSerializer wzImgSerializer = new WzImgSerializer((iv.Length == 0) ? WzTool.GetIvByMapleVersion(WzMapleVersion.GMS) : iv);
				wzImgSerializer.SerializeImage(wzImage, saveFileDialog.FileName);
				SetStatus("Saved string IMG: " + saveFileDialog.FileName);
			}
			finally
			{
				wzImage.Dispose();
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveConsumeSideAsXml()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry != null)
		{
			UpsertConsumeText(codexCardEntry.CardId, _nameBox.Text.Trim(), _descBox.Text.Trim());
		}
		string path = _consumeStringPathBox.Text.Trim();
		if (!File.Exists(path) && _stringConsumeImage == null)
		{
			MessageBox.Show(this, "String-side source file does not exist.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "XML files (*.xml)|*.xml",
			FileName = "Consume.img.xml"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			WzImage wzImage = LoadConsumeSideImage(path, out byte[] _);
			try
			{
				_xmlDocumentService.SaveToXml(wzImage, saveFileDialog.FileName);
				SetStatus("Saved string XML: " + saveFileDialog.FileName);
			}
			finally
			{
				wzImage.Dispose();
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void SaveConsumeSideAsBoth()
	{
		CodexCardEntry codexCardEntry = SelectedCard();
		if (codexCardEntry != null)
		{
			UpsertConsumeText(codexCardEntry.CardId, _nameBox.Text.Trim(), _descBox.Text.Trim());
		}
		string path = _consumeStringPathBox.Text.Trim();
		if (!File.Exists(path) && _stringConsumeImage == null)
		{
			MessageBox.Show(this, "String-side source file does not exist.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "IMG files (*.img)|*.img",
			FileName = "Consume.img"
		};
		try
		{
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			string fileName = saveFileDialog.FileName;
			string text = fileName + ".xml";
			WzImage wzImage = LoadConsumeSideImage(path, out byte[] iv);
			try
			{
				WzImgSerializer wzImgSerializer = new WzImgSerializer((iv.Length == 0) ? WzTool.GetIvByMapleVersion(WzMapleVersion.GMS) : iv);
				wzImgSerializer.SerializeImage(wzImage, fileName);
				_xmlDocumentService.SaveToXml(wzImage, text);
				SetStatus("Saved string both: " + fileName + " and " + text);
			}
			finally
			{
				wzImage.Dispose();
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private WzImage LoadConsumeSideImage(string path, out byte[] iv)
	{
		if (_stringConsumeImage != null && !File.Exists(path))
		{
			iv = WzTool.GetIvByMapleVersion(WzMapleVersion.GMS);
			WzImage clone = new WzImage(_stringConsumeImage.Name);
			foreach (WzImageProperty property in _stringConsumeImage.WzProperties)
			{
				clone.AddProperty(property.DeepClone());
			}
			clone.MarkWzImageAsParsed();
			clone.Changed = true;
			return clone;
		}
		if (path.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase) || (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".img", StringComparison.OrdinalIgnoreCase)))
		{
			iv = Array.Empty<byte>();
			bool hasCanvasPlaceholders;
			return _xmlDocumentService.LoadFromXml(path, out hasCanvasPlaceholders);
		}
		WzImage result = LoadImgWithDetectedIv(path, out byte[] iv2, out string _);
		iv = iv2;
		return result;
	}

	private static void ExportRootAsImg(CodexCardEntry selected, string path)
	{
		WzImage wzImage = new WzImage("0238.img");
		foreach (WzImageProperty wzProperty in selected.RootImage.WzProperties)
		{
			wzImage.AddProperty(wzProperty.DeepClone());
		}
		wzImage.MarkWzImageAsParsed();
		wzImage.Changed = true;
		try
		{
			WzImgSerializer wzImgSerializer = new WzImgSerializer((selected.Iv.Length == 0) ? WzTool.GetIvByMapleVersion(WzMapleVersion.GMS) : selected.Iv);
			wzImgSerializer.SerializeImage(wzImage, path);
		}
		finally
		{
			wzImage.Dispose();
		}
	}

	private void ExportRootAsXml(CodexCardEntry selected, string path)
	{
		WzImage wzImage = new WzImage("0238.img");
		foreach (WzImageProperty wzProperty in selected.RootImage.WzProperties)
		{
			wzImage.AddProperty(wzProperty.DeepClone());
		}
		wzImage.MarkWzImageAsParsed();
		wzImage.Changed = true;
		try
		{
			_xmlDocumentService.SaveToXml(wzImage, path);
		}
		finally
		{
			wzImage.Dispose();
		}
	}

	private void TryApplyMobId(CodexCardEntry selected)
	{
		if (int.TryParse(_mobIdBox.Text.Trim(), out var result))
		{
			if (selected.Info["mob"] is WzIntProperty wzIntProperty)
			{
				wzIntProperty.Value = result;
			}
			else
			{
				selected.Info.RemoveProperty("mob");
				selected.Info.AddProperty(new WzIntProperty("mob", result));
			}
			selected.IsDirty = true;
			selected.RootImage.Changed = true;
			selected.MobId = result;
			RefreshList();
			SelectCardById(selected.CardId);
		}
	}

	private void SaveEntry(CodexCardEntry entry)
	{
		if (_itemArchive != null)
		{
			entry.RootImage.Changed = true;
			_itemArchive.MarkDirty();
			entry.IsDirty = true;
			return;
		}
		if (entry.IsXml)
		{
			_xmlDocumentService.SaveToXml(entry.RootImage, entry.FilePath);
		}
		else
		{
			WzImgSerializer wzImgSerializer = new WzImgSerializer(entry.Iv);
			wzImgSerializer.SerializeImage(entry.RootImage, entry.FilePath);
		}
		entry.IsDirty = false;
	}

	private void SelectCardById(string cardId)
	{
		foreach (ListViewItem item in _cardList.Items)
		{
			if (item.Tag is CodexCardEntry codexCardEntry && codexCardEntry.CardId == cardId)
			{
				item.Selected = true;
				item.EnsureVisible();
				break;
			}
		}
	}

	private void SelectCardsByIds(IReadOnlyCollection<string> cardIds)
	{
		_loadingSelection = true;
		foreach (ListViewItem item in _cardList.Items)
		{
			item.Selected = item.Tag is CodexCardEntry codexCardEntry && cardIds.Contains(codexCardEntry.CardId);
			if (item.Selected)
			{
				item.EnsureVisible();
			}
		}
		_loadingSelection = false;
		UpdateSelectionState();
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

	private void ClearLoaded()
	{
		CloseArchiveSessions();
		foreach (CodexCardEntry card in _cards)
		{
			card.Dispose();
		}
		_cards.Clear();
		_images.Images.Clear();
		_cardList.Items.Clear();
	}

	private void SetStatus(string text)
	{
		_statusLabel.Text = text;
	}

	private (string? Name, string? Desc) TryReadConsumeText(string cardId)
	{
		if (_stringConsumeImage != null)
		{
			string archiveCardId = ToConsumeCardId(cardId);
			WzSubProperty? archiveEntry = FindCardEntry(_stringConsumeImage, archiveCardId);
			if (archiveEntry == null)
			{
				WzSubProperty? archiveGroup = FindConsumeGroup(_stringConsumeImage);
				archiveEntry = ((archiveGroup == null) ? null : FindCardEntry(archiveGroup, archiveCardId));
			}
			if (archiveEntry != null)
			{
				return (Name: (archiveEntry["name"] as WzStringProperty)?.Value, Desc: (archiveEntry["desc"] as WzStringProperty)?.Value);
			}
			return (Name: null, Desc: null);
		}
		string text = _consumeStringPathBox.Text.Trim();
		if (!File.Exists(text))
		{
			return (Name: null, Desc: null);
		}
		if (text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			WzImage wzImage = LoadImgWithDetectedIv(text, out byte[] _, out string _);
			try
			{
				string cardId2 = ToConsumeCardId(cardId);
				WzSubProperty wzSubProperty = FindCardEntry(wzImage, cardId2);
				if (wzSubProperty != null)
				{
					return (Name: (wzSubProperty["name"] as WzStringProperty)?.Value, Desc: (wzSubProperty["desc"] as WzStringProperty)?.Value);
				}
				WzSubProperty wzSubProperty2 = FindConsumeGroup(wzImage);
				if (wzSubProperty2 != null)
				{
					WzSubProperty wzSubProperty3 = FindCardEntry(wzSubProperty2, cardId2);
					if (wzSubProperty3 != null)
					{
						return (Name: (wzSubProperty3["name"] as WzStringProperty)?.Value, Desc: (wzSubProperty3["desc"] as WzStringProperty)?.Value);
					}
				}
			}
			finally
			{
				wzImage.Dispose();
			}
			return (Name: null, Desc: null);
		}
		XDocument xDocument = XDocument.Load(text);
		XElement root = xDocument.Root;
		string cardId3 = ToConsumeCardId(cardId);
		XElement xElement = ((root == null) ? null : FindCardEntry(root, cardId3));
		if (xElement == null)
		{
			XElement xElement2 = ((root == null) ? null : FindConsumeGroup(root));
			xElement = ((xElement2 == null) ? null : FindCardEntry(xElement2, cardId3));
		}
		return (Name: (string?)xElement?.Elements("string").FirstOrDefault((XElement e) => (string?)e.Attribute("name") == "name")?.Attribute("value"), Desc: (string?)xElement?.Elements("string").FirstOrDefault((XElement e) => (string?)e.Attribute("name") == "desc")?.Attribute("value"));
	}

	private void UpsertConsumeText(string cardId, string name, string desc)
	{
		if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(desc))
		{
			return;
		}
		if (_stringConsumeImage != null)
		{
			string archiveCardId = ToConsumeCardId(cardId);
			WzSubProperty? archiveEntry = FindCardEntry(_stringConsumeImage, archiveCardId);
			if (archiveEntry == null)
			{
				archiveEntry = new WzSubProperty(archiveCardId);
				_stringConsumeImage.AddProperty(archiveEntry);
			}
			ApplyTextOnEntry(archiveEntry, name, desc);
			_stringConsumeImage.Changed = true;
			_stringArchive?.MarkDirty();
			return;
		}
		string text = _consumeStringPathBox.Text.Trim();
		if (!File.Exists(text))
		{
			return;
		}
		string text2 = ToConsumeCardId(cardId);
		if (text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
		{
			WzImage wzImage = LoadImgWithDetectedIv(text, out byte[] iv, out string _);
			try
			{
				WzSubProperty wzSubProperty = FindCardEntry(wzImage, text2);
				if (wzSubProperty != null)
				{
					ApplyTextOnEntry(wzSubProperty, name, desc);
					wzImage.Changed = true;
					WzImgSerializer wzImgSerializer = new WzImgSerializer(iv);
					wzImgSerializer.SerializeImage(wzImage, text);
				}
				else
				{
					WzSubProperty wzSubProperty2 = new WzSubProperty(text2);
					wzImage.AddProperty(wzSubProperty2);
					ApplyTextOnEntry(wzSubProperty2, name, desc);
					wzImage.Changed = true;
					WzImgSerializer wzImgSerializer2 = new WzImgSerializer(iv);
					wzImgSerializer2.SerializeImage(wzImage, text);
				}
				return;
			}
			finally
			{
				wzImage.Dispose();
			}
		}
		XDocument xDocument = XDocument.Load(text);
		XElement xElement = xDocument.Root ?? throw new InvalidOperationException("Invalid Consume XML");
		XElement xElement2 = FindCardEntry(xElement, text2) ?? new XElement("subprop", new XAttribute("name", text2));
		if (xElement2.Parent == null)
		{
			xElement.Add(xElement2);
		}
		if (!string.IsNullOrWhiteSpace(name))
		{
			XElement xElement3 = xElement2.Elements("string").FirstOrDefault((XElement e) => (string?)e.Attribute("name") == "name") ?? new XElement("string", new XAttribute("name", "name"), new XAttribute("value", name));
			if (xElement3.Parent == null)
			{
				xElement2.Add(xElement3);
			}
			xElement3.SetAttributeValue("value", name);
		}
		if (!string.IsNullOrWhiteSpace(desc))
		{
			XElement xElement4 = xElement2.Elements("string").FirstOrDefault((XElement e) => (string?)e.Attribute("name") == "desc") ?? new XElement("string", new XAttribute("name", "desc"), new XAttribute("value", desc));
			if (xElement4.Parent == null)
			{
				xElement2.Add(xElement4);
			}
			xElement4.SetAttributeValue("value", desc);
		}
		xDocument.Save(text);
	}

	private static void ApplyTextOnEntry(WzSubProperty entry, string name, string desc)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			if (entry["name"] is WzStringProperty wzStringProperty)
			{
				wzStringProperty.Value = name;
			}
			else
			{
				entry.RemoveProperty("name");
				entry.AddProperty(new WzStringProperty("name", name));
			}
		}
		if (!string.IsNullOrWhiteSpace(desc))
		{
			if (entry["desc"] is WzStringProperty wzStringProperty2)
			{
				wzStringProperty2.Value = desc;
				return;
			}
			entry.RemoveProperty("desc");
			entry.AddProperty(new WzStringProperty("desc", desc));
		}
	}

	private static string ToCanonicalCardId(string cardId)
	{
		if (!int.TryParse(cardId, out var result))
		{
			return cardId;
		}
		return result.ToString("D8", CultureInfo.InvariantCulture);
	}

	private static IEnumerable<string> CardIdCandidates(string cardId)
	{
		string canonical = ToCanonicalCardId(cardId);
		yield return canonical;
		string trimmed = canonical.TrimStart('0');
		if (!string.IsNullOrEmpty(trimmed))
		{
			yield return trimmed;
		}
		if (!string.Equals(cardId, canonical, StringComparison.Ordinal))
		{
			yield return cardId;
		}
	}

	private static string ToConsumeCardId(string cardId)
	{
		if (!int.TryParse(cardId, out var result))
		{
			return cardId.TrimStart('0');
		}
		return result.ToString(CultureInfo.InvariantCulture);
	}

	private static IEnumerable<string> ConsumeGroupCandidates()
	{
		yield return "0238";
		yield return "238";
	}

	private static WzSubProperty? FindConsumeGroup(IPropertyContainer container)
	{
		foreach (string item in ConsumeGroupCandidates())
		{
			if (container[item] is WzSubProperty result)
			{
				return result;
			}
		}
		return null;
	}

	private static XElement? FindConsumeGroup(XElement root)
	{
		HashSet<string> keys = ConsumeGroupCandidates().ToHashSet(StringComparer.Ordinal);
		return root.Elements("subprop").FirstOrDefault((XElement e) =>
		{
			string text = (string?)e.Attribute("name");
			return text != null && keys.Contains(text);
		});
	}

	private static WzSubProperty? FindCardEntry(IPropertyContainer container, string cardId)
	{
		foreach (string item in CardIdCandidates(cardId))
		{
			if (container[item] is WzSubProperty result)
			{
				return result;
			}
		}
		return null;
	}

	private static XElement? FindCardEntry(XElement container, string cardId)
	{
		HashSet<string> keys = CardIdCandidates(cardId).ToHashSet(StringComparer.Ordinal);
		return container.Elements("subprop").FirstOrDefault((XElement e) =>
		{
			string text = (string?)e.Attribute("name");
			return text != null && keys.Contains(text);
		});
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
			_splitContainer.Panel1MinSize = Math.Max(360, (int)(width * 0.26));
			_splitContainer.Panel2MinSize = Math.Max(700, (int)(width * 0.52));
			int num = Math.Max(_splitContainer.Panel1MinSize, (int)(width * 0.34));
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
		bool itemDirty = _itemArchive?.IsDirty == true;
		bool stringDirty = _stringArchive?.IsDirty == true;
		if (itemDirty || stringDirty)
		{
			DialogResult result = MessageBox.Show(this, "Save staged WZ changes before closing? Choose No to discard them.", "Unsaved WZ changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.Cancel)
			{
				e.Cancel = true;
				return;
			}
			if (result == DialogResult.Yes)
			{
				SaveWzArchives(askConfirmation: false);
				if ((_itemArchive?.IsDirty == true) || (_stringArchive?.IsDirty == true))
				{
					e.Cancel = true;
					return;
				}
			}
		}
		ClearLoaded();
		base.OnFormClosing(e);
	}
}







