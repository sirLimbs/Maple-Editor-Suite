using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class RoundedDropDownButton : RoundedButton
{
	public RoundedDropDownButton()
	{
		ContentRightPadding = 18;
	}

	protected override void OnPaint(PaintEventArgs pevent)
	{
		base.OnPaint(pevent);
		using SolidBrush brush = new SolidBrush(Enabled ? ForeColor : UiTheme.TextMuted);
		float centerX = Width - 15f;
		float centerY = Height / 2f;
		PointF[] points = new PointF[3]
		{
			new PointF(centerX - 4f, centerY - 2f),
			new PointF(centerX + 4f, centerY - 2f),
			new PointF(centerX, centerY + 3f)
		};
		pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		pevent.Graphics.FillPolygon(brush, points);
	}
}
