using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MapleEditor.UI;
using MapleLib.WzLib;

namespace MapleImgEditor;

internal sealed class ArchiveCategoryForm : Form
{
	private readonly ListBox _listBox;

	public string? SelectedCategory { get; private set; }

	public ArchiveCategoryForm(WzArchiveSession session, string fileName)
	{
		Text = "Select " + fileName + " category";
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MinimizeBox = false;
		MaximizeBox = false;
		ClientSize = new Size(380, 420);
		BackColor = Color.FromArgb(29, 32, 40);
		ForeColor = Color.FromArgb(235, 239, 245);
		Font = new Font("Bahnschrift", 10f, FontStyle.Regular, GraphicsUnit.Point);
		Label label = new Label
		{
			Text = "Choose which folder to load. Large folders (Weapon, Longcoat, etc.) take longer because previews are generated eagerly.",
			Left = 16,
			Top = 12,
			Width = 348,
			Height = 52,
			ForeColor = Color.FromArgb(154, 163, 178)
		};
		Controls.Add(label);
		List<string> categories = session.File.WzDirectory.WzDirectories
			.Where((WzDirectory d) => d.WzImages.Count > 0 || d.WzDirectories.Count > 0)
			.Select((WzDirectory d) => d.Name)
			.OrderBy((string n) => n, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (session.File.WzDirectory.WzImages.Count > 0)
		{
			categories.Insert(0, "(root images)");
		}
		_listBox = new ListBox
		{
			Left = 16,
			Top = 68,
			Width = 348,
			Height = 300,
			BackColor = Color.FromArgb(37, 41, 51),
			ForeColor = Color.FromArgb(235, 239, 245),
			BorderStyle = BorderStyle.FixedSingle
		};
		_listBox.Items.AddRange(categories.ToArray());
		if (_listBox.Items.Count > 0)
		{
			_listBox.SelectedIndex = 0;
		}
		Controls.Add(_listBox);
		Button ok = new Button
		{
			Text = "Load",
			Left = 188,
			Top = 378,
			Width = 84,
			Height = 30,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(82, 163, 255),
			ForeColor = Color.White
		};
		ok.FlatAppearance.BorderSize = 1;
		ok.FlatAppearance.BorderColor = Color.FromArgb(57, 62, 74);
		Button cancel = new Button
		{
			Text = "Cancel",
			Left = 280,
			Top = 378,
			Width = 84,
			Height = 30,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(53, 70, 94),
			ForeColor = Color.FromArgb(235, 239, 245),
			DialogResult = DialogResult.Cancel
		};
		cancel.FlatAppearance.BorderSize = 1;
		cancel.FlatAppearance.BorderColor = Color.FromArgb(57, 62, 74);
		Controls.Add(ok);
		Controls.Add(cancel);
		AcceptButton = ok;
		CancelButton = cancel;
		ok.Click += (object? _, EventArgs _) =>
		{
			SelectedCategory = _listBox.SelectedItem as string;
			DialogResult = DialogResult.OK;
		};
	}
}

