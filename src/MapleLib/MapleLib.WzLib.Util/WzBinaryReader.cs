using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MapleLib.WzLib.Util;

public sealed class WzBinaryReader : BinaryReader
{
	private readonly long startOffset;

	private readonly ArrayPool<byte> s_bytePool = ArrayPool<byte>.Shared;

	private readonly ArrayPool<char> s_charPool = ArrayPool<char>.Shared;

	public WzMutableKey WzKey { get; init; }

	public uint Hash { get; set; }

	public WzHeader Header { get; set; }

	public WzBinaryReader(Stream input, byte[] WzIv, long startOffset = 0L)
		: base(input)
	{
		WzKey = WzKeyGenerator.GenerateWzKey(WzIv);
		this.startOffset = startOffset;
	}

	public void SetOffsetFromFStartToPosition(int offset)
	{
		BaseStream.Position = Header.FStart + offset - startOffset;
	}

	public void RollbackStreamPosition(int byOffset)
	{
		if (BaseStream.Position - startOffset < byOffset)
		{
			throw new Exception("Cant rollback stream position below 0");
		}
		BaseStream.Position -= byOffset;
	}

	public string ReadStringAtOffset(long Offset)
	{
		return ReadStringAtOffset(Offset, readByte: false);
	}

	public string ReadStringAtOffset(long Offset, bool readByte)
	{
		long position = BaseStream.Position;
		BaseStream.Position = Offset - startOffset;
		if (readByte)
		{
			ReadByte();
		}
		string result = ReadString();
		BaseStream.Position = position;
		return result;
	}

