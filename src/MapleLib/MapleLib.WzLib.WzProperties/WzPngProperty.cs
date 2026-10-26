#define DEBUG
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MapleLib.WzLib.WzProperties;

public class WzPngProperty : WzImageProperty
{
	private int width;

	private int height;

	private WzPngFormat format;

	internal byte[] compressedImageBytes;

	internal Bitmap png;

	internal WzObject parent;

	internal bool listWzUsed = false;

	internal WzBinaryReader wzReader;

	internal long offs;

	public override object WzValue => GetImage(saveInMemory: false);

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
			return "PNG";
		}
		set
		{
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.PNG;

	public int Width
	{
		get
		{
			return width;
		}
		set
		{
			width = value;
		}
	}

	public int Height
	{
		get
		{
			return height;
		}
		set
		{
			height = value;
		}
	}

	public WzPngFormat Format
	{
		get
		{
			return format;
		}
		set
		{
			format = value;
		}
	}

	public bool ListWzUsed
	{
		get
		{
			return listWzUsed;
		}
		set
		{
			if (value != listWzUsed)
			{
				listWzUsed = value;
				CompressPng(GetImage(saveInMemory: false));
			}
		}
	}

	public Bitmap PNG
	{
		set
		{
			if (png != null && png != value)
			{
				png.Dispose();
			}
			CompressPng(value);
			png = null;
		}
	}

	public override void SetValue(object value)
	{
		if (value is Bitmap)
		{
			PNG = (Bitmap)value;
		}
		else
		{
			compressedImageBytes = (byte[])value;
		}
	}

	public override WzImageProperty DeepClone()
	{
		WzPngProperty wzPngProperty = new WzPngProperty
		{
			width = width,
			height = height,
			format = format,
			listWzUsed = listWzUsed
		};
		if (compressedImageBytes != null)
		{
			wzPngProperty.compressedImageBytes = (byte[])compressedImageBytes.Clone();
		}
		else if (wzReader != null)
		{
			try
			{
				byte[] compressedBytes = GetCompressedBytes(saveInMemory: true);
				if (compressedBytes != null)
				{
					wzPngProperty.compressedImageBytes = (byte[])compressedBytes.Clone();
				}
				else
				{
					Debug.WriteLine($"[WzPngProperty.DeepClone] GetCompressedBytes returned null for {width}x{height} {format}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("[WzPngProperty.DeepClone] Reader failed, falling back to bitmap: " + ex.Message);
				Bitmap image = GetImage(saveInMemory: false);
				if (image != null)
				{
					wzPngProperty.PNG = (Bitmap)image.Clone();
				}
			}
		}
		else if (png != null)
		{
			Debug.WriteLine($"[WzPngProperty.DeepClone] No compressed bytes or reader, using bitmap copy for {width}x{height}");
			wzPngProperty.PNG = (Bitmap)png.Clone();
		}
		else
		{
			Debug.WriteLine($"[WzPngProperty.DeepClone] WARNING: No data available for {width}x{height} {format}");
		}
		return wzPngProperty;
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		throw new NotImplementedException("Cannot write a PngProperty");
	}

	public override void Dispose()
	{
		compressedImageBytes = null;
		if (png != null)
		{
			png.Dispose();
			png = null;
		}
		wzReader = null;
	}

	public WzPngProperty()
	{
	}

	public void SetCompressedBytes(byte[] bytes, int width, int height, WzPngFormat format)
	{
		compressedImageBytes = bytes;
		this.width = width;
		this.height = height;
		this.format = format;
		if (png != null)
		{
			png.Dispose();
			png = null;
		}
		wzReader = null;
	}

	public void SetBitmapPreservingFormat(Bitmap bitmap, WzPngFormat? targetFormat = null)
	{
		ArgumentNullException.ThrowIfNull(bitmap, "bitmap");
		if (!targetFormat.HasValue)
		{
			PNG = (Bitmap)bitmap.Clone();
			return;
		}
		WzPngFormat value = targetFormat.Value;
		bool isGrayscale = value == WzPngFormat.Format3;
		SurfaceFormat xNASurfaceFormat = value.GetXNASurfaceFormat();
		(WzPngFormat, byte[]) tuple = PngUtility.CompressImageToPngFormat(bitmap, xNASurfaceFormat, isGrayscale);
		WzPngFormat item = tuple.Item1;
		byte[] item2 = tuple.Item2;
		width = bitmap.Width;
		height = bitmap.Height;
		format = item;
		compressedImageBytes = Compress(item2);
		if (png != null)
		{
			png.Dispose();
			png = null;
		}
		if (listWzUsed)
		{
			using MemoryStream memoryStream = new MemoryStream();
			using WzBinaryWriter wzBinaryWriter = new WzBinaryWriter(memoryStream, WzTool.GetIvByMapleVersion(WzMapleVersion.GMS));
			wzBinaryWriter.Write(2);
			for (int i = 0; i < 2; i++)
			{
				wzBinaryWriter.Write((byte)(compressedImageBytes[i] ^ wzBinaryWriter.WzKey[i]));
			}
			wzBinaryWriter.Write(compressedImageBytes.Length - 2);
			for (int j = 2; j < compressedImageBytes.Length; j++)
			{
				wzBinaryWriter.Write((byte)(compressedImageBytes[j] ^ wzBinaryWriter.WzKey[j - 2]));
			}
			compressedImageBytes = memoryStream.ToArray();
		}
		wzReader = null;
	}

	internal WzPngProperty(WzBinaryReader reader, bool parseNow)
	{
		wzReader = reader;
		width = reader.ReadCompressedInt();
		height = reader.ReadCompressedInt();
		int num = reader.ReadCompressedInt();
		int num2 = reader.ReadCompressedInt();
		format = (WzPngFormat)(num + (num2 << 8));
		reader.BaseStream.Position += 4L;
		offs = reader.BaseStream.Position;
		int num3 = reader.ReadInt32() - 1;
		reader.BaseStream.Position++;
		lock (reader)
		{
			if (num3 <= 0)
			{
				return;
			}
			if (parseNow)
			{
				if (wzReader == null)
				{
					compressedImageBytes = reader.ReadBytes(num3);
				}
				else
				{
					compressedImageBytes = wzReader.ReadBytes(num3);
				}
				ParsePng(saveInMemory: true);
			}
			else
			{
				reader.BaseStream.Position += num3;
			}
		}
	}

	public byte[] GetCompressedBytes(bool saveInMemory)
	{
		if (compressedImageBytes == null)
		{
			lock (wzReader)
			{
				long position = wzReader.BaseStream.Position;
				wzReader.BaseStream.Position = offs;
				int num = wzReader.ReadInt32() - 1;
				if (num <= 0)
				{
					throw new Exception("The length of the image is negative. WzPngProperty. Wrong WzIV?");
				}
				wzReader.BaseStream.Position++;
				if (num > 0)
				{
					compressedImageBytes = wzReader.ReadBytes(num);
				}
				wzReader.BaseStream.Position = position;
			}
			if (!saveInMemory)
			{
				byte[] result = compressedImageBytes;
				compressedImageBytes = null;
				return result;
			}
		}
		return compressedImageBytes;
	}

	public byte[] GetCompressedBytesForExtraction(bool saveInMemory)
	{
		byte[] compressedBytes = GetCompressedBytes(saveInMemory);
		if (compressedBytes == null || compressedBytes.Length < 2)
		{
			return compressedBytes;
		}
		ushort num = (ushort)(compressedBytes[0] | (compressedBytes[1] << 8));
		if (num == 40056 || num == 55928 || num == 376 || num == 24184)
		{
			return compressedBytes;
		}
		WzMutableKey wzMutableKey = wzReader?.WzKey ?? ParentImage?.reader?.WzKey;
		if (wzMutableKey == null)
		{
			return compressedBytes;
		}
		try
		{
			using MemoryStream input = new MemoryStream(compressedBytes);
			using BinaryReader binaryReader = new BinaryReader(input);
			using MemoryStream memoryStream = new MemoryStream();
			int num2 = compressedBytes.Length;
			while (binaryReader.BaseStream.Position < num2)
			{
				int num3 = binaryReader.ReadInt32();
				for (int i = 0; i < num3; i++)
				{
					memoryStream.WriteByte((byte)(binaryReader.ReadByte() ^ wzMutableKey[i]));
				}
			}
			memoryStream.Position = 2L;
			int uncompressedSize = GetUncompressedSize();
			byte[] array = new byte[uncompressedSize];
			int j = 0;
			using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
			{
				int num4;
				for (; j < uncompressedSize; j += num4)
				{
					num4 = deflateStream.Read(array, j, uncompressedSize - j);
					if (num4 == 0)
					{
						break;
					}
				}
			}
			using MemoryStream memoryStream2 = new MemoryStream();
			memoryStream2.WriteByte(120);
			memoryStream2.WriteByte(156);
			using (DeflateStream deflateStream2 = new DeflateStream(memoryStream2, CompressionLevel.Optimal, leaveOpen: true))
			{
				deflateStream2.Write(array, 0, array.Length);
			}
			return memoryStream2.ToArray();
		}
		catch
		{
			return compressedBytes;
		}
	}

	private int GetUncompressedSize()
	{
		WzPngFormat wzPngFormat = Format;
		if (1 == 0)
		{
		}
		int result = wzPngFormat switch
		{
			WzPngFormat.Format1 => width * height * 2, 
			WzPngFormat.Format2 => width * height * 4, 
			WzPngFormat.Format3 => width * height * 4, 
			WzPngFormat.Format513 => width * height * 2, 
			WzPngFormat.Format517 => width * height / 128, 
			WzPngFormat.Format1026 => width * height * 4, 
			WzPngFormat.Format2050 => width * height * 4, 
			_ => width * height * 4, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public Bitmap GetImage(bool saveInMemory)
	{
		if (png != null)
		{
			if (saveInMemory)
			{
				return png;
			}
			return (Bitmap)png.Clone();
		}
		Bitmap result = DecodeBitmap(saveInMemory);
		if (saveInMemory)
		{
			png = result;
		}
		return result;
	}

	internal static byte[] Decompress(byte[] compressedBuffer, int decompressedSize)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(compressedBuffer, 2, compressedBuffer.Length - 2);
		byte[] array = new byte[decompressedSize];
		memoryStream.Position = 0L;
		using DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
		deflateStream.Read(array, 0, array.Length);
		return array;
	}

	internal static byte[] Compress(byte[] decompressedBuffer)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
		{
			deflateStream.Write(decompressedBuffer, 0, decompressedBuffer.Length);
		}
		memoryStream.Position = 0L;
		byte[] array = new byte[memoryStream.Length + 2];
		memoryStream.Read(array, 2, array.Length - 2);
		Buffer.BlockCopy(new byte[2] { 120, 156 }, 0, array, 0, 2);
		return array;
	}

	public void ParsePng(bool saveInMemory, Texture2D texture2d = null)
	{
		Bitmap bitmap = DecodeBitmap(saveInMemory, texture2d);
		if (saveInMemory)
		{
			png = bitmap;
		}
		else if (bitmap == null)
		{
			png = null;
		}
	}

	private Bitmap DecodeBitmap(bool saveInMemory, Texture2D texture2d = null)
	{
		byte[] rawImage = GetRawImage(saveInMemory);
		if (rawImage == null)
		{
			return null;
		}
		try
		{
			Bitmap bitmap = new Bitmap(width, height, Format.GetPixelFormat());
			System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
			switch (Format)
			{
			case WzPngFormat.Format1:
			{
				BitmapData bitmapData8 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				PngUtility.DecompressImage_PixelDataBgra4444(rawImage.AsSpan(), width, height, bitmap, bitmapData8);
				bitmap.UnlockBits(bitmapData8);
				break;
			}
			case WzPngFormat.Format2:
			{
				BitmapData bitmapData7 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				Marshal.Copy(rawImage, 0, bitmapData7.Scan0, rawImage.Length);
				bitmap.UnlockBits(bitmapData7);
				break;
			}
			case WzPngFormat.Format3:
			{
				BitmapData bitmapData6 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				PngUtility.DecompressImageDXT3(rawImage, width, height, bitmapData6);
				bitmap.UnlockBits(bitmapData6);
				break;
			}
			case WzPngFormat.Format257:
			{
				BitmapData bitmapData5 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
				PngUtility.CopyBmpDataWithStride(rawImage, bitmap.Width * 2, bitmapData5);
				bitmap.UnlockBits(bitmapData5);
				break;
			}
			case WzPngFormat.Format513:
			{
				BitmapData bitmapData4 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
				Marshal.Copy(rawImage, 0, bitmapData4.Scan0, rawImage.Length);
				bitmap.UnlockBits(bitmapData4);
				break;
			}
			case WzPngFormat.Format517:
			{
				BitmapData bitmapData3 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
				PngUtility.DecompressImage_PixelDataForm517(rawImage, width, height, bitmap, bitmapData3);
				bitmap.UnlockBits(bitmapData3);
				break;
			}
			case WzPngFormat.Format1026:
			{
				BitmapData bitmapData2 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				PngUtility.DecompressImageDXT3(rawImage, width, height, bitmapData2);
				bitmap.UnlockBits(bitmapData2);
				break;
			}
			case WzPngFormat.Format2050:
			{
				BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				PngUtility.DecompressImageDXT5(rawImage, width, height, bitmapData);
				bitmap.UnlockBits(bitmapData);
				break;
			}
			default:
				ErrorLogger.Log(ErrorLevel.MissingFeature, $"Unknown PNG format {Format}");
				break;
			}
			if (bitmap != null && texture2d != null)
			{
				Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(Microsoft.Xna.Framework.Point.Zero, new Microsoft.Xna.Framework.Point(width, height));
				texture2d.SetData(0, 0, value, rawImage, 0, rawImage.Length);
			}
			return bitmap;
		}
		catch (InvalidDataException)
		{
			return null;
		}
	}

	internal byte[] GetRawImage(bool saveInMemory)
	{
		byte[] compressedBytes = GetCompressedBytes(saveInMemory);
		using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(compressedBytes)))
		{
			ushort num = binaryReader.ReadUInt16();
			listWzUsed = num != 40056 && num != 55928 && num != 376 && num != 24184;
			DeflateStream deflateStream;
			if (!listWzUsed)
			{
				deflateStream = new DeflateStream(binaryReader.BaseStream, CompressionMode.Decompress);
			}
			else
			{
				binaryReader.BaseStream.Position -= 2L;
				MemoryStream memoryStream = new MemoryStream();
				int num2 = 0;
				int num3 = compressedBytes.Length;
				WzMutableKey wzMutableKey = wzReader?.WzKey ?? ParentImage?.reader?.WzKey;
				if (wzMutableKey == null)
				{
					throw new Exception("Cannot decrypt listWz format PNG - no WzKey available. " + $"wzReader={wzReader != null}, ParentImage={ParentImage != null}");
				}
				while (binaryReader.BaseStream.Position < num3)
				{
					num2 = binaryReader.ReadInt32();
					for (int i = 0; i < num2; i++)
					{
						memoryStream.WriteByte((byte)(binaryReader.ReadByte() ^ wzMutableKey[i]));
					}
				}
				memoryStream.Position = 2L;
				deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
			}
			int num4 = 0;
			byte[] array = null;
			switch (Format)
			{
			case WzPngFormat.Format1:
				num4 = width * height * 2;
				array = new byte[num4];
				break;
			case WzPngFormat.Format2:
				num4 = width * height * 4;
				array = new byte[num4];
				break;
			case WzPngFormat.Format3:
				num4 = width * height * 4;
				array = new byte[num4];
				break;
			case WzPngFormat.Format257:
				num4 = width * height * 2;
				array = new byte[num4];
				break;
			case WzPngFormat.Format513:
				num4 = width * height * 2;
				array = new byte[num4];
				break;
			case WzPngFormat.Format517:
				num4 = width * height / 128;
				array = new byte[num4];
				break;
			case WzPngFormat.Format1026:
				num4 = width * height * 4;
				array = new byte[num4];
				break;
			case WzPngFormat.Format2050:
				num4 = width * height;
				array = new byte[num4];
				break;
			default:
				ErrorLogger.Log(ErrorLevel.MissingFeature, $"Unknown PNG format {Format}");
				break;
			}
			if (array != null)
			{
				using (deflateStream)
				{
					int num5;
					for (int j = 0; j < array.Length; j += num5)
					{
						num5 = deflateStream.Read(array, j, array.Length - j);
						if (num5 == 0)
						{
							break;
						}
					}
					return array;
				}
			}
		}
		return null;
	}

	internal void CompressPng(Bitmap bmp)
	{
		width = bmp.Width;
		height = bmp.Height;
		BitmapData bitmapData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		byte[] array;
		try
		{
			int num = bmp.Width * bmp.Height * 4;
			array = new byte[num];
			if (bitmapData.Stride == bmp.Width * 4)
			{
				Marshal.Copy(bitmapData.Scan0, array, 0, num);
			}
			else
			{
				int num2 = bmp.Width * 4;
				for (int i = 0; i < bmp.Height; i++)
				{
					nint source = bitmapData.Scan0 + i * bitmapData.Stride;
					Marshal.Copy(source, array, i * num2, num2);
				}
			}
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
		SurfaceFormat surfaceFormat = ImageFormatDetector.DetermineTextureFormat(array, bmp.Width, bmp.Height);
		(WzPngFormat, byte[]) tuple = PngUtility.CompressImageToPngFormat(bmp, surfaceFormat);
		WzPngFormat item = tuple.Item1;
		byte[] item2 = tuple.Item2;
		Format = item;
		compressedImageBytes = Compress(item2);
		if (!listWzUsed)
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using WzBinaryWriter wzBinaryWriter = new WzBinaryWriter(memoryStream, WzTool.GetIvByMapleVersion(WzMapleVersion.GMS));
		wzBinaryWriter.Write(2);
		for (int j = 0; j < 2; j++)
		{
			wzBinaryWriter.Write((byte)(compressedImageBytes[j] ^ wzBinaryWriter.WzKey[j]));
		}
		wzBinaryWriter.Write(compressedImageBytes.Length - 2);
		for (int k = 2; k < compressedImageBytes.Length; k++)
		{
			wzBinaryWriter.Write((byte)(compressedImageBytes[k] ^ wzBinaryWriter.WzKey[k - 2]));
		}
		compressedImageBytes = memoryStream.ToArray();
	}

	public override Bitmap GetBitmap()
	{
		return GetImage(saveInMemory: false);
	}
}
