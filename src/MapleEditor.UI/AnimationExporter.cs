using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MapleEditor.UI;

public static class AnimationExporter
{
	private static readonly uint[] CrcTable = BuildCrcTable();

	public static void SaveApng(string path, IReadOnlyList<Bitmap> frames, IReadOnlyList<int> delays)
	{
		if (frames == null || frames.Count == 0)
		{
			throw new InvalidOperationException("The animation has no frames.");
		}
		int width = frames[0].Width;
		int height = frames[0].Height;
		foreach (Bitmap frame in frames)
		{
			if (frame.Width != width || frame.Height != height)
			{
				throw new InvalidOperationException("All APNG frames must have the same size.");
			}
		}
		using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
		stream.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
		byte[] ihdr = new byte[13];
		WriteInt32BigEndian(ihdr, 0, width);
		WriteInt32BigEndian(ihdr, 4, height);
		ihdr[8] = 8;
		ihdr[9] = 6;
		WriteChunk(stream, "IHDR", ihdr);
		byte[] actl = new byte[8];
		WriteInt32BigEndian(actl, 0, frames.Count);
		WriteInt32BigEndian(actl, 4, 0);
		WriteChunk(stream, "acTL", actl);
		uint sequence = 0;
		for (int i = 0; i < frames.Count; i++)
		{
			int delay = ((delays != null && i < delays.Count) ? delays[i] : 100);
			delay = Math.Max(1, Math.Min(65535, delay));
			byte[] fctl = new byte[26];
			WriteInt32BigEndian(fctl, 0, (int)sequence++);
			WriteInt32BigEndian(fctl, 4, width);
			WriteInt32BigEndian(fctl, 8, height);
			WriteInt32BigEndian(fctl, 12, 0);
			WriteInt32BigEndian(fctl, 16, 0);
			fctl[20] = (byte)(delay & 0xFF);
			fctl[21] = (byte)((delay >> 8) & 0xFF);
			fctl[22] = 0xE8;
			fctl[23] = 0x03;
			fctl[24] = 1;
			fctl[25] = 0;
			WriteChunk(stream, "fcTL", fctl);
			byte[] data = CompressFrame(frames[i]);
			if (i == 0)
			{
				WriteChunk(stream, "IDAT", data);
			}
			else
			{
				byte[] fdat = new byte[data.Length + 4];
				WriteInt32BigEndian(fdat, 0, (int)sequence++);
				Buffer.BlockCopy(data, 0, fdat, 4, data.Length);
				WriteChunk(stream, "fdAT", fdat);
			}
		}
		WriteChunk(stream, "IEND", Array.Empty<byte>());
	}

	public static void SaveGif(string path, IReadOnlyList<Bitmap> frames, IReadOnlyList<int> delays)
	{
		if (frames == null || frames.Count == 0)
		{
			throw new InvalidOperationException("The animation has no frames.");
		}
		int width = frames[0].Width;
		int height = frames[0].Height;
		foreach (Bitmap frame in frames)
		{
			if (frame.Width != width || frame.Height != height)
			{
				throw new InvalidOperationException("All GIF frames must have the same size.");
			}
		}
		Palette palette = BuildPalette(frames);
		using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
		WriteAscii(stream, "GIF89a");
		WriteUInt16(stream, width);
		WriteUInt16(stream, height);
		stream.WriteByte(0xF7);
		stream.WriteByte(0);
		stream.WriteByte(0);
		stream.Write(palette.Table, 0, palette.Table.Length);
		stream.Write(new byte[19] { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 }, 0, 19);
		for (int i = 0; i < frames.Count; i++)
		{
			int delay = ((delays != null && i < delays.Count) ? delays[i] : 100);
			int centiseconds = Math.Max(1, Math.Min(65535, delay / 10));
			stream.Write(new byte[8] { 33, 249, 4, 9, (byte)(centiseconds & 0xFF), (byte)((centiseconds >> 8) & 0xFF), 0, 0 }, 0, 8);
			stream.WriteByte(44);
			WriteUInt16(stream, 0);
			WriteUInt16(stream, 0);
			WriteUInt16(stream, width);
			WriteUInt16(stream, height);
			stream.WriteByte(0);
			byte[] indices = BuildIndices(frames[i], palette);
			byte[] lzw = EncodeGifLzw(indices, 8);
			stream.WriteByte(8);
			for (int offset = 0; offset < lzw.Length; offset += 255)
			{
				int count = Math.Min(255, lzw.Length - offset);
				stream.WriteByte((byte)count);
				stream.Write(lzw, offset, count);
			}
			stream.WriteByte(0);
		}
		stream.WriteByte(59);
	}

