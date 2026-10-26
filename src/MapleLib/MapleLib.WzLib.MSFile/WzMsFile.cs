using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.MSFile;

public class WzMsFile : IDisposable
{
	private readonly string originalFileName;

	private readonly string msFilePath;

	private bool leaveOpen;

	public Stream BaseStream { get; private set; }

	public WzMsHeader Header { get; private set; }

	public List<WzMsEntry> Entries { get; private set; }

	public WzMsFile(Stream baseStream, string originalFileName, string msFilePath, bool leaveOpen = false, bool isSavingFile = false)
	{
		ArgumentNullException.ThrowIfNull(baseStream, "baseStream");
		ArgumentNullException.ThrowIfNull(originalFileName, "originalFileName");
		if (string.IsNullOrWhiteSpace(msFilePath))
		{
			throw new ArgumentException("Path cannot be null or empty.", "msFilePath");
		}
		BaseStream = baseStream;
		this.leaveOpen = leaveOpen;
		this.msFilePath = msFilePath;
		this.originalFileName = originalFileName;
		Entries = new List<WzMsEntry>();
		if (!isSavingFile)
		{
			ReadHeader(originalFileName);
		}
	}

	private byte[] DeriveSnowKey(ReadOnlySpan<char> fileNameWithSalt, bool isEntryKey = false)
	{
		Span<byte> span = stackalloc byte[16];
		if (!isEntryKey)
		{
			for (int i = 0; i < span.Length; i++)
			{
				span[i] = (byte)(fileNameWithSalt[i % fileNameWithSalt.Length] + i);
			}
		}
		else
		{
			for (int j = 0; j < span.Length; j++)
			{
				span[j] = (byte)(j + (j % 3 + 2) * fileNameWithSalt[fileNameWithSalt.Length - 1 - j % fileNameWithSalt.Length]);
			}
		}
		return span.ToArray();
	}

	private void ReadHeader(string fullFileName)
	{
		string text = Path.GetFileName(fullFileName).ToLower();
		BaseStream.Position = 0L;
		using BinaryReader binaryReader = new BinaryReader(BaseStream, Encoding.ASCII, leaveOpen: true);
		int count = text.Sum((char c) => c) % 312 + 30;
		byte[] array = binaryReader.ReadBytes(count);
		int num = binaryReader.ReadInt32();
		int num2 = (byte)num ^ array[0];
		byte[] array2 = binaryReader.ReadBytes(num2 * 2);
		char[] array3 = new char[num2];
		for (int num3 = 0; num3 < num2; num3++)
		{
			array3[num3] = (char)(array[num3] ^ array2[num3 * 2]);
		}
		string text2 = new string(array3);
		string text3 = text + text2;
		byte[] key = DeriveSnowKey(text3);
		long position = BaseStream.Position;
		using Snow2CryptoTransform transform = new Snow2CryptoTransform(key, null, encrypting: false);
		using CryptoStream input = new CryptoStream(BaseStream, transform, CryptoStreamMode.Read, leaveOpen: true);
		using BinaryReader snowReader = new BinaryReader(input);
		(int EntryCount, int CalculatedHash) tuple = ReadAndValidateHeader(snowReader, num, array2, num2);
		int item = tuple.EntryCount;
		int item2 = tuple.CalculatedHash;
		int num4 = text.Sum((char v) => v * 3) % 212 + 33;
		long entryStartPosition = position + 9 + num4;
		WzMsHeader header = new WzMsHeader(fullFileName, text2, text3, item2, 2, item, position, entryStartPosition);
		Header = header;
	}

