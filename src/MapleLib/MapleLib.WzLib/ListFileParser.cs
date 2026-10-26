using System.Collections.Generic;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib;

public static class ListFileParser
{
	public static List<string> ParseListFile(string filePath, WzMapleVersion version)
	{
		return ParseListFile(filePath, WzTool.GetIvByMapleVersion(version));
	}

	private static List<string> ParseListFile(string filePath, byte[] WzIv)
	{
		List<string> list = new List<string>();
		byte[] buffer = File.ReadAllBytes(filePath);
		using (WzBinaryReader wzBinaryReader = new WzBinaryReader(new MemoryStream(buffer), WzIv, 0L))
		{
			while (wzBinaryReader.PeekChar() != -1)
			{
				int num = wzBinaryReader.ReadInt32();
				char[] array = new char[num];
				for (int i = 0; i < num; i++)
				{
					array[i] = (char)wzBinaryReader.ReadInt16();
				}
				wzBinaryReader.ReadUInt16();
				string item = wzBinaryReader.DecryptString(array);
				list.Add(item);
			}
		}
		int index = list.Count - 1;
		string text = list[index];
		list[index] = text.Substring(0, text.Length - 1) + "g";
		return list;
	}

	public static void SaveToDisk(string path, WzMapleVersion version, List<string> listEntries)
	{
		SaveToDisk(path, WzTool.GetIvByMapleVersion(version), listEntries);
	}

	public static void SaveToDisk(string path, byte[] WzIv, List<string> listEntries)
	{
		int index = listEntries.Count - 1;
		string text = listEntries[index];
		listEntries[index] = text.Substring(0, text.Length - 1) + "/";
		WzBinaryWriter wzBinaryWriter = new WzBinaryWriter(File.Create(path), WzIv);
		foreach (string listEntry in listEntries)
		{
			wzBinaryWriter.Write(listEntry.Length);
			char[] array = wzBinaryWriter.EncryptString(listEntry + "\0");
			for (int i = 0; i < array.Length; i++)
			{
				wzBinaryWriter.Write((short)array[i]);
			}
		}
		listEntries[index] = text.Substring(0, text.Length - 1) + "/";
	}
}