	private sealed class Palette
	{
		public byte[] Table { get; }

		public Dictionary<int, byte> IndexByKey { get; }

		public bool UseThreeTwoTwo { get; }

		public int Mask { get; }

		public Palette(byte[] table, Dictionary<int, byte> indexByKey, bool useThreeTwoTwo, int mask)
		{
			Table = table;
			IndexByKey = indexByKey;
			UseThreeTwoTwo = useThreeTwoTwo;
			Mask = mask;
		}

		public int GetKey(Color color)
		{
			if (UseThreeTwoTwo)
			{
				return ((color.R >> 5) << 5) | ((color.G >> 6) << 3) | (color.B >> 6);
			}
			return ((color.R & Mask) << 16) | ((color.G & Mask) << 8) | (color.B & Mask);
		}
	}

	private static Palette BuildPalette(IReadOnlyList<Bitmap> frames)
	{
		int[] masks = new int[3] { 0xF8, 0xF0, 0xE0 };
		int mask = masks[0];
		bool useThreeTwoTwo = false;
		HashSet<int> keys = CollectKeys(frames, mask);
		for (int i = 1; i < masks.Length && keys.Count > 255; i++)
		{
			mask = masks[i];
			keys = CollectKeys(frames, mask);
		}
		if (keys.Count > 255)
		{
			useThreeTwoTwo = true;
			keys.Clear();
			foreach (Bitmap frame in frames)
			{
				foreach (Color color in EnumeratePixels(frame))
				{
					if (color.A >= 128)
					{
						keys.Add(((color.R >> 5) << 5) | ((color.G >> 6) << 3) | (color.B >> 6));
					}
				}
			}
		}
		byte[] table = new byte[768];
		Dictionary<int, byte> indexByKey = new Dictionary<int, byte>();
		int index = 1;
		foreach (int key in keys.OrderBy((int value) => value))
		{
			if (index >= 256)
			{
				break;
			}
			indexByKey[key] = (byte)index;
			int red;
			int green;
			int blue;
			if (useThreeTwoTwo)
			{
				red = ((key >> 5) & 7) * 255 / 7;
				green = ((key >> 3) & 3) * 255 / 3;
				blue = (key & 3) * 255 / 3;
			}
			else
			{
				red = (key >> 16) & 0xFF;
				green = (key >> 8) & 0xFF;
				blue = key & 0xFF;
			}
			table[index * 3] = (byte)red;
			table[index * 3 + 1] = (byte)green;
			table[index * 3 + 2] = (byte)blue;
			index++;
		}
		return new Palette(table, indexByKey, useThreeTwoTwo, mask);
	}

	private static HashSet<int> CollectKeys(IReadOnlyList<Bitmap> frames, int mask)
	{
		HashSet<int> keys = new HashSet<int>();
		foreach (Bitmap frame in frames)
		{
			foreach (Color color in EnumeratePixels(frame))
			{
				if (color.A >= 128)
				{
					keys.Add(((color.R & mask) << 16) | ((color.G & mask) << 8) | (color.B & mask));
				}
			}
		}
		return keys;
	}

	private static byte[] BuildIndices(Bitmap frame, Palette palette)
	{
		byte[] indices = new byte[frame.Width * frame.Height];
		int offset = 0;
		foreach (Color color in EnumeratePixels(frame))
		{
			if (color.A < 128)
			{
				indices[offset++] = 0;
				continue;
			}
			int key = palette.GetKey(color);
			if (palette.IndexByKey.TryGetValue(key, out byte index))
			{
				indices[offset++] = index;
			}
			else
			{
				indices[offset++] = 0;
			}
		}
		return indices;
	}

