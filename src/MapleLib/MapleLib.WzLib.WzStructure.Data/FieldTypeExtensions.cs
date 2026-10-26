namespace MapleLib.WzLib.WzStructure.Data;

public static class FieldTypeExtensions
{
	public static string ToReadableString(this FieldType str)
	{
		string text = str.ToString().Replace("FIELDTYPE_", "");
		string text2 = text.Substring(0, 1) + text.Substring(1).ToLower();
		return text2 + $" = {(int)str}";
	}
}
