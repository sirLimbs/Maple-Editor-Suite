using System;
using System.Globalization;

namespace MapleLib.PacketLib;

public class HexEncoding
{
	public static bool IsHexDigit(char c)
	{
		int num = Convert.ToInt32('A');
		int num2 = Convert.ToInt32('0');
		c = char.ToUpper(c);
		int num3 = Convert.ToInt32(c);
		return (num3 >= num && num3 < num + 6) || (num3 >= num2 && num3 < num2 + 10);
	}

	private static byte HexToByte(string hex)
	{
		if (hex.Length > 2 || hex.Length <= 0)
		{
			throw new ArgumentException("hex must be 1 or 2 characters in length");
		}
		return byte.Parse(hex, NumberStyles.HexNumber);
	}

	public static byte[] GetBytes(string hexString)
	{
		string text = string.Empty;
		foreach (char c in hexString)
		{
			if (IsHexDigit(c))
			{
				text += c;
			}
		}
		if (text.Length % 2 != 0)
		{
			text = text.Substring(0, text.Length - 1);
		}
		int num = text.Length / 2;
		byte[] array = new byte[num];
		int num2 = 0;
		for (int j = 0; j < array.Length; j++)
		{
			string hex = new string(new char[2]
			{
				text[num2],
				text[num2 + 1]
			});
			array[j] = HexToByte(hex);
			num2 += 2;
		}
		return array;
	}

	public static string ToStringFromAscii(byte[] bytes)
	{
		char[] array = new char[bytes.Length];
		for (int i = 0; i < bytes.Length; i++)
		{
			array[i] = ((bytes[i] < 32 && bytes[i] >= 0) ? '.' : ((char)(bytes[i] & 0xFF)));
		}
		return new string(array);
	}
}
