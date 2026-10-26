using System.Windows.Forms;
using MapleEditor.UI;

namespace MapleMobEditor;

internal sealed class InputDialog : Form
{
	private readonly TextBox _valueTextBox;

	public string Value => _valueTextBox.Text;

	public InputDialog(string title, string label, string initialValue)
	{
		Text = title;
		Icon = UiTheme.AppIcon;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		Width = 420;
		Height = 150;
		Label value = new Label
		{
			AutoSize = true,
			Left = 12,
			Top = 15,
			Text = label
		};
		_valueTextBox = new TextBox
		{
			Left = 12,
			Top = 40,
			Width = 380,
			Text = initialValue
		};
		Button button = new Button
		{
			Text = "OK",
			DialogResult = DialogResult.OK,
			Left = 236,
			Width = 75,
			Top = 74
		};
		Button button2 = new Button
		{
			Text = "Cancel",
			DialogResult = DialogResult.Cancel,
			Left = 317,
			Width = 75,
			Top = 74
		};
		Controls.Add(value);
		Controls.Add(_valueTextBox);
		Controls.Add(button);
		Controls.Add(button2);
		AcceptButton = button;
		CancelButton = button2;
	}
}