	private static (int EntryCount, int CalculatedHash) ReadAndValidateHeader(BinaryReader snowReader, int hashedSaltLen, byte[] saltBytes, int saltLen)
	{
		int num = snowReader.ReadInt32();
		byte b = snowReader.ReadByte();
		int num2 = snowReader.ReadInt32();
		if (b != 2)
		{
			throw new Exception($"Unsupported version: expected {2}, got {b}");
		}
		int num3 = hashedSaltLen + b + num2;
		ReadOnlySpan<ushort> readOnlySpan = MemoryMarshal.Cast<byte, ushort>(saltBytes.AsSpan(0, saltLen * 2));
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			num3 += readOnlySpan[i];
		}
		if (num != num3)
		{
			throw new Exception($"Header hash mismatch: expected {num3}, got {num}");
		}
		return (EntryCount: num2, CalculatedHash: num3);
	}

	public void ReadEntries()
	{
		if (Header == null || Header.EntryCount == 0 || Header.EntryCount == Entries.Count)
		{
			return;
		}
		Entries.Clear();
		int entryCount = Header.EntryCount;
		if (Entries.Capacity < entryCount)
		{
			Entries.Capacity = entryCount;
		}
		string fileNameWithSalt = Header.FileNameWithSalt;
		byte[] key = DeriveSnowKey(fileNameWithSalt, isEntryKey: true);
		using Snow2CryptoTransform transform = new Snow2CryptoTransform(key, null, encrypting: false);
		BaseStream.Position = Header.EntryStartPosition;
		CryptoStream input = new CryptoStream(BaseStream, transform, CryptoStreamMode.Read);
		BinaryReader binaryReader = new BinaryReader(input, Encoding.Unicode, leaveOpen: true);
		for (int i = 0; i < entryCount; i++)
		{
			int count = binaryReader.ReadInt32();
			string name = new string(binaryReader.ReadChars(count));
			int checkSum = binaryReader.ReadInt32();
			int flags = binaryReader.ReadInt32();
			int startPos = binaryReader.ReadInt32();
			int size = binaryReader.ReadInt32();
			int sizeAligned = binaryReader.ReadInt32();
			int unk = binaryReader.ReadInt32();
			int unk2 = binaryReader.ReadInt32();
			byte[] entryKey = binaryReader.ReadBytes(16);
			WzMsEntry item = new WzMsEntry(name, checkSum, flags, startPos, size, sizeAligned, unk, unk2, entryKey);
			Entries.Add(item);
		}
		long position = BaseStream.Position;
		position = AlignToPage(position);
		Header.DataStartPosition = position;
		foreach (WzMsEntry entry in Entries)
		{
			entry.StartPos = position + entry.StartPos * 1024;
		}
	}

	private static long AlignToPage(long pos)
	{
		return (pos + 1023) & -1024;
	}

	public WzFile LoadAsWzFile()
	{
		ReadEntries();
		WzMapleVersion wzMapleVersion = WzMapleVersion.BMS;
		WzFile wzFile = new WzFile(0, wzMapleVersion)
		{
			Name = Path.GetFileName(msFilePath.Replace(".ms", ".wz")),
			path = msFilePath
		};
		WzDirectory wzDirectory = wzFile.WzDirectory;
		wzDirectory.wzFile = wzFile;
		foreach (WzMsEntry entry in Entries)
		{
			if (entry.Data == null)
			{
				BaseStream.Position = entry.StartPos;
				byte[] array = new byte[entry.Size];
				BaseStream.ReadExactly(array);
				entry.Data = array;
			}
			Stream input = DecryptData(entry);
			WzBinaryReader reader = new WzBinaryReader(input, WzTool.GetIvByMapleVersion(wzMapleVersion), 0L);
			int num = entry.Name.LastIndexOf('/');
			string name = ((num >= 0) ? entry.Name.Substring(num + 1) : entry.Name);
			WzImage wzImage = new WzImage(name, reader);
			wzImage.ParseImage();
			wzDirectory.AddImage(wzImage);
		}
		return wzFile;
	}

	private void CreateHeader(WzFile wzFile, byte[] iv)
	{
		Random random = new Random();
		Entries = new List<WzMsEntry>();
		foreach (WzImage wzImage in wzFile.WzDirectory.WzImages)
		{
			using MemoryStream memoryStream = new MemoryStream();
			using WzBinaryWriter writer = new WzBinaryWriter(memoryStream, iv);
			wzImage.SaveImage(writer, bIsWzUserKeyDefault: true, forceReadFromData: true);
			byte[] data = memoryStream.ToArray();
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(wzFile.Name);
			string name = fileNameWithoutExtension + "/" + wzImage.Name;
			byte[] array = new byte[16];
			random.NextBytes(array);
			WzMsEntry wzMsEntry = new WzMsEntry(name, 0, 0, 0, 0, 0, 0, 0, array);
			wzMsEntry.Data = data;
			Entries.Add(wzMsEntry);
		}
		string text = Path.GetFileName(originalFileName).ToLower();
		string text2 = GenerateSalt(4, 12);
		string fileNameWithSalt = text + text2;
		Header = new WzMsHeader(originalFileName, text2, fileNameWithSalt, 0, 2, Entries.Count, 0L, 0L);
		Header = new WzMsHeader(originalFileName, text2, fileNameWithSalt, 0, 2, Entries.Count, 0L, 0L);
	}

	private static string GenerateSalt(int minLen, int maxLen)
	{
		int @int = RandomNumberGenerator.GetInt32(minLen, maxLen + 1);
		char[] array = new char[@int];
		byte[] array2 = new byte[@int];
		using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
		randomNumberGenerator.GetBytes(array2);
		for (int i = 0; i < @int; i++)
		{
			array[i] = (char)(33 + array2[i] % 95);
		}
		return new string(array);
	}

	public Stream Save(WzFile wzFile)
	{
		byte[] ivByMapleVersion = WzTool.GetIvByMapleVersion(WzMapleVersion.BMS);
		if (Header != null)
		{
			throw new InvalidOperationException("Cannot save when Header is already present. Create a new WzMsFile instance for saving.");
		}
		CreateHeader(wzFile, ivByMapleVersion);
		if (Entries == null || Entries.Count == 0 || Header == null)
		{
			return BaseStream;
		}
		List<byte[]> list = new List<byte[]>();
		long num = 0L;
		foreach (WzMsEntry entry in Entries)
		{
			if (entry.Data != null)
			{
				byte[] array = EncryptData(entry, entry.Data);
				int num2 = array.Length;
				int num3 = (num2 + 1023) / 1024 * 1024;
				entry.Size = num2;
				entry.SizeAligned = num3;
				entry.StartPos = num;
				num += num3 / 1024;
				list.Add(array);
				entry.RecalculateFields(entry.Flags, (int)entry.StartPos, entry.Unk1);
			}
		}
		BaseStream.Position = 0L;
		BaseStream.SetLength(0L);
		using BinaryWriter binaryWriter = new BinaryWriter(BaseStream, Encoding.ASCII, leaveOpen: true);
		string source = Path.GetFileName(originalFileName).ToLower();
		Random random = new Random();
		int num4 = source.Sum((char c) => c) % 312 + 30;
		byte[] array2 = new byte[num4];
		for (int num5 = 0; num5 < num4; num5++)
		{
			array2[num5] = (byte)random.Next();
		}
		string salt = Header.Salt;
		char[] array3 = salt.ToCharArray();
		int length = salt.Length;
		byte b = array2[0];
		byte b2 = (byte)(length ^ b);
		int num6 = b2;
		byte[] array4 = new byte[length * 2];
		for (int num7 = 0; num7 < length; num7++)
		{
			array4[num7 * 2] = (byte)(array2[num7] ^ (byte)array3[num7]);
			array4[num7 * 2 + 1] = 0;
		}
		ReadOnlySpan<ushort> readOnlySpan = MemoryMarshal.Cast<byte, ushort>(array4);
		int num8 = 0;
		ReadOnlySpan<ushort> readOnlySpan2 = readOnlySpan;
		for (int num9 = 0; num9 < readOnlySpan2.Length; num9++)
		{
			ushort num10 = readOnlySpan2[num9];
			num8 += num10;
		}
		byte version = Header.Version;
		int count = Entries.Count;
		int num11 = num6 + version + count + num8;
		string fileNameWithSalt = Header.FileNameWithSalt;
		binaryWriter.Write(array2);
		binaryWriter.Write(num6);
		binaryWriter.Write(array4);
		long position = BaseStream.Position;
		byte[] key = DeriveSnowKey(fileNameWithSalt);
		using (Snow2CryptoTransform transform = new Snow2CryptoTransform(key, null, encrypting: true))
		{
			using CryptoStream cryptoStream = new CryptoStream(BaseStream, transform, CryptoStreamMode.Write, leaveOpen: true);
			using BinaryWriter binaryWriter2 = new BinaryWriter(cryptoStream);
			binaryWriter2.Write(num11);
			binaryWriter2.Write(version);
			binaryWriter2.Write(count);
			binaryWriter2.Flush();
			cryptoStream.FlushFinalBlock();
		}
		int num12 = source.Select((char v) => v * 3).Sum() % 212 + 33;
		byte[] buffer = new byte[num12];
		random.NextBytes(buffer);
		binaryWriter.Write(buffer);
		byte[] key2 = DeriveSnowKey(fileNameWithSalt, isEntryKey: true);
		using (Snow2CryptoTransform transform2 = new Snow2CryptoTransform(key2, null, encrypting: true))
		{
			using CryptoStream output = new CryptoStream(BaseStream, transform2, CryptoStreamMode.Write, leaveOpen: true);
			using BinaryWriter binaryWriter3 = new BinaryWriter(output, Encoding.Unicode, leaveOpen: true);
			foreach (WzMsEntry entry2 in Entries)
			{
				int length2 = entry2.Name.Length;
				binaryWriter3.Write(length2);
				binaryWriter3.Write(entry2.Name.ToCharArray());
				binaryWriter3.Write(entry2.CheckSum);
				binaryWriter3.Write(entry2.Flags);
				binaryWriter3.Write((int)entry2.StartPos);
				binaryWriter3.Write(entry2.Size);
				binaryWriter3.Write(entry2.SizeAligned);
				binaryWriter3.Write(entry2.Unk1);
				binaryWriter3.Write(entry2.Unk2);
				binaryWriter3.Write(entry2.EntryKey);
			}
		}
		long num13 = BaseStream.Position;
		if ((num13 & 0x3FF) != 0)
		{
			long num14 = 1024 - (num13 & 0x3FF);
			byte[] array5 = new byte[num14];
			BaseStream.Write(array5);
			num13 += num14;
		}
		long num15 = num13;
		for (int num16 = 0; num16 < Entries.Count; num16++)
		{
			WzMsEntry wzMsEntry = Entries[num16];
			if (wzMsEntry.Data != null)
			{
				byte[] array6 = list[num16];
				long num17 = num13 + wzMsEntry.StartPos * 1024;
				if (num17 > BaseStream.Position)
				{
					BaseStream.Position = num17;
				}
				BaseStream.Write(array6);
				int num18 = wzMsEntry.SizeAligned - wzMsEntry.Size;
				if (num18 > 0)
				{
					byte[] array7 = new byte[num18];
					BaseStream.Write(array7);
				}
				long num19 = num17 + wzMsEntry.SizeAligned;
				if (num19 > num15)
				{
					num15 = num19;
				}
			}
		}
		BaseStream.SetLength(num15);
		BaseStream.Position = 0L;
		Header.UpdateHeader(num11, count, position, position + 9 + num12, num13);
		return BaseStream;
	}

	private byte[] DeriveImgKey(WzMsEntry entry)
	{
		uint num = 2166136261u;
		string salt = Header.Salt;
		foreach (char c in salt)
		{
			num = (num ^ c) * 16777619;
		}
		ReadOnlySpan<byte> readOnlySpan = (from c2 in num.ToString()
			select (byte)(c2 - 48)).ToArray().AsSpan();
		Span<byte> span = stackalloc byte[16];
		ReadOnlySpan<char> readOnlySpan2 = entry.Name;
		ReadOnlySpan<byte> readOnlySpan3 = entry.EntryKey;
		for (int num2 = 0; num2 < span.Length; num2++)
		{
			int index = num2 % readOnlySpan.Length;
			int index2 = (readOnlySpan[(num2 + 2) % readOnlySpan.Length] + num2) % readOnlySpan3.Length;
			span[num2] = (byte)(num2 + readOnlySpan2[num2 % readOnlySpan2.Length] * (readOnlySpan[index] % 2 + readOnlySpan3[index2] + (readOnlySpan[(num2 + 1) % readOnlySpan.Length] + num2) % 5));
		}
		return span.ToArray();
	}

	private Stream DecryptData(WzMsEntry entry)
	{
		byte[] key = DeriveImgKey(entry);
		using PartialStream partialStream = new PartialStream(BaseStream, entry.StartPos, entry.SizeAligned, leaveOpen: true);
		byte[] array = new byte[entry.Size];
		Span<byte> buffer = array;
		partialStream.Position = 0L;
		CryptoStream cryptoStream = new CryptoStream(partialStream, new Snow2CryptoTransform(key, null, encrypting: false), CryptoStreamMode.Read);
		CryptoStream cryptoStream2 = new CryptoStream(cryptoStream, new Snow2CryptoTransform(key, null, encrypting: false), CryptoStreamMode.Read);
		int num = Math.Min(buffer.Length, 1024);
		cryptoStream2.ReadExactly(buffer.Slice(0, num));
		buffer = buffer.Slice(num);
		if (buffer.Length > 0)
		{
			cryptoStream.ReadExactly(buffer);
		}
		return new MemoryStream(array);
	}

	private byte[] EncryptData(WzMsEntry entry, byte[] plainData)
	{
		byte[] key = DeriveImgKey(entry);
		MemoryStream memoryStream = new MemoryStream();
		using CryptoStream cryptoStream = new CryptoStream(memoryStream, new Snow2CryptoTransform(key, null, encrypting: true), CryptoStreamMode.Write);
		int count = Math.Min(plainData.Length, 1024);
		using (CryptoStream cryptoStream2 = new CryptoStream(cryptoStream, new Snow2CryptoTransform(key, null, encrypting: true), CryptoStreamMode.Write, leaveOpen: true))
		{
			cryptoStream2.Write(plainData, 0, count);
			cryptoStream2.Flush();
		}
		if (plainData.Length > 1024)
		{
			cryptoStream.Write(plainData, 1024, plainData.Length - 1024);
			cryptoStream.Flush();
		}
		return memoryStream.ToArray();
	}

	public void Close()
	{
		BaseStream?.Dispose();
		BaseStream = null;
	}

	public void Dispose()
	{
		Close();
	}
}
