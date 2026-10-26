using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MapleEditor.UI;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleImgEditor;

internal sealed class InfoNodeEditorForm : Form
{
	private readonly WzSubProperty _info;

	private readonly DataGridView _grid;

	private readonly List<WzImageProperty> _deleted = new List<WzImageProperty>();

	public InfoNodeEditorForm(WzSubProperty info, string itemId)
	{
		_info = info;
		Text = "Edit WZ nodes - " + itemId;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.Sizable;
		MinimizeBox = false;
		ShowInTaskbar = false;
		ClientSize = new Size(700, 460);
		MinimumSize = new Size(560, 360);
		BackColor = UiTheme.Surface;
		ForeColor = UiTheme.Text;
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		Label hint = new Label
		{
			Dock = DockStyle.Top,
			Height = 42,
			Padding = new Padding(12, 8, 12, 0),
			Text = "Edit scalar nodes directly under info. Int, Short, Long, String and Vector nodes are editable; canvas/sub nodes are shown read-only.",
			ForeColor = UiTheme.TextSubtle
		};
		Controls.Add(hint);
		Panel buttons = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 50,
			Padding = new Padding(12, 8, 12, 8)
		};
		Controls.Add(buttons);
		Button addButton = CreateButton("Add node", 0);
		addButton.Click += (object? _, EventArgs _) =>
		{
			int index = _grid.Rows.Add("newNode", "Int", "0");
			_grid.Rows[index].Tag = null;
			_grid.Rows[index].Cells[1].ReadOnly = false;
			_grid.ClearSelection();
			_grid.Rows[index].Selected = true;
			_grid.CurrentCell = _grid.Rows[index].Cells[0];
			_grid.BeginEdit(selectAll: true);
		};
		buttons.Controls.Add(addButton);
		Button deleteButton = CreateButton("Delete node", 104);
		deleteButton.Click += (object? _, EventArgs _) =>
		{
			foreach (DataGridViewRow row in _grid.SelectedRows)
			{
				if (row.Tag is WzImageProperty property)
				{
					_deleted.Add(property);
				}
				if (!row.IsNewRow)
				{
					_grid.Rows.Remove(row);
				}
			}
		};
		buttons.Controls.Add(deleteButton);
		Button okButton = CreateButton("Apply", 540);
		okButton.Width = 70;
		okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		okButton.BackColor = UiTheme.Accent;
		okButton.ForeColor = Color.White;
		okButton.Click += (object? _, EventArgs _) =>
		{
			if (!ApplyChanges())
			{
				return;
			}
			DialogResult = DialogResult.OK;
		};
		buttons.Controls.Add(okButton);
		Button cancelButton = CreateButton("Cancel", 616);
		cancelButton.Width = 70;
		cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		cancelButton.DialogResult = DialogResult.Cancel;
		buttons.Controls.Add(cancelButton);
		AcceptButton = okButton;
		CancelButton = cancelButton;
		_grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			AllowUserToResizeRows = false,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			BackgroundColor = UiTheme.SurfaceRaised,
			BorderStyle = BorderStyle.None,
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
			ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
			ColumnHeadersHeight = 30,
			EnableHeadersVisualStyles = false,
			GridColor = UiTheme.Border,
			RowHeadersVisible = false,
			RowTemplate = { Height = 26 },
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			MultiSelect = true,
			EditMode = DataGridViewEditMode.EditOnEnter
		};
		_grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceRaised;
		_grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
		_grid.DefaultCellStyle.BackColor = UiTheme.Surface;
		_grid.DefaultCellStyle.ForeColor = UiTheme.Text;
		_grid.DefaultCellStyle.SelectionBackColor = UiTheme.AccentSoft;
		_grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
		DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn
		{
			HeaderText = "Node",
			FillWeight = 42
		};
		DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn
		{
			HeaderText = "Type",
			FillWeight = 18
		};
		DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn
		{
			HeaderText = "Value",
			FillWeight = 40
		};
		_grid.Columns.AddRange(nameColumn, typeColumn, valueColumn);
		_grid.DataError += (object? _, DataGridViewDataErrorEventArgs e) =>
		{
			e.ThrowException = false;
		};
		Controls.Add(_grid);
		LoadNodes();
	}

	private static Button CreateButton(string text, int left)
	{
		Button button = new Button
		{
			Left = left,
			Top = 8,
			Width = 96,
			Height = 30,
			Text = text,
			FlatStyle = FlatStyle.Flat,
			BackColor = UiTheme.AccentSoft,
			ForeColor = UiTheme.Text
		};
		button.FlatAppearance.BorderColor = UiTheme.Border;
		button.FlatAppearance.BorderSize = 1;
		return button;
	}

	private void LoadNodes()
	{
		foreach (WzImageProperty property in _info.WzProperties)
		{
			int index = _grid.Rows.Add(property.Name, GetTypeName(property), FormatValue(property));
			DataGridViewRow row = _grid.Rows[index];
			row.Tag = property;
			if (!IsEditable(property))
			{
				row.ReadOnly = true;
				row.DefaultCellStyle.ForeColor = UiTheme.TextMuted;
			}
			else
			{
				row.Cells[1].ReadOnly = true;
			}
		}
	}

	private static string GetTypeName(WzImageProperty property)
	{
		return property switch
		{
			WzIntProperty => "Int", 
			WzShortProperty => "Short", 
			WzLongProperty => "Long", 
			WzStringProperty => "String", 
			WzVectorProperty => "Vector", 
			WzCanvasProperty => "Canvas", 
			WzSubProperty => "Sub", 
			WzUOLProperty => "UOL", 
			_ => property.PropertyType.ToString()
		};
	}

	private static bool IsEditable(WzImageProperty property)
	{
		return property is WzIntProperty || property is WzShortProperty || property is WzLongProperty || property is WzStringProperty || property is WzVectorProperty;
	}

	private static string FormatValue(WzImageProperty property)
	{
		return property switch
		{
			WzIntProperty intProperty => intProperty.Value.ToString(), 
			WzShortProperty shortProperty => shortProperty.Value.ToString(), 
			WzLongProperty longProperty => longProperty.Value.ToString(), 
			WzStringProperty stringProperty => stringProperty.Value, 
			WzVectorProperty vectorProperty => $"{vectorProperty.X.Value}, {vectorProperty.Y.Value}", 
			_ => string.Empty
		};
	}

	private bool ApplyChanges()
	{
		List<string> names = new List<string>();
		foreach (DataGridViewRow row in _grid.Rows)
		{
			string name = (row.Cells[0].Value as string)?.Trim() ?? string.Empty;
			if (name.Length == 0)
			{
				MessageBox.Show(this, "Every node needs a name.", "Edit WZ nodes", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			if (names.Any((string existing) => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
			{
				MessageBox.Show(this, "Node names must be unique: " + name, "Edit WZ nodes", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			names.Add(name);
		}
		foreach (WzImageProperty property in _deleted)
		{
			_info.RemoveProperty(property);
		}
		foreach (DataGridViewRow row in _grid.Rows)
		{
			string name = (row.Cells[0].Value as string)?.Trim() ?? string.Empty;
			string value = (row.Cells[2].Value as string) ?? string.Empty;
			if (row.Tag is WzImageProperty existing)
			{
				if (!IsEditable(existing))
				{
					continue;
				}
				existing.Name = name;
				if (!TrySetValue(existing, value, out string? error))
				{
					MessageBox.Show(this, error, "Edit WZ nodes", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
			}
			else
			{
				string type = (row.Cells[1].Value as string) ?? "Int";
				WzImageProperty? created = CreateProperty(type, name, value, out string? error);
				if (created == null)
				{
					MessageBox.Show(this, error, "Edit WZ nodes", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				_info.AddProperty(created);
			}
		}
		return true;
	}

	private static WzImageProperty? CreateProperty(string type, string name, string value, out string? error)
	{
		error = null;
		if (string.Equals(type, "String", StringComparison.OrdinalIgnoreCase))
		{
			return new WzStringProperty(name, value);
		}
		if (string.Equals(type, "Vector", StringComparison.OrdinalIgnoreCase))
		{
			if (!TryParseVector(value, out int x, out int y))
			{
				error = "Vector values must use the format x, y.";
				return null;
			}
			return new WzVectorProperty(name, x, y);
		}
		if (string.Equals(type, "Short", StringComparison.OrdinalIgnoreCase))
		{
			if (!short.TryParse(value, out short shortValue))
			{
				error = "That value is not a valid Short.";
				return null;
			}
			return new WzShortProperty(name, shortValue);
		}
		if (string.Equals(type, "Long", StringComparison.OrdinalIgnoreCase))
		{
			if (!long.TryParse(value, out long longValue))
			{
				error = "That value is not a valid Long.";
				return null;
			}
			return new WzLongProperty(name, longValue);
		}
		if (!int.TryParse(value, out int intValue))
		{
			error = "That value is not a valid Int.";
			return null;
		}
		return new WzIntProperty(name, intValue);
	}

	private static bool TrySetValue(WzImageProperty property, string value, out string? error)
	{
		error = null;
		switch (property)
		{
		case WzIntProperty intProperty:
			if (!int.TryParse(value, out int intValue))
			{
				error = "That value is not a valid Int.";
				return false;
			}
			intProperty.Value = intValue;
			return true;
		case WzShortProperty shortProperty:
			if (!short.TryParse(value, out short shortValue))
			{
				error = "That value is not a valid Short.";
				return false;
			}
			shortProperty.Value = shortValue;
			return true;
		case WzLongProperty longProperty:
			if (!long.TryParse(value, out long longValue))
			{
				error = "That value is not a valid Long.";
				return false;
			}
			longProperty.Value = longValue;
			return true;
		case WzStringProperty stringProperty:
			stringProperty.Value = value;
			return true;
		case WzVectorProperty vectorProperty:
			if (!TryParseVector(value, out int x, out int y))
			{
				error = "Vector values must use the format x, y.";
				return false;
			}
			vectorProperty.X.Value = x;
			vectorProperty.Y.Value = y;
			return true;
		default:
			return true;
		}
	}

	private static bool TryParseVector(string value, out int x, out int y)
	{
		x = 0;
		y = 0;
		string[] parts = value.Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
	}
}
