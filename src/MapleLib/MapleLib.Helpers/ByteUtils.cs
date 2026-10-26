using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MapleLib.Helpers;

public static class ByteUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool CompareBytearrays(byte[] a, byte[] b)
	{
		return a.Length == b.Length && a.SequenceEqual(b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] IntegerToLittleEndian(int data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] HexToBytes(string pValue)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char item in pValue.Where(IsHexDigit).Select(char.ToUpper))
		{
			stringBuilder.Append(item);
		}
		string text = stringBuilder.ToString();
		if (text.Length % 2 == 1)
		{
			text += "0";
		}
		byte[] array = new byte[text.Length / 2];
		Random random = new Random();
		int num = 0;
		int num2 = 0;
		while (num < array.Length)
		{
			string text2 = string.Concat(text[num2], text[num2 + 1]);
			if (text2 == "**")
			{
				array[num] = (byte)random.Next(0, 255);
			}
			else
			{
				array[num] = HexToByte(text2);
			}
			num++;
			num2 += 2;
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string BytesToHex(byte[] bytes, string header = "")
	{
		StringBuilder stringBuilder = new StringBuilder(header);
		foreach (byte b in bytes)
		{
			stringBuilder.AppendFormat("{0:X2} ", b);
		}
		return stringBuilder.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexDigit(char c)
	{
		return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f') || c == '*';
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte HexToByte(string hex)
	{
		if (hex == null)
		{
			throw new ArgumentNullException("hex");
		}
		if (hex.Length == 0 || 2 < hex.Length)
		{
			throw new ArgumentOutOfRangeException("hex", "The hexadecimal string must be 1 or 2 characters in length.");
		}
		return byte.Parse(hex, NumberStyles.HexNumber);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RotateLeft(uint x, byte n)
	{
		return (x << (int)n) | (x >> 32 - n);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RotateRight(uint x, byte n)
	{
		return (x >> (int)n) | (x << 32 - n);
	}
}