	private static IEnumerable<Color> EnumeratePixels(Bitmap frame)
	{
		using Bitmap normalized = (frame.PixelFormat == PixelFormat.Format32bppArgb) ? null : frame.Clone(new Rectangle(0, 0, frame.Width, frame.Height), PixelFormat.Format32bppArgb);
		Bitmap source = normalized ?? frame;
		BitmapData data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] buffer = new byte[data.Stride * source.Height];
			Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
			for (int y = 0; y < source.Height; y++)
			{
				int row = y * data.Stride;
				for (int x = 0; x < source.Width; x++)
				{
					int offset = row + x * 4;
					yield return Color.FromArgb(buffer[offset + 3], buffer[offset + 2], buffer[offset + 1], buffer[offset]);
				}
			}
		}
		finally
		{
			source.UnlockBits(data);
		}
	}

	private static byte[] CompressFrame(Bitmap frame)
	{
		int width = frame.Width;
		int height = frame.Height;
		byte[] raw = new byte[(width * 4 + 1) * height];
		using Bitmap normalized = (frame.PixelFormat == PixelFormat.Format32bppArgb) ? null : frame.Clone(new Rectangle(0, 0, width, height), PixelFormat.Format32bppArgb);
		Bitmap source = normalized ?? frame;
		BitmapData data = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] buffer = new byte[data.Stride * height];
			Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
			for (int y = 0; y < height; y++)
			{
				int rowOffset = y * (width * 4 + 1);
				raw[rowOffset] = 0;
				int sourceRow = y * data.Stride;
				for (int x = 0; x < width; x++)
				{
					int sourceOffset = sourceRow + x * 4;
					int targetOffset = rowOffset + 1 + x * 4;
					raw[targetOffset] = buffer[sourceOffset + 2];
					raw[targetOffset + 1] = buffer[sourceOffset + 1];
					raw[targetOffset + 2] = buffer[sourceOffset];
					raw[targetOffset + 3] = buffer[sourceOffset + 3];
				}
			}
		}
		finally
		{
			source.UnlockBits(data);
		}
		using MemoryStream output = new MemoryStream();
		using (ZLibStream zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
		{
			zlib.Write(raw, 0, raw.Length);
		}
		return output.ToArray();
	}

	private static byte[] EncodeGifLzw(byte[] indices, int minCodeSize)
	{
		List<byte> output = new List<byte>();
		int clearCode = 1 << minCodeSize;
		int endCode = clearCode + 1;
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		int nextCode = endCode + 1;
		int codeSize = minCodeSize + 1;
		int bitBuffer = 0;
		int bitCount = 0;
		void WriteCode(int code)
		{
			bitBuffer |= code << bitCount;
			bitCount += codeSize;
			while (bitCount >= 8)
			{
				output.Add((byte)(bitBuffer & 0xFF));
				bitBuffer >>= 8;
				bitCount -= 8;
			}
		}
		WriteCode(clearCode);
		int prefix = indices[0];
		for (int i = 1; i < indices.Length; i++)
		{
			int value = indices[i];
			int key = (prefix << 8) | value;
			if (dictionary.TryGetValue(key, out int code))
			{
				prefix = code;
				continue;
			}
			WriteCode(prefix);
			if (nextCode < 4096)
			{
				dictionary[key] = nextCode++;
				if (nextCode == (1 << codeSize) && codeSize < 12)
				{
					codeSize++;
				}
			}
			else
			{
				WriteCode(clearCode);
				dictionary.Clear();
				nextCode = endCode + 1;
				codeSize = minCodeSize + 1;
			}
			prefix = value;
		}
		WriteCode(prefix);
		WriteCode(endCode);
		if (bitCount > 0)
		{
			output.Add((byte)(bitBuffer & 0xFF));
		}
		return output.ToArray();
	}

	private static void WriteChunk(Stream stream, string type, byte[] data)
	{
		byte[] length = new byte[4];
		WriteInt32BigEndian(length, 0, data.Length);
		stream.Write(length, 0, 4);
		byte[] typeBytes = Encoding.ASCII.GetBytes(type);
		stream.Write(typeBytes, 0, 4);
		stream.Write(data, 0, data.Length);
		byte[] crcInput = new byte[4 + data.Length];
		Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
		Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
		uint crc = ComputeCrc(crcInput);
		byte[] crcBytes = new byte[4];
		WriteInt32BigEndian(crcBytes, 0, (int)crc);
		stream.Write(crcBytes, 0, 4);
	}

	private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
	{
		buffer[offset] = (byte)((value >> 24) & 0xFF);
		buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
		buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
		buffer[offset + 3] = (byte)(value & 0xFF);
	}

	private static void WriteAscii(Stream stream, string text)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(text);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt16(Stream stream, int value)
	{
		stream.WriteByte((byte)(value & 0xFF));
		stream.WriteByte((byte)((value >> 8) & 0xFF));
	}

	private static uint[] BuildCrcTable()
	{
		uint[] table = new uint[256];
		for (uint n = 0; n < 256; n++)
		{
			uint c = n;
			for (int k = 0; k < 8; k++)
			{
				c = (((c & 1) != 0) ? (0xEDB88320u ^ (c >> 1)) : (c >> 1));
			}
			table[n] = c;
		}
		return table;
	}

	private static uint ComputeCrc(byte[] data)
	{
		uint crc = 0xFFFFFFFFu;
		foreach (byte value in data)
		{
			crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
		}
		return ~crc;
	}
}
