using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using MapleLib.WzLib.WzProperties;
using Microsoft.Xna.Framework.Graphics;

namespace MapleLib.Helpers;

public class PngUtility
{
	public static byte[] BitmapToByteArray(Bitmap bitmap)
	{
		using MemoryStream memoryStream = new MemoryStream();
		bitmap.Save(memoryStream, ImageFormat.Png);
		return memoryStream.ToArray();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color RGB565ToColor(ushort val)
	{
		int num = (val & 0xF800) >> 11;
		int num2 = (val & 0x7E0) >> 5;
		int num3 = val & 0x1F;
		return Color.FromArgb((num << 3) | (num >> 2), (num2 << 2) | (num2 >> 4), (num3 << 3) | (num3 >> 2));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void DecompressImage_PixelDataBgra4444(Span<byte> rawData, int width, int height, Bitmap bmp, BitmapData bmpData)
	{
		int num = width * height * 2;
		if (rawData.Length < num)
		{
			throw new ArgumentException("Raw data length is insufficient for the specified dimensions.");
		}
		int num2 = num * 2;
		Span<byte> span = ((num2 > 10240) ? new byte[num2].AsSpan() : stackalloc byte[num2]);
		Span<byte> span2 = span;
		if (Sse2.IsSupported)
		{
			int num3 = 0;
			fixed (byte* ptr = rawData)
			{
				for (num3 = 0; num3 <= rawData.Length - 16; num3 += 16)
				{
					Vector128<byte> vector = Sse2.LoadVector128(ptr + num3);
					Vector128<byte> vector2 = Sse2.And(vector, Vector128.Create((byte)15));
					Vector128<byte> vector3 = Sse2.And(Sse2.ShiftRightLogical(vector.AsUInt32(), 4).AsByte(), Vector128.Create((byte)15));
					Vector128<byte> left = Sse2.Or(Sse2.ShiftLeftLogical(vector2.AsUInt32(), 4).AsByte(), vector2);
					Vector128<byte> right = Sse2.Or(Sse2.ShiftLeftLogical(vector3.AsUInt32(), 4).AsByte(), vector3);
					Vector128<byte> vector4 = Sse2.UnpackLow(left, right);
					Vector128<byte> vector5 = Sse2.UnpackHigh(left, right);
					vector4.CopyTo(span2.Slice(num3 * 2));
					vector5.CopyTo(span2.Slice(num3 * 2 + 16));
				}
			}
			for (; num3 < num; num3++)
			{
				byte b = rawData[num3];
				int num4 = b & 0xF;
				byte b2 = (byte)(num4 | (num4 << 4));
				span2[num3 * 2] = b2;
				int num5 = b & 0xF0;
				byte b3 = (byte)(num5 | (num5 >> 4));
				span2[num3 * 2 + 1] = b3;
			}
		}
		else
		{
			for (int i = 0; i < num; i++)
			{
				byte b4 = rawData[i];
				int num6 = b4 & 0xF;
				byte b5 = (byte)(num6 | (num6 << 4));
				span2[i * 2] = b5;
				int num7 = b4 & 0xF0;
				byte b6 = (byte)(num7 | (num7 >> 4));
				span2[i * 2 + 1] = b6;
			}
		}
		span2.CopyTo(new Span<byte>(((IntPtr)bmpData.Scan0).ToPointer(), num2));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void DecompressImageDXT3(byte[] rawData, int width, int height, BitmapData bmpData)
	{
		int blockCountX = (width + 3) / 4;
		int num = (height + 3) / 4;
		int num2 = blockCountX * num * 16;
		if (rawData.Length < num2)
		{
			throw new ArgumentException($"Raw data length ({rawData.Length}) is insufficient for the specified dimensions ({width}x{height}).");
		}
		if (Sse2.IsSupported && Ssse3.IsSupported)
		{
			byte[] decoded = new byte[width * height * 4];
			Parallel.For(0, height / 4, (int blockY) =>
			{
				Color[] array = new Color[4];
				int[] array2 = new int[16];
				byte[] array3 = new byte[16];
				for (int i = 0; i < width / 4; i++)
				{
					int num3 = i * 4;
					int num4 = blockY * 4;
					int num5 = num3 * 4 + num4 * width;
					ExpandAlphaTableDXT3(array3, rawData, num5);
					ushort c = BitConverter.ToUInt16(rawData, num5 + 8);
					ushort c2 = BitConverter.ToUInt16(rawData, num5 + 10);
					ExpandColorTable(array, c, c2);
					ExpandColorIndexTable(array2, rawData, num5 + 12);
					Vector128<byte> value = Vector128.Create(array[0].B, array[1].B, array[2].B, array[3].B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
					Vector128<byte> value2 = Vector128.Create(array[0].G, array[1].G, array[2].G, array[3].G, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
					Vector128<byte> value3 = Vector128.Create(array[0].R, array[1].R, array[2].R, array[3].R, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
					for (int j = 0; j < 4; j++)
					{
						int num6 = j * 4;
						Vector128<byte> mask = Vector128.Create((byte)array2[num6], (byte)array2[num6 + 1], (byte)array2[num6 + 2], (byte)array2[num6 + 3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
						Vector128<byte> vector = Vector128.Create(array3[num6], array3[num6 + 1], array3[num6 + 2], array3[num6 + 3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
						Vector128<byte> left = Ssse3.Shuffle(value, mask);
						Vector128<byte> left2 = Ssse3.Shuffle(value2, mask);
						Vector128<byte> right = Ssse3.Shuffle(value3, mask);
						Vector128<byte> right2 = vector;
						Vector128<byte> left3 = Sse2.UnpackLow(left, right);
						Vector128<byte> right3 = Sse2.UnpackLow(left2, right2);
						Vector128<byte> source = Sse2.UnpackLow(left3, right3);
						int num7 = (num4 + j) * width * 4 + num3 * 4;
						fixed (byte* ptr = decoded)
						{
							Sse2.Store(ptr + num7, source);
						}
					}
				}
			});
			Marshal.Copy(decoded, 0, bmpData.Scan0, decoded.Length);
			return;
		}
		byte[] decoded2 = new byte[width * height * 4];
		Parallel.For(0, num, () => (new Color[4], new int[16], new byte[16]), (int blockY, ParallelLoopState _, (Color[], int[], byte[]) buffers) =>
		{
			Color[] item = buffers.Item1;
			int[] item2 = buffers.Item2;
			byte[] item3 = buffers.Item3;
			for (int i = 0; i < blockCountX; i++)
			{
				int num3 = i * 4;
				int num4 = blockY * 4;
				int num5 = blockY * blockCountX + i;
				int num6 = num5 * 16;
				ExpandAlphaTableDXT3(item3, rawData, num6);
				ushort c = BitConverter.ToUInt16(rawData, num6 + 8);
				ushort c2 = BitConverter.ToUInt16(rawData, num6 + 10);
				ExpandColorTable(item, c, c2);
				ExpandColorIndexTable(item2, rawData, num6 + 12);
				for (int j = 0; j < 4; j++)
				{
					int num7 = num4 + j;
					if (num7 < height)
					{
						for (int k = 0; k < 4; k++)
						{
							int num8 = num3 + k;
							if (num8 < width)
							{
								Color color = item[item2[j * 4 + k]];
								byte alpha = item3[j * 4 + k];
								SetPixel(decoded2, num8, num7, width, color, alpha);
							}
						}
					}
				}
			}
			return buffers;
		}, ((Color[], int[], byte[]) _) =>
		{
		});
		Marshal.Copy(decoded2, 0, bmpData.Scan0, decoded2.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DecompressImage_PixelDataForm517(byte[] rawData, int width, int height, Bitmap bmp, BitmapData bmpData)
	{
		byte[] array = new byte[width * height * 2];
		int num = 0;
		int i = 0;
		for (int num2 = height / 16; i < num2; i++)
		{
			int num3 = num;
			int j = 0;
			for (int num4 = width / 16; j < num4; j++)
			{
				int num5 = (j + i * num4) * 2;
				byte b = rawData[num5];
				byte b2 = rawData[num5 + 1];
				for (int k = 0; k < 16; k++)
				{
					array[num3++] = b;
					array[num3++] = b2;
				}
			}
			for (int l = 1; l < 16; l++)
			{
				Array.Copy(array, num, array, num3, width * 2);
				num3 += width * 2;
			}
			num += width * 32;
		}
		Marshal.Copy(array, 0, bmpData.Scan0, array.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void DecompressImageDXT5(byte[] rawData, int width, int height, BitmapData bmpData)
	{
		int blockCountX = (width + 3) / 4;
		int num = (height + 3) / 4;
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentException("Width and height must be positive.");
		}
		int num2 = blockCountX * num * 16;
		if (rawData.Length < num2)
		{
			throw new ArgumentException($"Raw data length ({rawData.Length}) is insufficient for the specified dimensions ({width}x{height}).");
		}
		int stride = bmpData.Stride;
		if (Sse2.IsSupported)
		{
			byte* ptr = (byte*)rawData.AsSpan().GetPinnableReference();
			byte* pDecoded = (byte*)bmpData.Scan0;
			Parallel.For(0, num, (int y) =>
			{
				for (int i = 0; i < blockCountX; i += 2)
				{
					int num3 = (y * blockCountX + i) * 16;
					if (num3 + 16 <= rawData.Length)
					{
						byte a = rawData[num3];
						byte a2 = rawData[num3 + 1];
						byte[] array = new byte[8];
						ExpandAlphaTableDXT5(array, a, a2);
						int[] array2 = new int[16];
						ExpandAlphaIndexTableDXT5(array2, rawData, num3 + 2);
						ushort c = BitConverter.ToUInt16(rawData, num3 + 8);
						ushort c2 = BitConverter.ToUInt16(rawData, num3 + 10);
						Color[] array3 = new Color[4];
						ExpandColorTable(array3, c, c2);
						int[] array4 = new int[16];
						ExpandColorIndexTable(array4, rawData, num3 + 12);
						for (int j = 0; j < 4; j++)
						{
							int num4 = y * 4 + j;
							if (num4 < height)
							{
								for (int k = 0; k < 4; k++)
								{
									int num5 = i * 4 + k;
									if (num5 < width)
									{
										int num6 = num4 * stride + num5 * 4;
										Color color = array3[array4[j * 4 + k]];
										byte b = array[array2[j * 4 + k]];
										pDecoded[num6] = color.B;
										pDecoded[num6 + 1] = color.G;
										pDecoded[num6 + 2] = color.R;
										pDecoded[num6 + 3] = b;
									}
								}
							}
						}
						if (i + 1 < blockCountX && num3 + 32 <= rawData.Length)
						{
							byte a3 = rawData[num3 + 16];
							byte a4 = rawData[num3 + 17];
							byte[] array5 = new byte[8];
							ExpandAlphaTableDXT5(array5, a3, a4);
							int[] array6 = new int[16];
							ExpandAlphaIndexTableDXT5(array6, rawData, num3 + 18);
							ushort c3 = BitConverter.ToUInt16(rawData, num3 + 24);
							ushort c4 = BitConverter.ToUInt16(rawData, num3 + 26);
							Color[] array7 = new Color[4];
							ExpandColorTable(array7, c3, c4);
							int[] array8 = new int[16];
							ExpandColorIndexTable(array8, rawData, num3 + 28);
							for (int l = 0; l < 4; l++)
							{
								int num7 = y * 4 + l;
								if (num7 < height)
								{
									for (int m = 0; m < 4; m++)
									{
										int num8 = (i + 1) * 4 + m;
										if (num8 < width)
										{
											int num9 = num7 * stride + num8 * 4;
											Color color2 = array7[array8[l * 4 + m]];
											byte b2 = array5[array6[l * 4 + m]];
											pDecoded[num9] = color2.B;
											pDecoded[num9 + 1] = color2.G;
											pDecoded[num9 + 2] = color2.R;
											pDecoded[num9 + 3] = b2;
										}
									}
								}
							}
						}
					}
				}
			});
			return;
		}
		byte* pDecoded2 = (byte*)bmpData.Scan0;
		Parallel.For(0, num, () => (new Color[4], new int[16], new byte[8], new int[16]), (int blockY, ParallelLoopState _, (Color[], int[], byte[], int[]) buffers) =>
		{
			Color[] item = buffers.Item1;
			int[] item2 = buffers.Item2;
			byte[] item3 = buffers.Item3;
			int[] item4 = buffers.Item4;
			for (int i = 0; i < blockCountX; i++)
			{
				int num3 = i * 4;
				int num4 = blockY * 4;
				int num5 = blockY * blockCountX + i;
				int num6 = num5 * 16;
				ExpandAlphaTableDXT5(item3, rawData[num6], rawData[num6 + 1]);
				ExpandAlphaIndexTableDXT5(item4, rawData, num6 + 2);
				ushort c = BitConverter.ToUInt16(rawData, num6 + 8);
				ushort c2 = BitConverter.ToUInt16(rawData, num6 + 10);
				ExpandColorTable(item, c, c2);
				ExpandColorIndexTable(item2, rawData, num6 + 12);
				for (int j = 0; j < 4; j++)
				{
					int num7 = num4 + j;
					if (num7 < height)
					{
						for (int k = 0; k < 4; k++)
						{
							int num8 = num3 + k;
							if (num8 < width)
							{
								int num9 = num7 * stride + num8 * 4;
								Color color = item[item2[j * 4 + k]];
								byte b = item3[item4[j * 4 + k]];
								pDecoded2[num9] = color.B;
								pDecoded2[num9 + 1] = color.G;
								pDecoded2[num9 + 2] = color.R;
								pDecoded2[num9 + 3] = b;
							}
						}
					}
				}
			}
			return buffers;
		}, ((Color[], int[], byte[], int[]) _) =>
		{
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetPixel(byte[] pixelData, int x, int y, int width, Color color, byte alpha)
	{
		int num = (y * width + x) * 4;
		pixelData[num] = color.B;
		pixelData[num + 1] = color.G;
		pixelData[num + 2] = color.R;
		pixelData[num + 3] = alpha;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyBmpDataWithStride(byte[] source, int stride, BitmapData bmpData)
	{
		if (bmpData.Stride == stride)
		{
			Marshal.Copy(source, 0, bmpData.Scan0, source.Length);
			return;
		}
		for (int i = 0; i < bmpData.Height; i++)
		{
			Marshal.Copy(source, stride * i, bmpData.Scan0 + bmpData.Stride * i, stride);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ExpandColorTable(Color[] color, ushort c0, ushort c1)
	{
		color[0] = RGB565ToColor(c0);
		color[1] = RGB565ToColor(c1);
		if (c0 > c1)
		{
			color[2] = Color.FromArgb(255, (color[0].R * 2 + color[1].R + 1) / 3, (color[0].G * 2 + color[1].G + 1) / 3, (color[0].B * 2 + color[1].B + 1) / 3);
			color[3] = Color.FromArgb(255, (color[0].R + color[1].R * 2 + 1) / 3, (color[0].G + color[1].G * 2 + 1) / 3, (color[0].B + color[1].B * 2 + 1) / 3);
		}
		else
		{
			color[2] = Color.FromArgb(255, (color[0].R + color[1].R) / 2, (color[0].G + color[1].G) / 2, (color[0].B + color[1].B) / 2);
			color[3] = Color.FromArgb(255, Color.Black);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ExpandColorIndexTable(int[] colorIndex, byte[] rawData, int offset)
	{
		int num = 0;
		while (num < 16)
		{
			colorIndex[num] = rawData[offset] & 3;
			colorIndex[num + 1] = (rawData[offset] & 0xC) >> 2;
			colorIndex[num + 2] = (rawData[offset] & 0x30) >> 4;
			colorIndex[num + 3] = (rawData[offset] & 0xC0) >> 6;
			num += 4;
			offset++;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ExpandAlphaTableDXT3(byte[] alpha, byte[] rawData, int offset)
	{
		int num = 0;
		while (num < 16)
		{
			alpha[num] = (byte)(rawData[offset] & 0xF);
			alpha[num + 1] = (byte)((rawData[offset] & 0xF0) >> 4);
			num += 2;
			offset++;
		}
		for (int i = 0; i < 16; i++)
		{
			alpha[i] = (byte)(alpha[i] | (alpha[i] << 4));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ExpandAlphaTableDXT5(byte[] alpha, byte a0, byte a1)
	{
		alpha[0] = a0;
		alpha[1] = a1;
		if (a0 > a1)
		{
			for (int i = 2; i < 8; i++)
			{
				alpha[i] = (byte)(((8 - i) * a0 + (i - 1) * a1 + 3) / 7);
			}
			return;
		}
		for (int j = 2; j < 6; j++)
		{
			alpha[j] = (byte)(((6 - j) * a0 + (j - 1) * a1 + 2) / 5);
		}
		alpha[6] = 0;
		alpha[7] = byte.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ExpandAlphaIndexTableDXT5(int[] alphaIndex, byte[] rawData, int offset)
	{
		int num = 0;
		while (num < 16)
		{
			int num2 = rawData[offset] | (rawData[offset + 1] << 8) | (rawData[offset + 2] << 16);
			for (int i = 0; i < 8; i++)
			{
				int num3 = 7 << 3 * i;
				alphaIndex[num + i] = (num2 & num3) >> 3 * i;
			}
			num += 8;
			offset += 3;
		}
	}

	public static (WzPngFormat, byte[]) CompressImageToPngFormat(Bitmap bmp, SurfaceFormat format)
	{
		bool isGrayscale = false;
		if (format == SurfaceFormat.Dxt3)
		{
			isGrayscale = IsGrayscaleBitmap(bmp);
		}
		return CompressImageToPngFormat(bmp, format, isGrayscale);
	}

	public static (WzPngFormat, byte[]) CompressImageToPngFormat(Bitmap bmp, SurfaceFormat format, bool isGrayscale)
	{
		byte[] item;
		WzPngFormat item2;
		switch (format)
		{
		case SurfaceFormat.Bgra4444:
			item = GetPixelDataFormat1(bmp);
			item2 = WzPngFormat.Format1;
			break;
		case SurfaceFormat.Color:
		case SurfaceFormat.Bgra32:
			item = GetPixelDataFormat2(bmp);
			item2 = WzPngFormat.Format2;
			break;
		case SurfaceFormat.Bgr565:
			if (bmp.Width % 16 == 0 && bmp.Height % 16 == 0)
			{
				item = GetPixelDataFormat517(bmp);
				item2 = WzPngFormat.Format517;
			}
			else
			{
				item = GetPixelDataFormat513(bmp);
				item2 = WzPngFormat.Format513;
			}
			break;
		case SurfaceFormat.Bgra5551:
			item = GetPixelDataFormat257(bmp);
			item2 = WzPngFormat.Format257;
			break;
		case SurfaceFormat.Dxt3:
			item = CompressDXT3(bmp);
			item2 = (isGrayscale ? WzPngFormat.Format3 : WzPngFormat.Format1026);
			break;
		case SurfaceFormat.Dxt5:
			item = GetPixelDataFormat2050(bmp);
			item2 = WzPngFormat.Format2050;
			break;
		default:
			item = GetPixelDataFormat2(bmp);
			item2 = WzPngFormat.Format2;
			break;
		}
		return (item2, item);
	}

	private unsafe static bool IsGrayscaleBitmap(Bitmap bmp)
	{
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte* scan = (byte*)bitmapData.Scan0;
			int num = Math.Max(1, bmp.Width * bmp.Height / 1000);
			for (int i = 0; i < bmp.Width * bmp.Height; i += num)
			{
				int num2 = i / bmp.Width;
				int num3 = i % bmp.Width;
				byte* ptr = scan + num2 * bitmapData.Stride;
				int num4 = *(int*)(ptr + num3 * 4);
				byte b = (byte)(num4 & 0xFF);
				byte b2 = (byte)((num4 >> 8) & 0xFF);
				byte b3 = (byte)((num4 >> 16) & 0xFF);
				if (Math.Abs(b3 - b2) > 8 || Math.Abs(b2 - b) > 8 || Math.Abs(b3 - b) > 8)
				{
					return false;
				}
			}
			return true;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] GetPixelDataFormat1(Bitmap bmp)
	{
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] array = new byte[bmp.Width * bmp.Height * 2];
			int num = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			for (int i = 0; i < bmp.Height; i++)
			{
				byte* ptr = scan + i * bitmapData.Stride;
				for (int j = 0; j < bmp.Width; j++)
				{
					int num2 = *(int*)(ptr + j * 4);
					byte b = (byte)(num2 & 0xFF);
					byte b2 = (byte)((num2 >> 8) & 0xFF);
					byte b3 = (byte)((num2 >> 16) & 0xFF);
					byte b4 = (byte)((num2 >> 24) & 0xFF);
					byte b5 = (byte)(b >> 4);
					byte b6 = (byte)(b2 >> 4);
					byte b7 = (byte)(b3 >> 4);
					byte b8 = (byte)(b4 >> 4);
					array[num++] = (byte)((b6 << 4) | b5);
					array[num++] = (byte)((b8 << 4) | b7);
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private static byte[] GetPixelDataFormat2(Bitmap bmp)
	{
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			int num = bmp.Width * bmp.Height * 4;
			byte[] array = new byte[num];
			if (bitmapData.Stride == bmp.Width * 4)
			{
				Marshal.Copy(bitmapData.Scan0, array, 0, num);
			}
			else
			{
				for (int i = 0; i < bmp.Height; i++)
				{
					nint source = bitmapData.Scan0 + i * bitmapData.Stride;
					Marshal.Copy(source, array, i * bmp.Width * 4, bmp.Width * 4);
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] GetPixelDataFormat257(Bitmap bmp)
	{
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] array = new byte[bmp.Width * bmp.Height * 2];
			int num = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			for (int i = 0; i < bmp.Height; i++)
			{
				byte* ptr = scan + i * bitmapData.Stride;
				for (int j = 0; j < bmp.Width; j++)
				{
					int num2 = *(int*)(ptr + j * 4);
					byte b = (byte)(num2 & 0xFF);
					byte b2 = (byte)((num2 >> 8) & 0xFF);
					byte b3 = (byte)((num2 >> 16) & 0xFF);
					byte b4 = (byte)((num2 >> 24) & 0xFF);
					byte b5 = ((b4 >= 128) ? ((byte)1) : ((byte)0));
					byte b6 = (byte)(b3 * 31 / 255);
					byte b7 = (byte)(b2 * 31 / 255);
					byte b8 = (byte)(b * 31 / 255);
					ushort num3 = (ushort)((b5 << 15) | (b6 << 10) | (b7 << 5) | b8);
					array[num++] = (byte)(num3 & 0xFF);
					array[num++] = (byte)((num3 >> 8) & 0xFF);
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] GetPixelDataFormat513(Bitmap bmp)
	{
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] array = new byte[bmp.Width * bmp.Height * 2];
			int num = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			for (int i = 0; i < bmp.Height; i++)
			{
				byte* ptr = scan + i * bitmapData.Stride;
				for (int j = 0; j < bmp.Width; j++)
				{
					int num2 = *(int*)(ptr + j * 4);
					byte b = (byte)(num2 & 0xFF);
					byte b2 = (byte)((num2 >> 8) & 0xFF);
					byte b3 = (byte)((num2 >> 16) & 0xFF);
					byte b4 = (byte)(b3 * 31 / 255);
					byte b5 = (byte)(b2 * 63 / 255);
					byte b6 = (byte)(b * 31 / 255);
					ushort num3 = (ushort)((b4 << 11) | (b5 << 5) | b6);
					array[num++] = (byte)(num3 & 0xFF);
					array[num++] = (byte)((num3 >> 8) & 0xFF);
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] GetPixelDataFormat517(Bitmap bmp)
	{
		int num = 16;
		int num2 = (bmp.Width + num - 1) / num;
		int num3 = (bmp.Height + num - 1) / num;
		byte[] array = new byte[num2 * num3 * 2];
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			int num4 = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					int num5 = j * num;
					int num6 = i * num;
					if (num5 < bmp.Width && num6 < bmp.Height)
					{
						byte* ptr = scan + num6 * bitmapData.Stride;
						int num7 = *(int*)(ptr + num5 * 4);
						byte b = (byte)(num7 & 0xFF);
						byte b2 = (byte)((num7 >> 8) & 0xFF);
						byte b3 = (byte)((num7 >> 16) & 0xFF);
						byte b4 = (byte)(b3 * 31 / 255);
						byte b5 = (byte)(b2 * 63 / 255);
						byte b6 = (byte)(b * 31 / 255);
						ushort num8 = (ushort)((b4 << 11) | (b5 << 5) | b6);
						array[num4++] = (byte)(num8 & 0xFF);
						array[num4++] = (byte)((num8 >> 8) & 0xFF);
					}
					else
					{
						array[num4++] = 0;
						array[num4++] = 0;
					}
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] CompressDXT3(Bitmap bmp)
	{
		if (bmp.Width % 4 != 0 || bmp.Height % 4 != 0)
		{
			throw new ArgumentException("DXT3 compression requires width and height to be multiples of 4.");
		}
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			int num = bmp.Width / 4;
			int num2 = bmp.Height / 4;
			byte[] array = new byte[num * num2 * 16];
			int num3 = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			Color[] array2 = new Color[16];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					for (int k = 0; k < 4; k++)
					{
						int num4 = i * 4 + k;
						byte* ptr = scan + num4 * bitmapData.Stride;
						for (int l = 0; l < 4; l++)
						{
							int num5 = j * 4 + l;
							int num6 = *(int*)(ptr + num5 * 4);
							byte blue = (byte)(num6 & 0xFF);
							byte green = (byte)((num6 >> 8) & 0xFF);
							byte red = (byte)((num6 >> 16) & 0xFF);
							byte alpha = (byte)((num6 >> 24) & 0xFF);
							array2[k * 4 + l] = Color.FromArgb(alpha, red, green, blue);
						}
					}
					for (int m = 0; m < 16; m += 2)
					{
						byte b = (byte)(array2[m].A >> 4);
						byte b2 = (byte)(array2[m + 1].A >> 4);
						array[num3++] = (byte)((b2 << 4) | b);
					}
					Color[] colors = CompressBlockColors(array2, out var c, out var c2);
					byte[] array3 = ComputeColorIndices(array2, colors);
					array[num3++] = (byte)(c & 0xFF);
					array[num3++] = (byte)(c >> 8);
					array[num3++] = (byte)(c2 & 0xFF);
					array[num3++] = (byte)(c2 >> 8);
					for (int n = 0; n < 4; n++)
					{
						array[num3++] = array3[n];
					}
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private unsafe static byte[] GetPixelDataFormat2050(Bitmap bmp)
	{
		if (bmp.Width % 4 != 0 || bmp.Height % 4 != 0)
		{
			throw new ArgumentException("DXT5 compression requires width and height to be multiples of 4.");
		}
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			int num = bmp.Width / 4;
			int num2 = bmp.Height / 4;
			byte[] array = new byte[num * num2 * 16];
			int num3 = 0;
			byte* scan = (byte*)bitmapData.Scan0;
			Color[] array2 = new Color[16];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					for (int k = 0; k < 4; k++)
					{
						int num4 = i * 4 + k;
						byte* ptr = scan + num4 * bitmapData.Stride;
						for (int l = 0; l < 4; l++)
						{
							int num5 = j * 4 + l;
							int num6 = *(int*)(ptr + num5 * 4);
							byte blue = (byte)(num6 & 0xFF);
							byte green = (byte)((num6 >> 8) & 0xFF);
							byte red = (byte)((num6 >> 16) & 0xFF);
							byte alpha = (byte)((num6 >> 24) & 0xFF);
							array2[k * 4 + l] = Color.FromArgb(alpha, red, green, blue);
						}
					}
					int[] array3 = CompressBlockAlphaDXT5(array2, out var a, out var a2);
					array[num3++] = a;
					array[num3++] = a2;
					long num7 = 0L;
					for (int m = 0; m < 16; m++)
					{
						num7 |= (long)array3[m] << m * 3;
					}
					array[num3++] = (byte)(num7 & 0xFF);
					array[num3++] = (byte)((num7 >> 8) & 0xFF);
					array[num3++] = (byte)((num7 >> 16) & 0xFF);
					array[num3++] = (byte)((num7 >> 24) & 0xFF);
					array[num3++] = (byte)((num7 >> 32) & 0xFF);
					array[num3++] = (byte)((num7 >> 40) & 0xFF);
					Color[] colors = CompressBlockColors(array2, out var c, out var c2);
					byte[] array4 = ComputeColorIndices(array2, colors);
					array[num3++] = (byte)(c & 0xFF);
					array[num3++] = (byte)(c >> 8);
					array[num3++] = (byte)(c2 & 0xFF);
					array[num3++] = (byte)(c2 >> 8);
					for (int n = 0; n < 4; n++)
					{
						array[num3++] = array4[n];
					}
				}
			}
			return array;
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private static Color[] CompressBlockColors(Color[] block, out ushort c0, out ushort c1)
	{
		int num = 255;
		int num2 = 255;
		int num3 = 255;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int i = 0; i < block.Length; i++)
		{
			Color color = block[i];
			num = Math.Min(num, color.R);
			num2 = Math.Min(num2, color.G);
			num3 = Math.Min(num3, color.B);
			num4 = Math.Max(num4, color.R);
			num5 = Math.Max(num5, color.G);
			num6 = Math.Max(num6, color.B);
		}
		c0 = ColorToRGB565((byte)num4, (byte)num5, (byte)num6);
		c1 = ColorToRGB565((byte)num, (byte)num2, (byte)num3);
		Color[] array = new Color[4];
		ExpandColorTable(array, c0, c1);
		return array;
	}

	private static ushort ColorToRGB565(byte r, byte g, byte b)
	{
		byte b2 = (byte)(r * 31 / 255);
		byte b3 = (byte)(g * 63 / 255);
		byte b4 = (byte)(b * 31 / 255);
		return (ushort)((b2 << 11) | (b3 << 5) | b4);
	}

	private static byte[] ComputeColorIndices(Color[] block, Color[] colors)
	{
		byte[] array = new byte[4];
		for (int i = 0; i < 4; i++)
		{
			byte b = 0;
			for (int j = 0; j < 4; j++)
			{
				Color c = block[i * 4 + j];
				int num = 0;
				double num2 = double.MaxValue;
				for (int k = 0; k < 4; k++)
				{
					double num3 = ColorDistance(c, colors[k]);
					if (num3 < num2)
					{
						num2 = num3;
						num = k;
					}
				}
				b |= (byte)(num << j * 2);
			}
			array[i] = b;
		}
		return array;
	}

	private static double ColorDistance(Color c1, Color c2)
	{
		int num = c1.R - c2.R;
		int num2 = c1.G - c2.G;
		int num3 = c1.B - c2.B;
		return num * num + num2 * num2 + num3 * num3;
	}

	private static int[] CompressBlockAlphaDXT5(Color[] block, out byte a0, out byte a1)
	{
		byte b = byte.MaxValue;
		byte b2 = 0;
		for (int i = 0; i < block.Length; i++)
		{
			Color color = block[i];
			b = Math.Min(b, color.A);
			b2 = Math.Max(b2, color.A);
		}
		a0 = b2;
		a1 = b;
		byte[] array = new byte[8];
		ExpandAlphaTableDXT5(array, a0, a1);
		int[] array2 = new int[16];
		for (int j = 0; j < 16; j++)
		{
			byte a2 = block[j].A;
			int num = 0;
			int num2 = int.MaxValue;
			for (int k = 0; k < 8; k++)
			{
				int num3 = Math.Abs(a2 - array[k]);
				if (num3 < num2)
				{
					num2 = num3;
					num = k;
				}
			}
			array2[j] = num;
		}
		return array2;
	}
}
