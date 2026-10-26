using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MapleEditor.UI;
using MapleLib.Configuration;
using MapleLib.WzLib;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleMobEditor;

internal sealed class MainForm : Form
{
	private const int PreviewCardSize = 128;

	private const int HeaderSectionHeight = 168;

	private const int SectionGap = 12;

	private static readonly Color AppBackColor = UiTheme.AppBackground;

	private static readonly Color SurfaceColor = UiTheme.Surface;

	private static readonly Color SurfaceRaisedColor = UiTheme.SurfaceRaised;

	private static readonly Color AccentColor = UiTheme.Accent;

	private static readonly Color AccentMutedColor = Color.FromArgb(66, UiTheme.Accent);

	private static readonly Color BorderColor = UiTheme.Border;

	private static readonly Color TextColor = UiTheme.Text;

	private static readonly Color SubtleTextColor = UiTheme.TextSubtle;

	private readonly AssetGridView _assetGrid;

	private readonly BrandHeader _header;

	private readonly Label _previewSizeLabel;

	private readonly SplitContainer _splitContainer;

	private readonly TextBox _searchTextBox;

	private readonly PictureBox _iconPreview;

	private readonly Label _itemNameLabel;

	private readonly Label _itemContextLabel;

	private readonly Label _itemPathLabel;

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

	private readonly NumericUpDown _drInput;

	private readonly CheckBox _applyDrCheckBox;

	private readonly CheckBox _physicalAndMagicCounterCheckBox;

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

	private WzArchiveSession? _archiveSession;

	private WzStringLookup? _stringLookup;

	private ToolStripMenuItem? _saveArchiveMenuItem;

	private ToolStripMenuItem? _v83VersionMenuItem;

	private ToolStripMenuItem? _v95VersionMenuItem;

