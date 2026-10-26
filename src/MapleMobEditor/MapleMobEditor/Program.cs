using System;
using System.Windows.Forms;
using MapleEditor.UI;

namespace MapleMobEditor;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();
		SplashForm.ShowStartup("Maple Editor Suite", "WZ Studio for v83 and v95", 1200);
		Application.Run(new EditorLauncherForm());
	}
}
