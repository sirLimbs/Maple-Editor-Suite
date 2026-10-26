using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MapleLib.Configuration;

namespace MapleLib.WzLib.Util;

public class WzTool
{
	public const int WZ_HEADER = 826755920;

	public static Hashtable StringCache = new Hashtable();

	public static uint RotateLeft(uint x, byte n)
	{
		return (x << (int)n) | (x >> 32 - n);
	}

	public static uint RotateRight(uint x, byte n)
	{
		return (x >> (int)n) | (x << 32 - n);
	}

	public static int GetCompressedIntLength(int i)
	{
		if (i > 127 || i < -127)
		{
			return 5;
		}
		return 1;
	}

	public static int GetEncodedStringLength(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 1;
		}
		bool flag = false;
		int length = s.Length;
		foreach (char c in s)
		{
			if (c > 'ÿ')
			{
				flag = true;
				break;
			}
		}
		int num = ((length <= (flag ? 126 : 127)) ? 1 : 5);
		int num2 = (flag ? (length * 2) : length);
		return num + num2;
	}

	public static int GetWzObjectValueLength(string s, byte type)
	{
		string key = type + "_" + s;
		if (s.Length > 4 && StringCache.ContainsKey(key))
		{
			return 5;
		}
		StringCache[key] = 1;
		return 1 + GetEncodedStringLength(s);
	}

	public static byte[] GetIvByMapleVersion(WzMapleVersion ver)
	{
		switch (ver)
		{
		case WzMapleVersion.EMS:
			return WzAESConstant.WZ_MSEAIV;
		case WzMapleVersion.GMS:
			return WzAESConstant.WZ_GMSIV;
		case WzMapleVersion.CUSTOM:
		{
			ConfigurationManager configurationManager = new ConfigurationManager();
			return configurationManager.GetCusomWzIVEncryption();
		}
		case WzMapleVersion.GENERATE:
			return new byte[4];
		default:
			return WzAESConstant.WZ_BMSCLASSIC;
		}
	}

	private static int GetRecognizedCharacters(string source)
	{
		return source.Count((char c) => c >= ' ' && c <= '~');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryBruteforcingWzIVKey(string wzPath, byte[] wzIvKey)
	{
		using (WzFile wzFile = new WzFile(wzPath, wzIvKey))
		{
			string empty = string.Empty;
			WzFileParseStatus wzFileParseStatus = wzFile.ParseMainWzDirectory(lazyParse: true);
			if (wzFileParseStatus != WzFileParseStatus.Success)
			{
				wzFile.Dispose();
				return false;
			}
			if (wzFile.WzDirectory.WzImages.Count > 0)
			{
				string name = wzFile.WzDirectory.WzImages[0].Name;
				if (name.EndsWith(".img"))
				{
					wzFile.Dispose();
					return true;
				}
			}
			wzFile.Dispose();
		}
		return false;
	}

	private static double GetDecryptionSuccessRate(string wzPath, WzMapleVersion encVersion, ref short? version)
	{
		WzFile wzFile = (version.HasValue ? new WzFile(wzPath, version.Value, encVersion) : new WzFile(wzPath, encVersion));
		WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
		if (wzFileParseStatus != WzFileParseStatus.Success)
		{
			return 0.0;
		}
		if (!version.HasValue)
		{
			version = wzFile.Version;
		}
		int num = 0;
		int num2 = 0;
		foreach (WzDirectory wzDirectory in wzFile.WzDirectory.WzDirectories)
		{
			num += GetRecognizedCharacters(wzDirectory.Name);
			num2 += wzDirectory.Name.Length;
		}
		foreach (WzImage wzImage in wzFile.WzDirectory.WzImages)
		{
			num += GetRecognizedCharacters(wzImage.Name);
			num2 += wzImage.Name.Length;
		}
		wzFile.Dispose();
		return (double)num / (double)num2;
	}

	public static WzMapleVersion DetectMapleVersion(string wzFilePath, out short fileVersion)
	{
		Hashtable hashtable = new Hashtable();
		short? version = null;
		hashtable.Add(WzMapleVersion.GMS, GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.GMS, ref version));
		hashtable.Add(WzMapleVersion.EMS, GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.EMS, ref version));
		hashtable.Add(WzMapleVersion.BMS, GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.BMS, ref version));
		fileVersion = version.Value;
		WzMapleVersion result = WzMapleVersion.GMS;
		double num = 0.0;
		foreach (DictionaryEntry item in hashtable)
		{
			if ((double)item.Value > num)
			{
				result = (WzMapleVersion)item.Key;
				num = (double)item.Value;
			}
		}
		if (num < 0.7 && File.Exists(Path.Combine(Path.GetDirectoryName(wzFilePath), "ZLZ.dll")))
		{
			return WzMapleVersion.GETFROMZLZ;
		}
		return result;
	}

	public static bool IsListFile(string path)
	{
		using BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
		int num = binaryReader.ReadInt32();
		return num != 826755920;
	}

	public static bool IsDataWzHotfixFile(string path)
	{
		bool result = false;
		using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
		{
			byte b = binaryReader.ReadByte();
			result = b == 115;
		}
		return result;
	}

	private static byte[] Combine(byte[] a, byte[] b)
	{
		byte[] array = new byte[a.Length + b.Length];
		Array.Copy(a, 0, array, 0, a.Length);
		Array.Copy(b, 0, array, a.Length, b.Length);
		return array;
	}
}