	public override string ReadString()
	{
		sbyte b = base.ReadSByte();
		if (b == 0)
		{
			return string.Empty;
		}
		int num;
		if (b <= 0)
		{
			num = ((b == sbyte.MinValue) ? ReadInt32() : (-b));
		}
		else
		{
			num = ((b == sbyte.MaxValue) ? ReadInt32() : b);
		}
		if (num <= 0)
		{
			return string.Empty;
		}
		if (b > 0)
		{
			return DecodeUnicode(num);
		}
		return DecodeAscii(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private string DecodeUnicode(int length)
	{
		char[] array = null;
		try
		{
			Span<char> span = ((length > 10240) ? (array = s_charPool.Rent(length)).AsSpan(0, length) : stackalloc char[length]);
			Span<char> span2 = span;
			ushort num = 43690;
			ref char reference = ref MemoryMarshal.GetReference(span2);
			for (int i = 0; i < length; i++)
			{
				ushort num2 = ReadUInt16();
				num2 ^= num;
				num2 ^= (ushort)((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]);
				Unsafe.Add(ref reference, i) = (char)num2;
				num++;
			}
			return new string(span2);
		}
		finally
		{
			if (array != null)
			{
				s_charPool.Return(array);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private string DecodeAscii(int length)
	{
		byte[] array = null;
		try
		{
			Span<byte> span = ((length > 10240) ? (array = s_bytePool.Rent(length)).AsSpan(0, length) : stackalloc byte[length]);
			Span<byte> span2 = span;
			byte b = 170;
			ref byte reference = ref MemoryMarshal.GetReference(span2);
			for (int i = 0; i < length; i++)
			{
				byte b2 = ReadByte();
				b2 ^= b;
				b2 ^= WzKey[i];
				Unsafe.Add(ref reference, i) = b2;
				b++;
			}
			return Encoding.ASCII.GetString(span2);
		}
		finally
		{
			if (array != null)
			{
				s_bytePool.Return(array);
			}
		}
	}

	public string ReadString(int length)
	{
		byte[] array = null;
		try
		{
			Span<byte> span = ((length > 10240) ? (array = s_bytePool.Rent(length)).AsSpan(0, length) : stackalloc byte[length]);
			Span<byte> span2 = span;
			BaseStream.Read(span2);
			return Encoding.ASCII.GetString(span2);
		}
		finally
		{
			if (array != null)
			{
				s_bytePool.Return(array);
			}
		}
	}

	public string ReadNullTerminatedString()
	{
		byte[] array = null;
		try
		{
			Span<byte> span = stackalloc byte[256];
			int num = 0;
			byte b;
			while ((b = ReadByte()) != 0)
			{
				if (num == span.Length)
				{
					if (array == null)
					{
						array = s_bytePool.Rent(span.Length * 2);
						span.CopyTo(array);
					}
					else
					{
						byte[] array2 = s_bytePool.Rent(array.Length * 2);
						array.AsSpan(0, num).CopyTo(array2);
						s_bytePool.Return(array);
						array = array2;
					}
					span = array;
				}
				span[num++] = b;
			}
			return Encoding.UTF8.GetString(span.Slice(0, num));
		}
		finally
		{
			if (array != null)
			{
				s_bytePool.Return(array);
			}
		}
	}

	public int ReadCompressedInt()
	{
		sbyte b = base.ReadSByte();
		if (b == sbyte.MinValue)
		{
			return ReadInt32();
		}
		return b;
	}

	public long ReadLong()
	{
		sbyte b = base.ReadSByte();
		if (b == sbyte.MinValue)
		{
			return ReadInt64();
		}
		return b;
	}

	public long Available()
	{
		return BaseStream.Length - BaseStream.Position;
	}

	public long ReadOffset()
	{
		uint num = (uint)BaseStream.Position;
		num = (num - Header.FStart) ^ 0xFFFFFFFFu;
		num *= Hash;
		num -= WzAESConstant.WZ_OffsetConstant;
		num = WzTool.RotateLeft(num, (byte)(num & 0x1F));
		uint num2 = ReadUInt32();
		num ^= num2;
		num += Header.FStart * 2;
		return num + startOffset;
	}

	public string DecryptString(ReadOnlySpan<char> stringToDecrypt)
	{
		char[] array = null;
		try
		{
			Span<char> span = ((stringToDecrypt.Length > 10240) ? (array = s_charPool.Rent(stringToDecrypt.Length)).AsSpan(0, stringToDecrypt.Length) : stackalloc char[stringToDecrypt.Length]);
			Span<char> span2 = span;
			ref char reference = ref MemoryMarshal.GetReference(span2);
			ref char reference2 = ref MemoryMarshal.GetReference(stringToDecrypt);
			for (int i = 0; i < stringToDecrypt.Length; i++)
			{
				Unsafe.Add(ref reference, i) = (char)(Unsafe.Add(ref reference2, i) ^ (ushort)((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]));
			}
			return new string(span2);
		}
		finally
		{
			if (array != null)
			{
				s_charPool.Return(array);
			}
		}
	}

	public string DecryptNonUnicodeString(ReadOnlySpan<char> stringToDecrypt)
	{
		char[] array = null;
		try
		{
			Span<char> span = ((stringToDecrypt.Length > 10240) ? (array = s_charPool.Rent(stringToDecrypt.Length)).AsSpan(0, stringToDecrypt.Length) : stackalloc char[stringToDecrypt.Length]);
			Span<char> span2 = span;
			ref char reference = ref MemoryMarshal.GetReference(span2);
			ref char reference2 = ref MemoryMarshal.GetReference(stringToDecrypt);
			for (int i = 0; i < stringToDecrypt.Length; i++)
			{
				Unsafe.Add(ref reference, i) = (char)(Unsafe.Add(ref reference2, i) ^ WzKey[i]);
			}
			return new string(span2);
		}
		finally
		{
			if (array != null)
			{
				s_charPool.Return(array);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadStringBlock(long offset)
	{
		byte b = ReadByte();
		if (1 == 0)
		{
		}
		if ((uint)b <= 1u)
		{
			if (b == 0)
			{
				goto IL_0028;
			}
			if (b == 1)
			{
				goto IL_0031;
			}
		}
		else
		{
			if (b == 27)
			{
				goto IL_0031;
			}
			if (b == 115)
			{
				goto IL_0028;
			}
		}
		string result = string.Empty;
		goto IL_004b;
		IL_0028:
		result = ReadString();
		goto IL_004b;
		IL_0031:
		result = ReadStringAtOffset(offset + ReadInt32());
		goto IL_004b;
		IL_004b:
		if (1 == 0)
		{
		}
		return result;
	}

	public WzBinaryReader CreateReaderForSection(long start, int length)
	{
		if (start < 0 || start >= BaseStream.Length)
		{
			throw new ArgumentOutOfRangeException("start");
		}
		if (length <= 0 || start + length > BaseStream.Length)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		byte[] buffer = new byte[length];
		lock (this)
		{
			long position = BaseStream.Position;
			BaseStream.Seek(start, SeekOrigin.Begin);
			BaseStream.Read(buffer, 0, length);
			BaseStream.Position = position;
		}
		MemoryStream input = new MemoryStream(buffer);
		return new WzBinaryReader(input, WzKey.GetKeys(), start)
		{
			WzKey = WzKey,
			Hash = Hash,
			Header = Header
		};
	}

	public void PrintHexBytes(int numberOfBytes)
	{
		byte[] array = null;
		try
		{
			Span<byte> span = ((numberOfBytes > 10240) ? (array = s_bytePool.Rent(numberOfBytes)).AsSpan(0, numberOfBytes) : stackalloc byte[numberOfBytes]);
			Span<byte> buffer = span;
			BaseStream.Read(buffer);
			BaseStream.Position -= numberOfBytes;
		}
		finally
		{
			if (array != null)
			{
				s_bytePool.Return(array);
			}
		}
	}

	public override void Close()
	{
		base.Close();
	}
}
