using System.IO;
using System.Text.RegularExpressions;

namespace MapleLib.WzLib.Serializer;

public abstract class ProgressingWzSerializer
{
	protected int total = 0;

	protected int curr = 0;

	private static readonly string regexSearch = ":" + new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

	private static readonly Regex regex_invalidPath = new Regex($"[{Regex.Escape(regexSearch)}]");

	public int Total => total;

	public int Current => curr;

	protected static void CreateDirSafe(ref string path)
	{
		if (path.Substring(path.Length - 1, 1) == "\\")
		{
			path = path.Substring(0, path.Length - 1);
		}
		string text = path;
		int num = 0;
		while (Directory.Exists(path) || File.Exists(path))
		{
			num++;
			path = text + num;
		}
		Directory.CreateDirectory(path);
	}

	public static string EscapeInvalidFilePathNames(string path)
	{
		return regex_invalidPath.Replace(path, "");
	}
}
