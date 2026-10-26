using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Graphics;

namespace MapleLib.Converters;

public static class ImageConverter
{
	public static MemoryStream Texture2DToPng(this Texture2D texture2D)
	{
		MemoryStream memoryStream = new MemoryStream();
		texture2D.SaveAsPng(memoryStream, texture2D.Width, texture2D.Height);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return memoryStream;
	}

	public static MemoryStream Texture2DToJpg(this Texture2D texture2D)
	{
		MemoryStream memoryStream = new MemoryStream();
		texture2D.SaveAsJpeg(memoryStream, texture2D.Width, texture2D.Height);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return memoryStream;
	}

	public static Bitmap ToWinFormsBitmap(this BitmapSource bitmapsource)
	{
		using MemoryStream stream = new MemoryStream();
		BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
		bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapsource));
		bitmapEncoder.Save(stream);
		using Bitmap original = new Bitmap(stream);
		return new Bitmap(original);
	}

	public static Image ToImage(this Bitmap bitmap)
	{
		return bitmap;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] BitmapToBytes(this Bitmap bitmap)
	{
		BitmapData bitmapData = null;
		try
		{
			bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
			int num = bitmapData.Stride * bitmap.Height;
			byte[] array = new byte[num];
			nint scan = bitmapData.Scan0;
			Marshal.Copy(scan, array, 0, num);
			return array;
		}
		finally
		{
			if (bitmapData != null)
			{
				bitmap.UnlockBits(bitmapData);
			}
		}
	}

	public static BitmapSource ToWpfBitmap(this Bitmap bitmap)
	{
		if (bitmap == null)
		{
			throw new ArgumentNullException("bitmap");
		}
		using MemoryStream memoryStream = new MemoryStream();
		bitmap.Save(memoryStream, GetImageFormat(bitmap));
		memoryStream.Position = 0L;
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = memoryStream;
		bitmapImage.EndInit();
		((Freezable)bitmapImage).Freeze();
		return bitmapImage;
	}

	public static ImageFormat GetImageFormat(Bitmap bitmap)
	{
		if (bitmap == null)
		{
			throw new ArgumentNullException("bitmap");
		}
		switch (bitmap.PixelFormat)
		{
		case PixelFormat.Format1bppIndexed:
		case PixelFormat.Format4bppIndexed:
		case PixelFormat.Format8bppIndexed:
			return ImageFormat.Bmp;
		case PixelFormat.Format16bppRgb555:
		case PixelFormat.Format16bppRgb565:
		case PixelFormat.Format24bppRgb:
		case PixelFormat.Format32bppRgb:
		case PixelFormat.Format32bppPArgb:
		case PixelFormat.Format16bppGrayScale:
		case PixelFormat.Format48bppRgb:
		case PixelFormat.Format64bppPArgb:
		case PixelFormat.Format32bppArgb:
		case PixelFormat.Format64bppArgb:
			return ImageFormat.Png;
		default:
			return ImageFormat.Jpeg;
		}
	}
}