	public MainForm()
	{
		Text = "Maple Mob Editor - " + MapleEditorVersionSettings.Profile.DisplayName;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.Manual;
		BackColor = AppBackColor;
		MinimumSize = new Size(1480, 900);
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		Bounds = GetStartupBounds();
		MenuStrip value = (MainMenuStrip = BuildMenu(out _cashOnlyMenuItem, out _copyIdsMenuItem));
		UiTheme.StyleMenu(value);
		_header = new BrandHeader
		{
			Dock = DockStyle.Top,
			Height = 94,
			Compact = true,
			Title = "Maple Mob Editor",
			Subtitle = "Mob stats, icons and String.wz names for v83 / v95",
			VersionText = MapleEditorVersionSettings.Profile.DisplayName,
			RightText = _currentFolderPath ?? "No archive loaded"
		};
		StatusStrip statusStrip = new StatusStrip();
		_statusLabel = new ToolStripStatusLabel("Open Mob.wz or a folder containing .img/.img.xml files");
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
		Label value2 = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Mob Browser",
			Font = new Font("Bahnschrift SemiBold", 16f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor,
			TextAlign = ContentAlignment.MiddleLeft
		};
		Label value3 = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Sorted by mob ID. Open a folder or a Mob.wz archive.",
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
			PlaceholderText = "Search mobs by id, category, or name"
		};
		_searchTextBox.TextChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
		};
		TableLayoutPanel browserLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			BackColor = SurfaceColor,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		browserLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		browserLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		browserLayout.Controls.Add(value2, 0, 0);
		browserLayout.Controls.Add(value3, 0, 1);
		browserLayout.Controls.Add(_searchTextBox, 0, 2);
		browserLayout.Controls.Add(_assetGrid, 0, 3);
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
			RowCount = 2,
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
			ColumnCount = 2,
			RowCount = 1,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		panel3.Controls.Add(tableLayoutPanel2);
		Panel panel4 = CreatePreviewCard("Mob Preview");
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
			Text = "48 px",
			TextAlign = ContentAlignment.MiddleRight,
			ForeColor = UiTheme.TextMuted,
			BackColor = SurfaceRaisedColor,
			Padding = new Padding(0, 0, 8, 2),
			Font = UiTheme.Font(8f)
		};
		panel5.Controls.Add(_previewSizeLabel);
		tableLayoutPanel2.Controls.Add(panel4, 0, 0);
		Panel panel6 = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(14, 2, 0, 0),
			Margin = Padding.Empty
		};
		tableLayoutPanel2.Controls.Add(panel6, 1, 0);
		_itemNameLabel = new Label
		{
			Left = 0,
			Top = 0,
			AutoSize = true,
			Font = new Font("Bahnschrift SemiBold", 20f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor,
			Text = "No mob selected"
		};
		panel6.Controls.Add(_itemNameLabel);
		_itemContextLabel = new Label
		{
			Left = 2,
			Top = 42,
			AutoSize = true,
			ForeColor = SubtleTextColor,
			Text = "Detected context"
		};
		panel6.Controls.Add(_itemContextLabel);
		_itemPathLabel = new Label
		{
			Left = 2,
			Top = 70,
			Width = 470,
			Height = 72,
			ForeColor = SubtleTextColor,
			Text = string.Empty
		};
		panel6.Controls.Add(_itemPathLabel);
		Panel panel7 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 600,
			BackColor = SurfaceRaisedColor,
			Margin = Padding.Empty
		};
		tableLayoutPanel.Controls.Add(panel7, 0, 1);
		Label value4 = new Label
		{
			Left = 16,
			Top = 16,
			AutoSize = true,
			Text = "Focused Stat Editor",
			Font = new Font("Bahnschrift SemiBold", 14f, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = TextColor
		};
		panel7.Controls.Add(value4);
		Label value5 = new Label
		{
			Left = 16,
			Top = 44,
			Width = 760,
			Text = "Edits mob stats in info and supports mass editing across selected mobs.",
			ForeColor = SubtleTextColor
		};
		panel7.Controls.Add(value5);
		int num = 84;
		_strInput = AddStatInput(panel7, "Level", 16, num);
		_dexInput = AddStatInput(panel7, "Max HP", 200, num);
		_intInput = AddStatInput(panel7, "Max MP", 384, num);
		_lukInput = AddStatInput(panel7, "Physical Attack", 568, num);
		_padInput = AddStatInput(panel7, "Physical Defense", 16, num + 88);
		_madInput = AddStatInput(panel7, "Magic Attack", 200, num + 88);
		_speedInput = AddStatInput(panel7, "Speed", 384, num + 88);
		_jumpInput = AddStatInput(panel7, "Magic Defense", 568, num + 88);
		_hpInput = AddStatInput(panel7, "Accuracy", 16, num + 176);
		_mpInput = AddStatInput(panel7, "Avoidability", 200, num + 176);
		_accInput = AddStatInput(panel7, "EXP", 384, num + 176);
		_avoidInput = AddStatInput(panel7, "Push Resist", 568, num + 176);
		_slotsInput = AddStatInput(panel7, "Summon Type", 16, num + 264);
		_reqLevelInput = AddStatInput(panel7, "Rare Drop Lv", 200, num + 264);
		_drInput = AddStatInput(panel7, "Damage Reflect (PA+MA)", 384, num + 264);
		_applyDrCheckBox = new CheckBox
		{
			Left = 568,
			Top = num + 292,
			Width = 190,
			Height = 28,
			Text = "Apply DR to PA/MA",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel7.Controls.Add(_applyDrCheckBox);
		_cashCheckBox = new CheckBox
		{
			Left = 16,
			Top = num + 336,
			Width = 190,
			Height = 28,
			Text = "Boss (0 / 1)",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel7.Controls.Add(_cashCheckBox);
		_tradeLockCheckBox = new CheckBox
		{
			Left = 200,
			Top = num + 336,
			Width = 190,
			Height = 28,
			Text = "First Attack (0 / 1)",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel7.Controls.Add(_tradeLockCheckBox);
		_physicalAndMagicCounterCheckBox = new CheckBox
		{
			Left = 384,
			Top = num + 336,
			Width = 350,
			Height = 28,
			Text = "Physical+Magic Counter (skill 145)",
			ForeColor = TextColor,
			BackColor = SurfaceRaisedColor
		};
		panel7.Controls.Add(_physicalAndMagicCounterCheckBox);
		_applyButton = new RoundedButton
		{
			Left = 16,
			Top = num + 370,
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
		panel7.Controls.Add(_applyButton);
		_saveAsImgButton = new RoundedButton
		{
			Left = 16,
			Top = num + 416,
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
		panel7.Controls.Add(_saveAsImgButton);
		_saveAsXmlButton = new RoundedButton
		{
			Left = 172,
			Top = num + 416,
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
		panel7.Controls.Add(_saveAsXmlButton);
		_saveCurrentButton = new RoundedButton
		{
			Left = 172,
			Top = num + 370,
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
		panel7.Controls.Add(_saveCurrentButton);
		_saveAllButton = new RoundedButton
		{
			Left = 328,
			Top = num + 370,
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
		panel7.Controls.Add(_saveAllButton);
		Label value6 = new Label
		{
			Left = 16,
			Top = num + 464,
			Width = 760,
			Height = 40,
			ForeColor = SubtleTextColor,
			Text = "Exported XML now defaults to the correct Maple naming format: 5220000.img.xml"
		};
		panel7.Controls.Add(value6);
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
		toolStripMenuItem.DropDownItems.Add("Open Mob.wz Archive...", null, (object? _, EventArgs _) =>
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
		_v83VersionMenuItem = new ToolStripMenuItem("v83 (native)")
		{
			CheckOnClick = false
		};
		_v83VersionMenuItem.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V83);
		};
		_v95VersionMenuItem = new ToolStripMenuItem("v95")
		{
			CheckOnClick = false
		};
		_v95VersionMenuItem.Click += (object? _, EventArgs _) =>
		{
			SetVersion(MapleEditorGameVersion.V95);
		};
		versionMenu.DropDownItems.Add(_v83VersionMenuItem);
		versionMenu.DropDownItems.Add(_v95VersionMenuItem);
		menuStrip.Items.Add(versionMenu);
		cashOnlyMenuItem = new ToolStripMenuItem("Boss Only")
		{
			CheckOnClick = true
		};
		cashOnlyMenuItem.CheckedChanged += (object? _, EventArgs _) =>
		{
			RefreshItemBrowser();
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
		Text = "Maple Mob Editor - " + MapleEditorVersionSettings.Profile.DisplayName;
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
			Title = "Open Mob.wz (v95 archives supported)",
			Filter = "WZ archives (*.wz)|*.wz|All files (*.*)|*.*",
			FileName = "Mob.wz",
			InitialDirectory = ((MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95) ? MapleEditorVersionSettings.GetDefaultWzFolder() : MapleEditorVersionSettings.GetDefaultOpenFolder())
		};
		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			LoadMobArchive(dialog.FileName);
		}
	}

	private void LoadMobArchive(string path)
	{
		ClearLoadedItems();
		SplashForm splash = new SplashForm("Opening " + Path.GetFileName(path), "Reading WZ directory and preparing previews");
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
			MapleEditorVersionSettings.Settings.LastMobWz = path;
			if (MapleEditorVersionSettings.Current == MapleEditorGameVersion.V95)
			{
				MapleEditorVersionSettings.Settings.V95WzFolder = Path.GetDirectoryName(path) ?? MapleEditorVersionSettings.Settings.V95WzFolder;
			}
			MapleEditorVersionSettings.Save();
			List<WzImage> source = session.File.WzDirectory.WzImages.ToList();
			foreach (WzDirectory directory in session.File.WzDirectory.WzDirectories)
			{
				if (!string.Equals(directory.Name, "ExcelExport", StringComparison.OrdinalIgnoreCase))
				{
					source.AddRange(directory.WzImages);
				}
			}
			_loadingSelection = true;
			int count = 0;
			splash.Report("Loading mob images...", 0.0);
			foreach (WzImage image in source.OrderBy((WzImage i) => i.Name))
			{
				foreach (ImgItemEntry entry in TryLoadArchiveMobItems(path, session, image))
				{
					_items.Add(entry);
					count++;
					if (count % 20 == 0)
					{
						splash.Report($"Loading mob images {count}/{source.Count}...", (double)count / Math.Max(1, source.Count));
					}
				}
			}
			_currentFolderPath = path;
			_saveAllButton.Enabled = count > 0;
			if (count == 0)
			{
				throw new InvalidOperationException("No readable mob images with an info section were found in that archive.");
			}
			RefreshItemBrowser();
			_header.RightText = Path.GetFileName(path) + " · v" + session.GameVersion;
			splash.Report("Ready.", 1.0);
			SetStatus($"Loaded {count} mobs from {Path.GetFileName(path)} (v{session.GameVersion})");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "WZ load failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			CloseArchiveSession();
		}
		finally
		{
			splash.Close();
			splash.Dispose();
			_loadingSelection = false;
			Cursor = Cursors.Default;
			LoadSelectedItem();
		}
	}

	private IEnumerable<ImgItemEntry> TryLoadArchiveMobItems(string archivePath, WzArchiveSession session, WzImage image)
	{
		try
		{
			image.ParseImage();
			if (!(image["info"] is WzSubProperty info))
			{
				return Array.Empty<ImgItemEntry>();
			}
			string syntheticPath = Path.Combine(archivePath, image.Name);
			GameFileInfo detection = GameFileDetection.Detect(syntheticPath);
			string itemId = string.IsNullOrEmpty(detection.ItemId) ? Path.GetFileNameWithoutExtension(image.Name) : detection.ItemId;
			string displayName = GetDisplayName(info, detection);
			Bitmap thumbnail = CreateThumbnail(image);
			byte[] iv = WzTool.GetIvByMapleVersion(WzMapleVersion.GMS);
			return new ImgItemEntry[1]
			{
				new ImgItemEntry(syntheticPath, image, info, iv, $"WZ v{session.GameVersion}", detection, itemId, displayName, thumbnail, delegate(ImgItemEntry entry)
				{
					entry.Image.Changed = true;
					session.MarkDirty();
				}, null, null, ownsImage: false)
			};
		}
		catch
		{
			return Array.Empty<ImgItemEntry>();
		}
	}

	private void SaveWzArchive(bool askConfirmation)
	{
		if (_archiveSession == null || !_archiveSession.IsDirty)
		{
			SetStatus("No staged WZ changes to save.");
			return;
		}
		if (askConfirmation)
		{
			DialogResult result = MessageBox.Show(this, $"Write changes to {_archiveSession.FileName}?\n\nThis repacks the entire archive and can take a while for large WZ files. A .bak backup will be created next to the archive.", "Save WZ archive", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (result != DialogResult.Yes)
			{
				return;
			}
		}
		Cursor = Cursors.WaitCursor;
		SplashForm splash = new SplashForm("Saving " + _archiveSession.FileName, "Repacking the WZ archive. This can take a few minutes.");
		splash.Show(this);
		splash.Report("Repacking " + _archiveSession.FileName + "...", -1.0);
		try
		{
			SetStatus("Repacking " + _archiveSession.FileName + "...");
			_archiveSession.Save();
			foreach (ImgItemEntry item in _items)
			{
				item.IsDirty = false;
			}
			splash.Report("Saved.", 1.0);
			SetStatus("Saved " + _archiveSession.FileName);
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
		return new RoundedPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(14),
			BackColor = SurfaceColor,
			CornerRadius = 16
		};
	}

	private static Panel CreatePreviewCard(string title)
	{
		Panel panel = new RoundedPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = new Padding(10, 6, 10, 10),
			BackColor = SurfaceRaisedColor,
			CornerRadius = 12
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
			Minimum = -2147483648m,
			Maximum = 2147483647m,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = SurfaceColor,
			ForeColor = TextColor
		};
		parent.Controls.Add(numericUpDown);
		return numericUpDown;
	}

	private static decimal ClampToInputRange(NumericUpDown input, int value)
	{
		decimal num = value;
		if (num < input.Minimum)
		{
			return input.Minimum;
		}
		if (num > input.Maximum)
		{
			return input.Maximum;
		}
		return num;
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
		return new Rectangle(workingArea.Left, workingArea.Top, Math.Max(workingArea.Width, 1480), Math.Max(workingArea.Height, 900));
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
				throw new InvalidOperationException("No readable mob .img or .img.xml files with an info section were found in that folder.");
			}
			RefreshItemBrowser();
			_header.RightText = folderPath;
			SetStatus($"Loaded {num} mob files from {folderPath}");
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
		Bitmap thumbnail = CreateThumbnail(wzImage);
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
				Bitmap thumbnail = CreateThumbnail(wzSubProperty);
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
			Bitmap thumbnail = CreateThumbnail(rootImage);
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
				Bitmap thumbnail2 = CreateThumbnail(wzSubProperty);
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
			string? stringName = _stringLookup.GetMobName(detection.ItemId);
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

	internal static Bitmap CreateThumbnail(IPropertyContainer container)
	{
		foreach (WzCanvasProperty item in EnumeratePreviewCanvases(container))
		{
			try
			{
				using Bitmap source = item.PngProperty.GetImage(saveInMemory: false);
				using Bitmap original = CropTransparentBounds(source);
				return new Bitmap(original, new Size(48, 48));
			}
			catch
			{
			}
		}
		return CreateEmptyThumbnail();
	}

	private static Bitmap CreateEmptyThumbnail()
	{
		Bitmap bitmap = new Bitmap(48, 48);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.Clear(Color.Transparent);
		return bitmap;
	}

	private static Bitmap CropTransparentBounds(Bitmap source)
	{
		int num = source.Width;
		int num2 = source.Height;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < source.Height; i++)
		{
			for (int j = 0; j < source.Width; j++)
			{
				if (source.GetPixel(j, i).A != 0)
				{
					num = Math.Min(num, j);
					num2 = Math.Min(num2, i);
					num3 = Math.Max(num3, j);
					num4 = Math.Max(num4, i);
				}
			}
		}
		if (num3 < num || num4 < num2)
		{
			return (Bitmap)source.Clone();
		}
		Rectangle rect = Rectangle.FromLTRB(num, num2, num3 + 1, num4 + 1);
		return source.Clone(rect, source.PixelFormat);
	}

	private static IEnumerable<WzCanvasProperty> EnumeratePreviewCanvases(IPropertyContainer container)
	{
		string[] preferredActions = new string[9] { "stand", "move", "fly", "hit", "attack", "die", "regen", "jump", "skill" };
		HashSet<WzCanvasProperty> yielded = new HashSet<WzCanvasProperty>();
		string[] array = preferredActions;
		foreach (string action in array)
		{
			WzImageProperty wzImageProperty = container[action];
			if (!(wzImageProperty is IPropertyContainer actionContainer))
			{
				continue;
			}
			foreach (WzCanvasProperty canvas in EnumerateCanvases(actionContainer))
			{
				if (yielded.Add(canvas))
				{
					yield return canvas;
				}
			}
		}
		foreach (WzCanvasProperty canvas2 in EnumerateCanvases(container))
		{
			if (yielded.Add(canvas2))
			{
				yield return canvas2;
			}
		}
	}

	private static IEnumerable<WzCanvasProperty> EnumerateCanvases(IPropertyContainer container)
	{
		foreach (WzImageProperty property in container.WzProperties)
		{
			WzImageProperty wzImageProperty = property;
			if (!(wzImageProperty is WzCanvasProperty canvas))
			{
				if (!(wzImageProperty is WzUOLProperty { LinkValue: var linkValue }) || !(linkValue is WzCanvasProperty linkedCanvas))
				{
					if (!(wzImageProperty is IPropertyContainer childContainer))
					{
						continue;
					}
					foreach (WzCanvasProperty item in EnumerateCanvases(childContainer))
					{
						yield return item;
					}
				}
				else
				{
					yield return linkedCanvas;
				}
			}
			else
			{
				yield return canvas;
			}
		}
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
		_copyIdsMenuItem.Enabled = enabled;
		if (imgItemEntry == null)
		{
			_itemNameLabel.Text = "No mob selected";
			_itemContextLabel.Text = ((_currentFolderPath == null) ? "Detected context" : _currentFolderPath);
			_itemPathLabel.Text = string.Empty;
			SetPreview(null);
			SetStatInputs(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, applyDamageReflect: false, hasPhysicalAndMagicCounter: false, isCash: false, isTradeLocked: false);
			return;
		}
		_itemNameLabel.Text = ((entries.Count == 1) ? imgItemEntry.DisplayName : $"{entries.Count} mobs selected");
		_itemContextLabel.Text = ((entries.Count == 1) ? $"{imgItemEntry.Detection.DisplayContext} | Mob ID {imgItemEntry.ItemId} | {imgItemEntry.IvLabel}" : "Mass edit mode | changes apply to every selected mob");
		_itemPathLabel.Text = ((entries.Count == 1) ? imgItemEntry.FilePath : (string.Join(Environment.NewLine, from i in entries.Take(3)
			select i.FilePath) + ((entries.Count > 3) ? (Environment.NewLine + "...") : string.Empty)));
		SetPreview(imgItemEntry.Thumbnail);
		int infoInt = GetInfoInt(imgItemEntry.Info, "PADamage");
		int infoInt2 = GetInfoInt(imgItemEntry.Info, "MADamage");
		int damageReflect = ((infoInt == infoInt2) ? infoInt : 0);
		bool applyDamageReflect = infoInt == infoInt2 && infoInt != 0;
		SetStatInputs(GetInfoInt(imgItemEntry.Info, "level"), GetInfoInt(imgItemEntry.Info, "maxHP"), GetInfoInt(imgItemEntry.Info, "maxMP"), infoInt, GetInfoInt(imgItemEntry.Info, "PDDamage"), infoInt2, GetInfoInt(imgItemEntry.Info, "speed"), GetInfoInt(imgItemEntry.Info, "MDDamage"), GetInfoInt(imgItemEntry.Info, "acc"), GetInfoInt(imgItemEntry.Info, "eva"), GetInfoInt(imgItemEntry.Info, "exp"), GetInfoInt(imgItemEntry.Info, "pushed"), GetInfoInt(imgItemEntry.Info, "summonType"), GetInfoInt(imgItemEntry.Info, "rareItemDropLevel"), damageReflect, applyDamageReflect, HasPhysicalAndMagicCounterSkill(imgItemEntry.Info), GetInfoInt(imgItemEntry.Info, "boss") != 0, GetInfoInt(imgItemEntry.Info, "firstAttack") != 0);
	}

	private static int GetInfoInt(WzSubProperty info, string name)
	{
		return (info[name] as WzIntProperty)?.Value ?? 0;
	}

	private void SetStatInputs(int str, int dex, int intel, int luk, int pad, int mad, int speed, int jump, int hp, int mp, int acc, int avoid, int slots, int reqLevel, int damageReflect, bool applyDamageReflect, bool hasPhysicalAndMagicCounter, bool isCash, bool isTradeLocked)
	{
		_strInput.Value = ClampToInputRange(_strInput, str);
		_dexInput.Value = ClampToInputRange(_dexInput, dex);
		_intInput.Value = ClampToInputRange(_intInput, intel);
		_lukInput.Value = ClampToInputRange(_lukInput, luk);
		_padInput.Value = ClampToInputRange(_padInput, pad);
		_madInput.Value = ClampToInputRange(_madInput, mad);
		_speedInput.Value = ClampToInputRange(_speedInput, speed);
		_jumpInput.Value = ClampToInputRange(_jumpInput, jump);
		_hpInput.Value = ClampToInputRange(_hpInput, hp);
		_mpInput.Value = ClampToInputRange(_mpInput, mp);
		_accInput.Value = ClampToInputRange(_accInput, acc);
		_avoidInput.Value = ClampToInputRange(_avoidInput, avoid);
		_slotsInput.Value = ClampToInputRange(_slotsInput, slots);
		_reqLevelInput.Value = ClampToInputRange(_reqLevelInput, reqLevel);
		_drInput.Value = ClampToInputRange(_drInput, damageReflect);
		_applyDrCheckBox.Checked = applyDamageReflect;
		_physicalAndMagicCounterCheckBox.Checked = hasPhysicalAndMagicCounter;
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
			int value = decimal.ToInt32(_lukInput.Value);
			int value2 = decimal.ToInt32(_madInput.Value);
			if (_applyDrCheckBox.Checked)
			{
				int num = decimal.ToInt32(_drInput.Value);
				value = num;
				value2 = num;
			}
			SetOrCreateInfoInt(item.Info, "level", decimal.ToInt32(_strInput.Value));
			SetOrCreateInfoInt(item.Info, "maxHP", decimal.ToInt32(_dexInput.Value));
			SetOrCreateInfoInt(item.Info, "maxMP", decimal.ToInt32(_intInput.Value));
			SetOrCreateInfoInt(item.Info, "PADamage", value);
			SetOrCreateInfoInt(item.Info, "PDDamage", decimal.ToInt32(_padInput.Value));
			SetOrCreateInfoInt(item.Info, "MADamage", value2);
			SetOrCreateInfoInt(item.Info, "speed", decimal.ToInt32(_speedInput.Value));
			SetOrCreateInfoInt(item.Info, "MDDamage", decimal.ToInt32(_jumpInput.Value));
			SetOrCreateInfoInt(item.Info, "acc", decimal.ToInt32(_hpInput.Value));
			SetOrCreateInfoInt(item.Info, "eva", decimal.ToInt32(_mpInput.Value));
			SetOrCreateInfoInt(item.Info, "exp", decimal.ToInt32(_accInput.Value));
			SetOrCreateInfoInt(item.Info, "pushed", decimal.ToInt32(_avoidInput.Value));
			SetOrCreateInfoInt(item.Info, "summonType", decimal.ToInt32(_slotsInput.Value));
			SetOrCreateInfoInt(item.Info, "rareItemDropLevel", decimal.ToInt32(_reqLevelInput.Value));
			SetOrCreateInfoInt(item.Info, "boss", _cashCheckBox.Checked ? 1 : 0);
			SetOrCreateInfoInt(item.Info, "firstAttack", _tradeLockCheckBox.Checked ? 1 : 0);
			SetPhysicalAndMagicCounterSkill(item.Info, _physicalAndMagicCounterCheckBox.Checked);
			item.Image.Changed = true;
			item.IsDirty = true;
		}
		SetStatus((selectedEntries.Count == 1) ? ("Updated mob stats for " + selectedEntries[0].DisplayName) : $"Updated {selectedEntries.Count} selected mobs");
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

	private static bool HasPhysicalAndMagicCounterSkill(WzSubProperty info)
	{
		if (!(info["skill"] is WzSubProperty wzSubProperty))
		{
			return false;
		}
		foreach (WzImageProperty wzProperty in wzSubProperty.WzProperties)
		{
			if (wzProperty is WzSubProperty wzSubProperty2)
			{
				WzIntProperty obj = wzSubProperty2["skill"] as WzIntProperty;
				if (obj != null && obj.Value == 145)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void SetPhysicalAndMagicCounterSkill(WzSubProperty info, bool enabled)
	{
		WzSubProperty wzSubProperty = info["skill"] as WzSubProperty;
		if (enabled)
		{
			if (wzSubProperty == null)
			{
				wzSubProperty = new WzSubProperty("skill");
				info.AddProperty(wzSubProperty);
			}
			if (HasPhysicalAndMagicCounterSkill(info))
			{
				return;
			}
			int val = 0;
			foreach (WzImageProperty wzProperty in wzSubProperty.WzProperties)
			{
				if (int.TryParse(wzProperty.Name, out var result))
				{
					val = Math.Max(val, result + 1);
				}
			}
			WzSubProperty wzSubProperty2 = new WzSubProperty(val.ToString());
			wzSubProperty2.AddProperty(new WzIntProperty("skill", 145));
			wzSubProperty2.AddProperty(new WzIntProperty("action", 1));
			wzSubProperty2.AddProperty(new WzIntProperty("level", 1));
			wzSubProperty2.AddProperty(new WzIntProperty("effectAfter", 0));
			wzSubProperty.AddProperty(wzSubProperty2);
			wzSubProperty.SortProperties();
		}
		else
		{
			if (wzSubProperty == null)
			{
				return;
			}
			List<WzImageProperty> list = new List<WzImageProperty>();
			foreach (WzImageProperty wzProperty2 in wzSubProperty.WzProperties)
			{
				if (wzProperty2 is WzSubProperty wzSubProperty3)
				{
					WzIntProperty obj = wzSubProperty3["skill"] as WzIntProperty;
					if (obj != null && obj.Value == 145)
					{
						list.Add(wzProperty2);
					}
				}
			}
			foreach (WzImageProperty item in list)
			{
				wzSubProperty.RemoveProperty(item);
			}
			if (wzSubProperty.WzProperties.Count == 0)
			{
				info.RemoveProperty("skill");
			}
		}
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
				SetStatus((selectedEntries.Count == 1) ? ("Staged " + selectedEntries[0].DisplayName + " for WZ save") : $"Staged {selectedEntries.Count} selected mobs for WZ save");
				SaveWzArchive(askConfirmation: true);
			}
			else
			{
				SetStatus((selectedEntries.Count == 1) ? ("Saved " + selectedEntries[0].DisplayName) : $"Saved {selectedEntries.Count} selected mobs");
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
				SetStatus("Staged all modified mobs for WZ save");
				SaveWzArchive(askConfirmation: true);
			}
			else
			{
				SetStatus("Saved all modified mobs");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CopySelectedIds()
	{
		List<ImgItemEntry> selectedEntries = GetSelectedEntries();
		if (selectedEntries.Count != 0)
		{
			string text = string.Join(Environment.NewLine, selectedEntries.Select((ImgItemEntry entry) => NormalizeItemIdForCopy(entry.ItemId)).Distinct());
			Clipboard.SetText(text);
			SetStatus((selectedEntries.Count == 1) ? ("Copied ID " + text) : $"Copied {selectedEntries.Count} mob IDs");
		}
	}

	private static string NormalizeItemIdForCopy(string itemId)
	{
		return (!string.IsNullOrEmpty(itemId) && itemId[0] == '0') ? itemId.Substring(1) : itemId;
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
		if (_archiveSession != null && _archiveSession.IsDirty)
		{
			DialogResult result = MessageBox.Show(this, "Save staged changes to " + _archiveSession.FileName + " before continuing? Choose No to discard the staged changes.", "Unsaved WZ changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.Cancel)
			{
				return false;
			}
			if (result == DialogResult.Yes)
			{
				SaveWzArchive(askConfirmation: false);
				if (_archiveSession != null && _archiveSession.IsDirty)
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
		SetSelectionState(new List<ImgItemEntry>());
		_saveAllButton.Enabled = false;
	}

	private void RefreshItemBrowser()
	{
		_loadingSelection = true;
		try
		{
			string query = _searchTextBox.Text.Trim();
			IEnumerable<ImgItemEntry> enumerable = _items;
			if (!string.IsNullOrWhiteSpace(query))
			{
				enumerable = enumerable.Where((ImgItemEntry item) => item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || item.ItemId.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Detection.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
			}
			if (_cashOnlyMenuItem.Checked)
			{
				enumerable = enumerable.Where((ImgItemEntry item) => GetInfoInt(item.Info, "boss") != 0);
			}
			List<object> preserve = _assetGrid.SelectedRows.Select((AssetGridRow r) => r.Tag).ToList();
			List<AssetGridRow> rows = enumerable.Select((ImgItemEntry item) => new AssetGridRow
			{
				Tag = item,
				Id = item.ItemId,
				Name = item.DisplayName,
				Category = item.Detection.DisplayContext,
				Level = GetInfoInt(item.Info, "level").ToString(),
				Flags = BuildMobFlags(item),
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

	private static string BuildMobFlags(ImgItemEntry item)
	{
		List<string> list = new List<string>();
		if (GetInfoInt(item.Info, "boss") != 0)
		{
			list.Add("Boss");
		}
		if (GetInfoInt(item.Info, "firstAttack") != 0)
		{
			list.Add("First Atk");
		}
		if (HasPhysicalAndMagicCounterSkill(item.Info))
		{
			list.Add("Counter");
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
			_previewSizeLabel.Text = ((bitmap == null) ? "48 px" : $"{bitmap.Width} × {bitmap.Height} px");
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
			_splitContainer.Panel1MinSize = Math.Max(360, (int)(width * 0.28));
			_splitContainer.Panel2MinSize = Math.Max(680, (int)(width * 0.52));
			int num = Math.Max(_splitContainer.Panel1MinSize, (int)(width * 0.36));
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
		base.OnFormClosing(e);
	}
}












