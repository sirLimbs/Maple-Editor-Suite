using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MapleEditor.UI;

namespace MapleCodexCreator;

internal sealed class MassAddCardsForm : Form
{
	private static readonly Color AppBackColor = Color.FromArgb(18, 20, 26);

	private static readonly Color SurfaceColor = Color.FromArgb(29, 32, 40);

	private static readonly Color SurfaceRaisedColor = Color.FromArgb(37, 41, 51);

	private static readonly Color AccentColor = Color.FromArgb(82, 163, 255);

	private static readonly Color BorderColor = Color.FromArgb(57, 62, 74);

	private static readonly Color TextColor = Color.FromArgb(235, 239, 245);

	private static readonly Color SubtleTextColor = Color.FromArgb(154, 163, 178);

	private readonly DataGridView _grid;

	private readonly Label _summaryLabel;

	private readonly TextBox _cardIdBox;

	private readonly TextBox _mobIdBox;

	private readonly TextBox _nameBox;

	private readonly TextBox _descriptionBox;

	private readonly PictureBox _iconPreview;

	private readonly PictureBox _iconRawPreview;

	private readonly Button _addCardsButton;

	private bool _syncingEditor;

	public MassAddCardsForm()
	{
		Text = "Mass Add Cards";
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(1280, 700);
		Size = new Size(1400, 800);
		BackColor = AppBackColor;
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Padding = new Padding(10)
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		Controls.Add(tableLayoutPanel);
		_summaryLabel = new Label
		{
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = SubtleTextColor,
			Text = "0 rows"
		};
		tableLayoutPanel.Controls.Add(_summaryLabel, 0, 0);
		SplitContainer splitContainer = new SplitContainer
		{
			Dock = DockStyle.Fill,
			SplitterDistance = 900,
			BackColor = BorderColor
		};
		tableLayoutPanel.Controls.Add(splitContainer, 0, 1);
		_grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			AllowUserToAddRows = true,
			AllowUserToDeleteRows = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			MultiSelect = true,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			BackgroundColor = SurfaceColor,
			GridColor = BorderColor,
			BorderStyle = BorderStyle.FixedSingle,
			EnableHeadersVisualStyles = false,
			RowHeadersVisible = false,
			ForeColor = TextColor,
			RowTemplate = 
			{
				Height = 56
			}
		};
		_grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceRaisedColor;
		_grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
		_grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceRaisedColor;
		_grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;
		_grid.DefaultCellStyle.BackColor = SurfaceColor;
		_grid.DefaultCellStyle.ForeColor = TextColor;
		_grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(53, 70, 94);
		_grid.DefaultCellStyle.SelectionForeColor = TextColor;
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Status",
			Name = "Status",
			ReadOnly = true,
			FillWeight = 70f
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Card ID",
			Name = "CardId",
			FillWeight = 70f
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Mob ID",
			Name = "MobId",
			FillWeight = 70f
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Name",
			Name = "Name",
			FillWeight = 120f
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Description",
			Name = "Description",
			FillWeight = 160f
		});
		_grid.Columns.Add(new DataGridViewButtonColumn
		{
			HeaderText = "Icon",
			Name = "PickIcon",
			Text = "Add Icon...",
			UseColumnTextForButtonValue = true,
			FillWeight = 90f
		});
		_grid.Columns.Add(new DataGridViewImageColumn
		{
			HeaderText = "Icon Preview",
			Name = "IconPreview",
			ImageLayout = DataGridViewImageCellLayout.Zoom,
			FillWeight = 90f,
			ReadOnly = true
		});
		_grid.Columns.Add(new DataGridViewButtonColumn
		{
			HeaderText = "IconRaw",
			Name = "PickIconRaw",
			Text = "Add IconRaw...",
			UseColumnTextForButtonValue = true,
			FillWeight = 100f
		});
		_grid.Columns.Add(new DataGridViewImageColumn
		{
			HeaderText = "IconRaw Preview",
			Name = "IconRawPreview",
			ImageLayout = DataGridViewImageCellLayout.Zoom,
			FillWeight = 90f,
			ReadOnly = true
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "IconPath",
			Name = "IconPath",
			Visible = false
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "IconRawPath",
			Name = "IconRawPath",
			Visible = false
		});
		_grid.SelectionChanged += (object? _, EventArgs _) =>
		{
			LoadSelectedRowIntoEditor();
		};
		_grid.CellValueChanged += (object? _, DataGridViewCellEventArgs _) =>
		{
			OnGridDataChanged();
		};
		_grid.RowsRemoved += (object? _, DataGridViewRowsRemovedEventArgs _) =>
		{
			OnGridDataChanged();
		};
		_grid.UserAddedRow += (object? _, DataGridViewRowEventArgs _) =>
		{
			OnGridDataChanged();
		};
		_grid.CellContentClick += Grid_CellContentClick;
		splitContainer.Panel1.Controls.Add(_grid);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 7,
			Padding = new Padding(10),
			BackColor = SurfaceColor
		};
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		splitContainer.Panel2.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Text = "Row Editor",
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = TextColor,
			Font = new Font("Bahnschrift SemiBold", 13f, FontStyle.Bold, GraphicsUnit.Point)
		}, 0, 0);
		_cardIdBox = AddField(tableLayoutPanel2, "Card ID (blank = auto)", 1);
		_mobIdBox = AddField(tableLayoutPanel2, "Mob ID", 2);
		_nameBox = AddField(tableLayoutPanel2, "Name", 3);
		_descriptionBox = AddField(tableLayoutPanel2, "Description", 4, multiline: true);
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			BackColor = SurfaceColor
		};
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		_iconPreview = new PictureBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.FixedSingle,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = SurfaceColor
		};
		_iconRawPreview = new PictureBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.FixedSingle,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = SurfaceColor
		};
		tableLayoutPanel3.Controls.Add(WrapPreview("Icon", _iconPreview), 0, 0);
		tableLayoutPanel3.Controls.Add(WrapPreview("IconRaw", _iconRawPreview), 1, 0);
		tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 5);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = SurfaceColor
		};
		Button button = new Button
		{
			Text = "Add Icon...",
			Width = 120,
			Height = 30
		};
		Button button2 = new Button
		{
			Text = "Add IconRaw...",
			Width = 140,
			Height = 30
		};
		StyleButton(button, primary: false);
		StyleButton(button2, primary: false);
		button.Click += (object? _, EventArgs _) =>
		{
			PickImageForSelectedRow(icon: true);
		};
		button2.Click += (object? _, EventArgs _) =>
		{
			PickImageForSelectedRow(icon: false);
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(button2);
		tableLayoutPanel2.Controls.Add(flowLayoutPanel, 0, 6);
		_cardIdBox.TextChanged += (object? _, EventArgs _) =>
		{
			WriteEditorToSelectedRow();
		};
		_mobIdBox.TextChanged += (object? _, EventArgs _) =>
		{
			WriteEditorToSelectedRow();
		};
		_nameBox.TextChanged += (object? _, EventArgs _) =>
		{
			WriteEditorToSelectedRow();
		};
		_descriptionBox.TextChanged += (object? _, EventArgs _) =>
		{
			WriteEditorToSelectedRow();
		};
		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = AppBackColor
		};
		Button button3 = new Button
		{
			Text = "Remove Selected Rows",
			Width = 180,
			Height = 30
		};
		button3.Click += (object? _, EventArgs _) =>
		{
			RemoveSelectedRows();
		};
		_addCardsButton = new Button
		{
			Text = "Add Cards",
			Width = 120,
			Height = 30,
			DialogResult = DialogResult.OK
		};
		Button button4 = new Button
		{
			Text = "Cancel",
			Width = 120,
			Height = 30,
			DialogResult = DialogResult.Cancel
		};
		StyleButton(button3, primary: false);
		StyleButton(_addCardsButton, primary: true);
		StyleButton(button4, primary: false);
		flowLayoutPanel2.Controls.Add(button3);
		flowLayoutPanel2.Controls.Add(_addCardsButton);
		flowLayoutPanel2.Controls.Add(button4);
		tableLayoutPanel.Controls.Add(flowLayoutPanel2, 0, 2);
		AcceptButton = _addCardsButton;
		CancelButton = button4;
		RevalidateAllRows();
	}

	public IReadOnlyList<MassAddCardInput> GetRows()
	{
		List<MassAddCardInput> list = new List<MassAddCardInput>();
		foreach (DataGridViewRow item in (IEnumerable)_grid.Rows)
		{
			if (!item.IsNewRow)
			{
				string text = CellText(item, "CardId");
				string text2 = CellText(item, "MobId");
				string text3 = CellText(item, "Name");
				string text4 = CellText(item, "Description");
				string text5 = CellText(item, "IconPath");
				string text6 = CellText(item, "IconRawPath");
				if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(text2) || !string.IsNullOrWhiteSpace(text3) || !string.IsNullOrWhiteSpace(text4) || !string.IsNullOrWhiteSpace(text5) || !string.IsNullOrWhiteSpace(text6))
				{
					list.Add(new MassAddCardInput(text, text2, text3, text4, text5, text6));
				}
			}
		}
		return list;
	}

	private TextBox AddField(TableLayoutPanel parent, string label, int rowIndex, bool multiline = false)
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = SurfaceColor
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = label,
			Dock = DockStyle.Fill,
			ForeColor = SubtleTextColor
		}, 0, 0);
		TextBox textBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = multiline,
			BackColor = SurfaceRaisedColor,
			ForeColor = TextColor,
			BorderStyle = BorderStyle.FixedSingle
		};
		tableLayoutPanel.Controls.Add(textBox, 0, 1);
		parent.Controls.Add(tableLayoutPanel, 0, rowIndex);
		return textBox;
	}

	private Control WrapPreview(string label, Control preview)
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 2,
			BackColor = SurfaceColor
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = label,
			Dock = DockStyle.Fill,
			ForeColor = SubtleTextColor
		}, 0, 0);
		tableLayoutPanel.Controls.Add(preview, 0, 1);
		return tableLayoutPanel;
	}

	private void StyleButton(Button button, bool primary)
	{
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 1;
		button.FlatAppearance.BorderColor = (primary ? AccentColor : BorderColor);
		button.BackColor = (primary ? AccentColor : SurfaceColor);
		button.ForeColor = TextColor;
		button.Font = new Font("Bahnschrift SemiBold", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
	}

	private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
		{
			string name = _grid.Columns[e.ColumnIndex].Name;
			if (name == "PickIcon")
			{
				PickImageForRow(_grid.Rows[e.RowIndex], icon: true);
			}
			else if (name == "PickIconRaw")
			{
				PickImageForRow(_grid.Rows[e.RowIndex], icon: false);
			}
		}
	}

	private void LoadSelectedRowIntoEditor()
	{
		DataGridViewRow dataGridViewRow = SelectedRow();
		_syncingEditor = true;
		if (dataGridViewRow == null || dataGridViewRow.IsNewRow)
		{
			_cardIdBox.Text = "";
			_mobIdBox.Text = "";
			_nameBox.Text = "";
			_descriptionBox.Text = "";
			_iconPreview.Image?.Dispose();
			_iconPreview.Image = null;
			_iconRawPreview.Image?.Dispose();
			_iconRawPreview.Image = null;
		}
		else
		{
			_cardIdBox.Text = CellText(dataGridViewRow, "CardId");
			_mobIdBox.Text = CellText(dataGridViewRow, "MobId");
			_nameBox.Text = CellText(dataGridViewRow, "Name");
			_descriptionBox.Text = CellText(dataGridViewRow, "Description");
			SetPreview(_iconPreview, CellText(dataGridViewRow, "IconPath"));
			SetPreview(_iconRawPreview, CellText(dataGridViewRow, "IconRawPath"));
		}
		_syncingEditor = false;
	}

	private void WriteEditorToSelectedRow()
	{
		if (!_syncingEditor)
		{
			DataGridViewRow dataGridViewRow = SelectedRow();
			if (dataGridViewRow != null && !dataGridViewRow.IsNewRow)
			{
				dataGridViewRow.Cells["CardId"].Value = _cardIdBox.Text.Trim();
				dataGridViewRow.Cells["MobId"].Value = _mobIdBox.Text.Trim();
				dataGridViewRow.Cells["Name"].Value = _nameBox.Text;
				dataGridViewRow.Cells["Description"].Value = _descriptionBox.Text;
				RevalidateAllRows();
			}
		}
	}

	private DataGridViewRow? SelectedRow()
	{
		return (_grid.SelectedRows.Count == 0) ? null : _grid.SelectedRows[0];
	}

	private void PickImageForSelectedRow(bool icon)
	{
		DataGridViewRow dataGridViewRow = SelectedRow();
		if (dataGridViewRow != null && !dataGridViewRow.IsNewRow)
		{
			PickImageForRow(dataGridViewRow, icon);
		}
	}

	private void PickImageForRow(DataGridViewRow row, bool icon)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select image",
			Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*"
		};
		try
		{
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				string columnName = (icon ? "IconPath" : "IconRawPath");
				string columnName2 = (icon ? "IconPreview" : "IconRawPreview");
				row.Cells[columnName].Value = openFileDialog.FileName;
				row.Cells[columnName2].Value = LoadThumb(openFileDialog.FileName);
				RevalidateAllRows();
				if (row.Selected)
				{
					LoadSelectedRowIntoEditor();
				}
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private static Image? LoadThumb(string path)
	{
		try
		{
			using Image original = Image.FromFile(path);
			return new Bitmap(original, new Size(40, 40));
		}
		catch
		{
			return null;
		}
	}

	private static void SetPreview(PictureBox box, string path)
	{
		box.Image?.Dispose();
		box.Image = null;
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			using Image image = Image.FromFile(path);
			box.Image = (Image)image.Clone();
		}
		catch
		{
		}
	}

	private void OnGridDataChanged()
	{
		if (!_syncingEditor)
		{
			RevalidateAllRows();
		}
	}

	private void RevalidateAllRows()
	{
		int num = 0;
		int num2 = 0;
		foreach (DataGridViewRow item in (IEnumerable)_grid.Rows)
		{
			if (!item.IsNewRow && (!string.IsNullOrWhiteSpace(CellText(item, "CardId")) || !string.IsNullOrWhiteSpace(CellText(item, "MobId")) || !string.IsNullOrWhiteSpace(CellText(item, "Name")) || !string.IsNullOrWhiteSpace(CellText(item, "Description")) || !string.IsNullOrWhiteSpace(CellText(item, "IconPath")) || !string.IsNullOrWhiteSpace(CellText(item, "IconRawPath"))))
			{
				num++;
				string text = ValidateRow(item);
				item.Cells["Status"].Value = text;
				if (text == "OK")
				{
					num2++;
				}
			}
		}
		_summaryLabel.Text = $"{num} rows | {num2} valid | {Math.Max(0, num - num2)} errors";
		_addCardsButton.Enabled = num > 0 && num2 == num;
	}

	private static string ValidateRow(DataGridViewRow row)
	{
		string s = CellText(row, "MobId");
		string path = CellText(row, "IconPath");
		string path2 = CellText(row, "IconRawPath");
		if (!int.TryParse(s, out var _))
		{
			return "Bad Mob ID";
		}
		if (!File.Exists(path))
		{
			return "Missing Icon";
		}
		if (!File.Exists(path2))
		{
			return "Missing IconRaw";
		}
		return "OK";
	}

	private void RemoveSelectedRows()
	{
		List<DataGridViewRow> list = new List<DataGridViewRow>();
		foreach (DataGridViewRow selectedRow in _grid.SelectedRows)
		{
			if (!selectedRow.IsNewRow)
			{
				list.Add(selectedRow);
			}
		}
		foreach (DataGridViewRow item in list)
		{
			if (item.Cells["IconPreview"].Value is Image image)
			{
				image.Dispose();
			}
			if (item.Cells["IconRawPreview"].Value is Image image2)
			{
				image2.Dispose();
			}
			_grid.Rows.Remove(item);
		}
		RevalidateAllRows();
		LoadSelectedRowIntoEditor();
	}

	private static string CellText(DataGridViewRow row, string name)
	{
		return row.Cells[name].Value?.ToString()?.Trim() ?? string.Empty;
	}
}
