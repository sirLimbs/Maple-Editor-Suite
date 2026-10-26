using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MapleLib.Helpers;

public class ImageFormatDetector
{
	public static bool UsePreBigBangImageFormats { get; set; }

	public static SurfaceFormat DetermineTextureFormat(byte[] argbData, int width, int height)
	{
		if (argbData == null || argbData.Length == 0)
		{
			throw new ArgumentException("Invalid argbData");
		}
		if (argbData.Length != width * height * 4)
		{
			throw new ArgumentException("Data length does not match dimensions");
		}
		(int uniqueRgbColors, int uniqueAlphaValues, bool hasAlpha, bool hasPartialAlpha, byte maxAlpha, double avgAlphaGradient, double alphaVariance, bool isGrayscale) tuple = AnalyzeImageData(argbData, width, height);
		int item = tuple.uniqueRgbColors;
		int item2 = tuple.uniqueAlphaValues;
		bool item3 = tuple.hasAlpha;
		bool item4 = tuple.hasPartialAlpha;
		byte item5 = tuple.maxAlpha;
		double item6 = tuple.avgAlphaGradient;
		double item7 = tuple.alphaVariance;
		bool item8 = tuple.isGrayscale;
		bool flag = width * height < 65536;
		bool flag2 = !UsePreBigBangImageFormats && IsDxtCompressionCandidate(width, height);
		if (item8 & item3 & flag2)
		{
			return SurfaceFormat.Dxt3;
		}
		if (!item3)
		{
			if (item <= 65536)
			{
				return SurfaceFormat.Bgr565;
			}
			return flag ? SurfaceFormat.Bgr565 : SurfaceFormat.Bgra32;
		}
		if (item > 4096 || item2 > 16)
		{
			if (flag2)
			{
				return (item6 < 10.0) ? SurfaceFormat.Dxt5 : SurfaceFormat.Dxt3;
			}
			if (!item4 && item2 <= 2 && item <= 32768)
			{
				return SurfaceFormat.Bgra5551;
			}
			return (flag && !item4) ? SurfaceFormat.Bgra4444 : SurfaceFormat.Bgra32;
		}
		if (!item4 && item2 <= 2 && item <= 32768)
		{
			return SurfaceFormat.Bgra5551;
		}
		if (item4)
		{
			return SurfaceFormat.Bgra4444;
		}
		return flag2 ? SurfaceFormat.Dxt3 : SurfaceFormat.Bgra4444;
	}

	public static (int uniqueRgbColors, int uniqueAlphaValues, bool hasAlpha, bool hasPartialAlpha, byte maxAlpha, double avgAlphaGradient, double alphaVariance, bool isGrayscale) AnalyzeImageData(byte[] argbData, int width, int height)
	{
		bool item = false;
		bool item2 = false;
		byte b = 0;
		bool flag = true;
		HashSet<uint> hashSet = new HashSet<uint>();
		HashSet<byte> hashSet2 = new HashSet<byte>();
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		int num4 = 0;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num5 = (i * width + j) * 4;
				byte b2 = argbData[num5 + 3];
				byte b3 = argbData[num5 + 2];
				byte b4 = argbData[num5 + 1];
				byte b5 = argbData[num5];
				if (b2 < byte.MaxValue)
				{
					item = true;
				}
				if (b2 > 5 && b2 < 250)
				{
					item2 = true;
				}
				b = Math.Max(b, b2);
				hashSet2.Add(b2);
				num += b2;
				num2 += (long)b2 * (long)b2;
				uint item3 = (uint)((b3 << 16) | (b4 << 8) | b5);
				hashSet.Add(item3);
				if (flag && (Math.Abs(b3 - b4) > 8 || Math.Abs(b4 - b5) > 8 || Math.Abs(b3 - b5) > 8))
				{
					flag = false;
				}
				if (j > 0)
				{
					byte b6 = argbData[num5 - 4 + 3];
					num3 += Math.Abs(b2 - b6);
					num4++;
				}
				if (i > 0)
				{
					byte b7 = argbData[num5 - width * 4 + 3];
					num3 += Math.Abs(b2 - b7);
					num4++;
				}
			}
		}
		int num6 = argbData.Length / 4;
		double num7 = (double)num / (double)num6;
		double item4 = (double)num2 / (double)num6 - num7 * num7;
		double item5 = ((num4 > 0) ? ((double)num3 / (double)num4) : 0.0);
		return (uniqueRgbColors: hashSet.Count, uniqueAlphaValues: hashSet2.Count, hasAlpha: item, hasPartialAlpha: item2, maxAlpha: b, avgAlphaGradient: item5, alphaVariance: item4, isGrayscale: flag);
	}

	public static bool IsDxtCompressionCandidate(int width, int height)
	{
		return width % 4 == 0 && height % 4 == 0 && width >= 4 && height >= 4 && width * height >= 4096;
	}
}
