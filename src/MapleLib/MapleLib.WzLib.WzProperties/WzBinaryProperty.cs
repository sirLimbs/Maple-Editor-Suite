using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;
using NAudio.Wave;

namespace MapleLib.WzLib.WzProperties;

public class WzBinaryProperty : WzExtended
{
	private static readonly byte[] _riff_waveHeader = new byte[28]
	{
		82, 73, 70, 70, 20, 96, 40, 0, 87, 65,
		86, 69, 102, 109, 116, 32, 16, 0, 0, 0,
		1, 0, 1, 0, 34, 86, 0, 0
	};

	public static readonly byte[] soundHeader = new byte[51]
	{
		2, 131, 235, 54, 228, 79, 82, 206, 17, 159,
		83, 0, 32, 175, 11, 167, 112, 139, 235, 54,
		228, 79, 82, 206, 17, 159, 83, 0, 32, 175,
		11, 167, 112, 0, 1, 129, 159, 88, 5, 86,
		195, 206, 17, 191, 1, 0, 170, 0, 85, 89,
		90
	};

	internal string name;

	internal byte[] fileBytes = null;

	internal WzObject parent;

	internal int len_ms;

	internal byte[] header;

	internal WzBinaryReader wzReader;

	internal bool headerEncrypted = false;

	internal long offs;

	internal int soundDataLen;

	internal WaveFormat wavFormat;

	public override object WzValue => GetBytes(saveInMemory: false);

	public override WzObject Parent
	{
		get
		{
			return parent;
		}
		internal set
		{
			parent = value;
		}
	}

	public override string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.Sound;

	public byte[] Header
	{
		get
		{
			return header;
		}
		set
		{
			header = value;
		}
	}

	public int Length
	{
		get
		{
			return len_ms;
		}
		set
		{
			len_ms = value;
		}
	}

	public int Frequency => (wavFormat != null) ? wavFormat.SampleRate : 0;

	public WaveFormat WavFormat
	{
		get
		{
			return wavFormat;
		}
		private set
		{
		}
	}

	public override WzImageProperty DeepClone()
	{
		return new WzBinaryProperty(this);
	}

