using System.Text;

namespace MapleLib.WzLib.Util;

public class XmlUtil
{
	private static readonly char[] specialCharacters = new char[5] { '"', '\'', '&', '<', '>' };

	private static readonly string[] replacementStrings = new string[5] { "&quot;", "&apos;", "&amp;", "&lt;", "&gt;" };

	public static string SanitizeText(string text)
	{
		StringBuilder stringBuilder = new StringBuilder("");
		for (int i = 0; i < text.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < specialCharacters.Length; j++)
			{
				if (text[i] == specialCharacters[j])
				{
					stringBuilder.Append(replacementStrings[j]);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				stringBuilder.Append(text[i]);
			}
		}
		return stringBuilder.ToString();
	}

	public static string OpenNamedTag(string tag, string name, bool finish)
	{
		return OpenNamedTag(tag, name, finish, empty: false);
	}

	public static string EmptyNamedTag(string tag, string name)
	{
		return OpenNamedTag(tag, name, finish: true, empty: true);
	}

	public static string EmptyNamedValuePair(string tag, string name, string value)
	{
		return OpenNamedTag(tag, name, finish: false, empty: false) + Attrib("value", value, closeTag: true, empty: true);
	}

	public static string OpenNamedTag(string tag, string name, bool finish, bool empty)
	{
		return "<" + tag + " name=\"" + name + "\"" + ((!finish) ? " " : (empty ? "/>" : ">"));
	}

	public static string Attrib(string name, string value)
	{
		return Attrib(name, value, closeTag: false, empty: false);
	}

	public static string Attrib(string name, string value, bool closeTag, bool empty)
	{
		return name + "=\"" + SanitizeText(value) + "\"" + ((!closeTag) ? " " : (empty ? "/>" : ">"));
	}

	public static string CloseTag(string tag)
	{
		return "</" + tag + ">";
	}

	public static string Indentation(int level)
	{
		char[] array = new char[level];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = '\t';
		}
		return new string(array);
	}
}
