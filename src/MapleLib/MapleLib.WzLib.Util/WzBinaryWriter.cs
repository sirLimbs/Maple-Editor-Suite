using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MapleLib.Helpers;
using MapleLib.WzLib.WzStructure.Enums;

namespace MapleLib.WzLib.Util;

public class WzBinaryWriter : BinaryWriter
{
	public WzMutableKey WzKey { get; set; }

	public uint Hash { get; set; }

	public Dictionary<string, int> StringCache { get; set; }

	public WzHeader Header { get; set; }

	public bool LeaveOpen { get; internal set; }

	public WzBinaryWriter(Stream output, byte[] WzIv)
		: this(output, WzIv, leaveOpen: false)
	{
		Hash = 0u;
	}

	public WzBinaryWriter(Stream output, byte[] WzIv, uint Hash)
		: this(output, WzIv, leaveOpen: false)
	{
		this.Hash = Hash;
	}

	public WzBinaryWriter(Stream output, byte[] WzIv, bool leaveOpen)
		: base(output)
	{
		WzKey = WzKeyGenerator.GenerateWzKey(WzIv);
		StringCache = new Dictionary<string, int>();
		LeaveOpen = leaveOpen;
	}

	public void WriteStringValue(string str, int withoutOffset, int withOffset)
	{
		if (str.Length > 4 && StringCache.ContainsKey(str))
		{
			Write((byte)withOffset);
			Write(StringCache[str]);
			return;
		}
		Write((byte)withoutOffset);
		int value = (int)BaseStream.Position;
		Write(str);
		if (!StringCache.ContainsKey(str))
		{
			StringCache[str] = value;
		}
	}

	public bool WriteWzObjectValue(string stringObjectValue, WzDirectoryType type)
	{
		string key = $"{(byte)type}_{stringObjectValue}";
		if (stringObjectValue.Length > 4 && StringCache.ContainsKey(key))
		{
			Write((byte)2);
			Write(StringCache[key]);
			return true;
		}
		int value = (int)(BaseStream.Position - Header.FStart);
		Write((byte)type);
		Write(stringObjectValue);
		if (!StringCache.ContainsKey(key))
		{
			StringCache[key] = value;
		}
		return false;
	}

	public override void Write(string value)
	{
		if (value.Length == 0)
		{
			Write((byte)0);
		}
		else if (value.Any((char c) => c > '\u007f'))
		{
			WriteUnicodeString(value);
		}
		else
		{
			WriteAsciiString(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteUnicodeString(string value)
	{
		if (value.Length >= 127)
		{
			Write(sbyte.MaxValue);
			Write(value.Length);
		}
		else
		{
			Write((sbyte)value.Length);
		}
		ushort num = 43690;
		int num2 = 0;
		foreach (char c in value)
		{
			ushort num3 = c;
			num3 ^= (ushort)((WzKey[num2 * 2 + 1] << 8) + WzKey[num2 * 2]);
			num3 ^= num;
			num++;
			Write(num3);
			num2++;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteAsciiString(string value)
	{
		if (value.Length > 127)
		{
			Write(sbyte.MinValue);
			Write(value.Length);
		}
		else
		{
			Write((sbyte)(-value.Length));
		}
		byte b = 170;
		int num = 0;
		foreach (char c in value)
		{
			byte b2 = (byte)c;
			b2 ^= WzKey[num];
			b2 ^= b;
			b++;
			Write(b2);
			num++;
		}
	}

	public char[] EncryptString(string stringToEncrypt)
	{
		return stringToEncrypt.Select((char c, int i) => (char)(c ^ ((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]))).ToArray();
	}

	public char[] EncryptNonUnicodeString(string stringToEncrypt)
	{
		return stringToEncrypt.Select((char c, int i) => (char)(c ^ WzKey[i])).ToArray();
	}

	public void WriteNullTerminatedString(string value)
	{
		Write((ReadOnlySpan<char>)value);
		Write((byte)0);
	}

	public void WriteCompressedInt(int value)
	{
		if (value > 127 || value <= -128)
		{
			Write(sbyte.MinValue);
			Write(value);
		}
		else
		{
			Write((sbyte)value);
		}
	}

	public void WriteCompressedLong(long value)
	{
		if (value > 127 || value <= -128)
		{
			Write(sbyte.MinValue);
			Write(value);
		}
		else
		{
			Write((sbyte)value);
		}
	}

	public void WriteOffset(long value)
	{
		uint num = (uint)BaseStream.Position;
		num = (num - Header.FStart) ^ 0xFFFFFFFFu;
		num *= Hash;
		num -= WzAESConstant.WZ_OffsetConstant;
		num = ByteUtils.RotateLeft(num, (byte)(num & 0x1F));
		uint value2 = num ^ (uint)((int)value - (int)(Header.FStart * 2));
		Write(value2);
	}

	public override void Close()
	{
		if (!LeaveOpen)
		{
			base.Close();
		}
	}
}
