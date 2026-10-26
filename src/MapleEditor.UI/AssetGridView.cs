using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class AssetGridRow
{
	public object Tag { get; set; }

	public string Id { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string Level { get; set; } = string.Empty;

	public string Flags { get; set; } = string.Empty;

	public bool IsDirty { get; set; }

	public Image Icon { get; set; }
}

internal sealed class BufferedDataGridView : DataGridView
{
	public BufferedDataGridView()
	{
		DoubleBuffered = true;
	}
}

public sealed class AssetGridRowEventArgs : EventArgs
{
	public AssetGridRow Row { get; }

	public AssetGridRowEventArgs(AssetGridRow row)
	{
		Row = row;
	}
}

public sealed class AssetGridView : UserControl
{
	private readonly BufferedDataGridView _grid;

	private bool _updating;

	public event EventHandler SelectionChanged;

	public event EventHandler CopyRequested;

	public event EventHandler<AssetGridRowEventArgs> RowActivated;

	public IReadOnlyList<AssetGridRow> SelectedRows
	{
		get
		{
			List<AssetGridRow> list = new List<AssetGridRow>();
			foreach (DataGridViewRow selectedRow in _grid.SelectedRows)
			{
				if (selectedRow.Tag is AssetGridRow item)
				{
					list.Add(item);
				}
			}
			list.Sort((AssetGridRow a, AssetGridRow b) => CompareIds(a.Id, b.Id));
			return list;
		}
	}

	public AssetGridRow PrimaryRow
	{
		get
		{
			if (_grid.SelectedRows.Count == 0)
			{
				return null;
			}
			return _grid.SelectedRows[0].Tag as AssetGridRow;
		}
	}

	public int RowCount => _grid.Rows.Count;

	public int IconSize { get; } = 36;

	public AssetGridView()
	{
		BackColor = UiTheme.Surface;
		_grid = new BufferedDataGridView
		{
			Dock = DockStyle.Fill,
			BackgroundColor = UiTheme.Surface,
			BorderStyle = BorderStyle.None,
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
			GridColor = Color.FromArgb(36, 45, 58),
			RowHeadersVisible = false,
			ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
			ColumnHeadersHeight = 38,
			ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
			EnableHeadersVisualStyles = false,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			AllowUserToResizeRows = false,
			AllowUserToResizeColumns = true,
			AllowUserToOrderColumns = false,
			ReadOnly = true,
			MultiSelect = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			ScrollBars = ScrollBars.Vertical,
			Font = UiTheme.Font(9.5f),
			DefaultCellStyle =
			{
				BackColor = UiTheme.Surface,
				ForeColor = UiTheme.Text,
				SelectionBackColor = UiTheme.Blend(UiTheme.Surface, UiTheme.Accent, 0.35),
				SelectionForeColor = UiTheme.Text,
				Padding = new Padding(6, 0, 4, 0)
			},
			AlternatingRowsDefaultCellStyle =
			{
				BackColor = Color.FromArgb(24, 31, 42),
				SelectionBackColor = UiTheme.Blend(UiTheme.Surface, UiTheme.Accent, 0.35),
				SelectionForeColor = UiTheme.Text
			},
			ColumnHeadersDefaultCellStyle =
			{
				BackColor = UiTheme.SurfaceRaised,
				ForeColor = UiTheme.TextSubtle,
				SelectionBackColor = UiTheme.SurfaceRaised,
				Font = UiTheme.Font(9f, FontStyle.Bold),
				Padding = new Padding(6, 0, 4, 0),
				Alignment = DataGridViewContentAlignment.MiddleLeft
			}
		};
		_grid.SelectionChanged += (object? _, EventArgs _) =>
		{
			if (!_updating)
			{
				this.SelectionChanged?.Invoke(this, EventArgs.Empty);
			}
		};
		_grid.RowTemplate.Height = 46;
		_grid.KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.Control && e.KeyCode == Keys.C)
			{
				this.CopyRequested?.Invoke(this, EventArgs.Empty);
				e.SuppressKeyPress = true;
				e.Handled = true;
			}
		};
		_grid.CellDoubleClick += (object? _, DataGridViewCellEventArgs e) =>
		{
			if (e.RowIndex >= 0)
			{
				this.SelectionChanged?.Invoke(this, EventArgs.Empty);
				if (_grid.Rows[e.RowIndex].Tag is AssetGridRow row)
				{
					this.RowActivated?.Invoke(this, new AssetGridRowEventArgs(row));
				}
			}
		};
		DataGridViewImageColumn iconColumn = new DataGridViewImageColumn
		{
			Name = "icon",
			HeaderText = string.Empty,
			Width = 52,
			MinimumWidth = 52,
			ImageLayout = DataGridViewImageCellLayout.Zoom,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
			DefaultCellStyle = { NullValue = null }
		};
		_grid.Columns.Add(iconColumn);
		AddTextColumn("id", "ID", 78, DataGridViewAutoSizeColumnMode.None);
		AddTextColumn("name", "Name", 100, DataGridViewAutoSizeColumnMode.Fill);
		AddTextColumn("category", "Category", 96, DataGridViewAutoSizeColumnMode.None);
		AddTextColumn("level", "Level", 54, DataGridViewAutoSizeColumnMode.None);
		AddTextColumn("flags", "Flags", 106, DataGridViewAutoSizeColumnMode.None);
		AddTextColumn("state", "Status", 60, DataGridViewAutoSizeColumnMode.None);
		Controls.Add(_grid);
	}

	private void AddTextColumn(string name, string header, int width, DataGridViewAutoSizeColumnMode mode)
	{
		DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
		{
			Name = name,
			HeaderText = header,
			Width = width,
			MinimumWidth = Math.Min(width, 56),
			AutoSizeMode = mode,
			SortMode = DataGridViewColumnSortMode.NotSortable
		};
		if (mode == DataGridViewAutoSizeColumnMode.Fill)
		{
			column.FillWeight = 100f;
		}
		_grid.Columns.Add(column);
	}

	public void ClearRows()
	{
		_updating = true;
		try
		{
			_grid.Rows.Clear();
		}
		finally
		{
			_updating = false;
		}
	}

	public void SetRows(IEnumerable<AssetGridRow> rows, IEnumerable<object> preserveSelection = null)
	{
		HashSet<object> hashSet = ((preserveSelection == null) ? new HashSet<object>() : new HashSet<object>(preserveSelection));
		List<AssetGridRow> list = (rows ?? Enumerable.Empty<AssetGridRow>()).OrderBy((AssetGridRow r) => r.Id, Comparer<string>.Create(CompareIds)).ToList();
		_updating = true;
		try
		{
			_grid.SuspendLayout();
			_grid.Rows.Clear();
			foreach (AssetGridRow item in list)
			{
				int index = _grid.Rows.Add(item.Icon, item.Id, item.Name, item.Category, item.Level, item.Flags, item.IsDirty ? "Dirty" : string.Empty);
				DataGridViewRow row = _grid.Rows[index];
				row.Tag = item;
				if (item.IsDirty)
				{
					row.DefaultCellStyle.ForeColor = UiTheme.Warning;
				}
			}
			_grid.ClearSelection();
			bool flag = false;
			if (hashSet.Count > 0)
			{
				foreach (DataGridViewRow row2 in _grid.Rows)
				{
					if (row2.Tag is AssetGridRow assetGridRow && hashSet.Contains(assetGridRow.Tag))
					{
						row2.Selected = true;
						flag = true;
					}
				}
			}
			if (!flag && _grid.Rows.Count > 0)
			{
				_grid.Rows[0].Selected = true;
			}
			if (_grid.Rows.Count > 0)
			{
				_grid.CurrentCell = _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Cells[1] : _grid.Rows[0].Cells[1];
			}
		}
		finally
		{
			_grid.ResumeLayout();
			_updating = false;
		}
		this.SelectionChanged?.Invoke(this, EventArgs.Empty);
	}

	public void SelectRow(object tag)
	{
		_updating = true;
		try
		{
			_grid.ClearSelection();
			foreach (DataGridViewRow row in _grid.Rows)
			{
				if (row.Tag is AssetGridRow assetGridRow && object.Equals(assetGridRow.Tag, tag))
				{
					row.Selected = true;
					_grid.CurrentCell = row.Cells[1];
					break;
				}
			}
		}
		finally
		{
			_updating = false;
		}
		this.SelectionChanged?.Invoke(this, EventArgs.Empty);
	}

	public void RefreshRow(AssetGridRow row)
	{
		if (row == null)
		{
			return;
		}
		foreach (DataGridViewRow item in (IEnumerable<DataGridViewRow>)_grid.Rows)
		{
			if (item.Tag == row)
			{
				item.Cells[0].Value = row.Icon;
				item.Cells[1].Value = row.Id;
				item.Cells[2].Value = row.Name;
				item.Cells[3].Value = row.Category;
				item.Cells[4].Value = row.Level;
				item.Cells[5].Value = row.Flags;
				item.Cells[6].Value = (row.IsDirty ? "Dirty" : string.Empty);
				item.DefaultCellStyle.ForeColor = (row.IsDirty ? UiTheme.Warning : UiTheme.Text);
				break;
			}
		}
	}

	public static int CompareIds(string a, string b)
	{
		if (long.TryParse(a, out long result) && long.TryParse(b, out long result2))
		{
			return result.CompareTo(result2);
		}
		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
	}
}


