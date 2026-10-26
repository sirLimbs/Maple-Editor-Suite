using System;
using System.Drawing;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class DarkComboBox : ComboBox
{
	public DarkComboBox()
	{
		DrawMode = DrawMode.OwnerDrawFixed;
		FlatStyle = FlatStyle.Flat;
		BackColor = UiTheme.SurfaceRaised;
		ForeColor = UiTheme.Text;
		ItemHeight = 26;
		Font = UiTheme.Font(9.5f);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		DrawMode = DrawMode.OwnerDrawFixed;
		FlatStyle = FlatStyle.Flat;
		BackColor = UiTheme.SurfaceRaised;
		ForeColor = UiTheme.Text;
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		Graphics graphics = e.Graphics;
		if (e.Index < 0)
		{
			using SolidBrush brush = new SolidBrush(BackColor);
			graphics.FillRectangle(brush, e.Bounds);
			return;
		}
		bool flag = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
		Color color = flag ? UiTheme.Blend(UiTheme.SurfaceRaised, UiTheme.Accent, 0.35) : UiTheme.SurfaceRaised;
		using (SolidBrush brush2 = new SolidBrush(color))
		{
			graphics.FillRectangle(brush2, e.Bounds);
		}
		string itemText = GetItemText(Items[e.Index]);
		TextRenderer.DrawText(graphics, itemText, Font, e.Bounds, UiTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}
}
