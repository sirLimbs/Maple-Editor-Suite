using System.Drawing;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
	public ModernMenuRenderer()
		: base(new ModernColorTable())
	{
		RoundedEdges = true;
	}

	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
	{
		if (!e.Item.Selected && !e.Item.Pressed)
		{
			base.OnRenderMenuItemBackground(e);
			return;
		}
		Rectangle rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
		using System.Drawing.Drawing2D.GraphicsPath path = UiTheme.RoundedRect(rect, 6f);
		using SolidBrush brush = new SolidBrush(e.Item.Pressed ? UiTheme.SurfaceHover : UiTheme.AccentSoft);
		e.Graphics.FillPath(brush, path);
	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
	{
		e.TextColor = (e.Item.Enabled ? UiTheme.Text : UiTheme.TextMuted);
		base.OnRenderItemText(e);
	}

	protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
	{
		using Pen pen = new Pen(UiTheme.Border);
		e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
	}

	private sealed class ModernColorTable : ProfessionalColorTable
	{
		public override Color ToolStripDropDownBackground => UiTheme.SurfaceRaised;

		public override Color ImageMarginGradientBegin => UiTheme.SurfaceRaised;

		public override Color ImageMarginGradientMiddle => UiTheme.SurfaceRaised;

		public override Color ImageMarginGradientEnd => UiTheme.SurfaceRaised;

		public override Color MenuBorder => UiTheme.Border;

		public override Color MenuItemBorder => UiTheme.Accent;

		public override Color MenuItemSelected => UiTheme.AccentSoft;

		public override Color MenuItemSelectedGradientBegin => UiTheme.AccentSoft;

		public override Color MenuItemSelectedGradientEnd => UiTheme.AccentSoft;

		public override Color MenuItemPressedGradientBegin => UiTheme.SurfaceHover;

		public override Color MenuItemPressedGradientEnd => UiTheme.SurfaceHover;

		public override Color SeparatorDark => UiTheme.Border;

		public override Color SeparatorLight => UiTheme.SurfaceHover;

		public override Color StatusStripGradientBegin => UiTheme.Surface;

		public override Color StatusStripGradientEnd => UiTheme.Surface;

		public override Color ToolStripContentPanelGradientBegin => UiTheme.Surface;

		public override Color ToolStripContentPanelGradientEnd => UiTheme.Surface;

		public override Color ToolStripGradientBegin => UiTheme.Surface;

		public override Color ToolStripGradientMiddle => UiTheme.Surface;

		public override Color ToolStripGradientEnd => UiTheme.Surface;

		public override Color ButtonSelectedGradientBegin => UiTheme.AccentSoft;

		public override Color ButtonSelectedGradientEnd => UiTheme.AccentSoft;

		public override Color ButtonPressedGradientBegin => UiTheme.SurfaceHover;

		public override Color ButtonPressedGradientEnd => UiTheme.SurfaceHover;
	}
}