	public override void SetValue(object value)
	{
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		byte[] bytes = GetBytes(saveInMemory: false);
		writer.WriteStringValue("Sound_DX8", 115, 27);
		writer.Write((byte)0);
		writer.WriteCompressedInt(bytes.Length);
		writer.WriteCompressedInt(len_ms);
		writer.Write(header);
		writer.Write(bytes);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedTag("WzSound", Name));
	}

	public override void Dispose()
	{
		name = null;
		fileBytes = null;
	}

	public WzBinaryProperty(string name, WzBinaryReader reader, bool parseNow)
	{
		this.name = name;
		wzReader = reader;
		reader.BaseStream.Position++;
		soundDataLen = reader.ReadCompressedInt();
		len_ms = reader.ReadCompressedInt();
		long position = reader.BaseStream.Position;
		reader.BaseStream.Position += soundHeader.Length;
		int count = reader.ReadByte();
		reader.BaseStream.Position = position;
		byte[] first = reader.ReadBytes(soundHeader.Length);
		byte[] second = reader.ReadBytes(1);
		byte[] second2 = reader.ReadBytes(count);
		header = first.Concat(second).Concat(second2).ToArray();
		ParseWzSoundPropertyHeader();
		offs = reader.BaseStream.Position;
		if (parseNow)
		{
			fileBytes = reader.ReadBytes(soundDataLen);
		}
		else
		{
			reader.BaseStream.Position += soundDataLen;
		}
	}

	public WzBinaryProperty(WzBinaryProperty otherProperty)
	{
		name = otherProperty.name;
		wavFormat = otherProperty.wavFormat;
		len_ms = otherProperty.len_ms;
		soundDataLen = otherProperty.soundDataLen;
		offs = otherProperty.offs;
		if (otherProperty.header == null)
		{
			otherProperty.ParseWzSoundPropertyHeader();
		}
		header = new byte[otherProperty.header.Length];
		Array.Copy(otherProperty.header, header, otherProperty.header.Length);
		if (otherProperty.fileBytes == null)
		{
			fileBytes = otherProperty.GetBytes(saveInMemory: false);
		}
		else
		{
			fileBytes = new byte[otherProperty.fileBytes.Length];
			Array.Copy(otherProperty.fileBytes, fileBytes, otherProperty.fileBytes.Length);
		}
		headerEncrypted = otherProperty.headerEncrypted;
	}

	public WzBinaryProperty(string name, int len_ms, byte[] headerClone, byte[] data)
	{
		this.name = name;
		this.len_ms = len_ms;
		header = new byte[headerClone.Length];
		Array.Copy(headerClone, header, headerClone.Length);
		fileBytes = new byte[data.Length];
		Array.Copy(data, fileBytes, data.Length);
		ParseWzSoundPropertyHeader();
	}

	public WzBinaryProperty(string name, string file)
	{
		this.name = name;
		Mp3FileReader mp3FileReader = new Mp3FileReader(file);
		wavFormat = mp3FileReader.Mp3WaveFormat;
		len_ms = (int)((double)mp3FileReader.Length * 1000.0 / (double)mp3FileReader.WaveFormat.AverageBytesPerSecond);
		RebuildHeader();
		mp3FileReader.Dispose();
		fileBytes = File.ReadAllBytes(file);
	}

	private void RebuildHeader()
	{
		using BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
		binaryWriter.Write(soundHeader);
		byte[] array = StructToBytes(wavFormat);
		if (headerEncrypted)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] ^= wzReader.WzKey[i];
			}
		}
		binaryWriter.Write((byte)array.Length);
		binaryWriter.Write(array, 0, array.Length);
		header = ((MemoryStream)binaryWriter.BaseStream).ToArray();
	}

	private static byte[] StructToBytes<T>(T obj)
	{
		byte[] array = new byte[Marshal.SizeOf(obj)];
		GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
		try
		{
			Marshal.StructureToPtr(obj, gCHandle.AddrOfPinnedObject(), fDeleteOld: false);
			return array;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	private static T BytesToStruct<T>(byte[] data) where T : new()
	{
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		try
		{
			return Marshal.PtrToStructure<T>(gCHandle.AddrOfPinnedObject());
		}
		finally
		{
			gCHandle.Free();
		}
	}

	private static T BytesToStructConstructorless<T>(byte[] data) where T : class
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		int num = Marshal.SizeOf(typeof(T));
		if (data.Length < num)
		{
			throw new ArgumentException($"The byte array must be at least {num} bytes long to contain the entire object.", "data");
		}
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		try
		{
			T val = (T)FormatterServices.GetUninitializedObject(typeof(T));
			Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), val);
			return val;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	private void ParseWzSoundPropertyHeader()
	{
		byte[] array = new byte[header.Length - soundHeader.Length - 1];
		Buffer.BlockCopy(header, soundHeader.Length + 1, array, 0, array.Length);
		if (array.Length < Marshal.SizeOf<WaveFormat>())
		{
			return;
		}
		WaveFormat waveFormat = BytesToStruct<WaveFormat>(array);
		if (Marshal.SizeOf<WaveFormat>() + waveFormat.ExtraSize != array.Length)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] ^= wzReader.WzKey[i];
			}
			waveFormat = BytesToStruct<WaveFormat>(array);
			if (Marshal.SizeOf<WaveFormat>() + waveFormat.ExtraSize != array.Length)
			{
				ErrorLogger.Log(ErrorLevel.Critical, "parse sound header failed");
				return;
			}
			headerEncrypted = true;
		}
		if (waveFormat.Encoding == WaveFormatEncoding.MpegLayer3 && array.Length >= Marshal.SizeOf<Mp3WaveFormat>())
		{
			wavFormat = BytesToStructConstructorless<Mp3WaveFormat>(array);
		}
		else if (waveFormat.Encoding == WaveFormatEncoding.Pcm)
		{
			wavFormat = waveFormat;
		}
		else
		{
			ErrorLogger.Log(ErrorLevel.MissingFeature, $"Unknown wave encoding {waveFormat.Encoding.ToString()}");
		}
	}

	public byte[] GetBytesForWAVPlayback()
	{
		byte[] bytes = GetBytes(saveInMemory: false);
		byte[] array = new byte[_riff_waveHeader.Length + header.Length + bytes.Length];
		Array.Copy(_riff_waveHeader, 0, array, 0, _riff_waveHeader.Length);
		Array.Copy(header, 0, array, _riff_waveHeader.Length, header.Length);
		Array.Copy(bytes, 0, array, _riff_waveHeader.Length + header.Length, bytes.Length);
		return array;
	}

	public byte[] GetBytes(bool saveInMemory)
	{
		if (fileBytes != null)
		{
			return fileBytes;
		}
		if (wzReader == null)
		{
			return null;
		}
		byte[] array = new byte[header.Length - soundHeader.Length - 1];
		Buffer.BlockCopy(header, soundHeader.Length + 1, array, 0, array.Length);
		long position = wzReader.BaseStream.Position;
		wzReader.BaseStream.Position = offs;
		fileBytes = wzReader.ReadBytes(soundDataLen);
		wzReader.BaseStream.Position = position;
		if (saveInMemory)
		{
			return fileBytes;
		}
		byte[] result = fileBytes;
		fileBytes = null;
		return result;
	}

	public void SaveToFile(string file)
	{
		File.WriteAllBytes(file, GetBytes(saveInMemory: false));
	}

	public override byte[] GetBytes()
	{
		return GetBytes(saveInMemory: false);
	}
}
