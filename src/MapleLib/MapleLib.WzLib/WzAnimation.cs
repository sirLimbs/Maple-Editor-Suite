using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public sealed class WzAnimationInfo
{
	public string Name { get; }

	public WzSubProperty Container { get; }

	public int FrameCount { get; }

	public WzAnimationInfo(string name, WzSubProperty container, int frameCount)
	{
		Name = name;
		Container = container;
		FrameCount = frameCount;
	}

	public override string ToString()
	{
		return $"{Name} ({FrameCount} frames)";
	}
}

public sealed class WzAnimationClip : IDisposable
{
	public string Name { get; }

	public List<Bitmap> Frames { get; }

	public List<int> Delays { get; }

	public int Width => (Frames.Count == 0) ? 0 : Frames[0].Width;

	public int Height => (Frames.Count == 0) ? 0 : Frames[0].Height;

	public WzAnimationClip(string name, List<Bitmap> frames, List<int> delays)
	{
		Name = name;
		Frames = frames;
		Delays = delays;
	}

	public void Dispose()
	{
		foreach (Bitmap frame in Frames)
		{
			frame.Dispose();
		}
		Frames.Clear();
		Delays.Clear();
	}
}

public static class WzAnimationReader
{
	public static IReadOnlyList<WzAnimationInfo> Enumerate(IPropertyContainer root)
	{
		List<WzAnimationInfo> animations = new List<WzAnimationInfo>();
		if (root == null)
		{
			return animations;
		}
		Collect(root, string.Empty, animations);
		return animations;
	}

	public static WzAnimationClip Load(WzAnimationInfo info)
	{
		if (info == null)
		{
			throw new ArgumentNullException(nameof(info));
		}
		List<WzCanvasProperty> frames = GetSortedFrames(info.Container);
		if (frames.Count == 0)
		{
			throw new InvalidOperationException("The animation has no frames.");
		}
		List<Bitmap?> bitmaps = new List<Bitmap?>(frames.Count);
		List<PointF> origins = new List<PointF>(frames.Count);
		List<int> delays = new List<int>(frames.Count);
		float maxWidth = 1f;
		float maxHeight = 1f;
		foreach (WzCanvasProperty frame in frames)
		{
			Bitmap? bitmap = GetCanvasBitmap(frame);
			bitmaps.Add(bitmap);
			PointF origin = frame.GetCanvasOriginPosition();
			origins.Add(origin);
			if (bitmap != null)
			{
				maxWidth = Math.Max(maxWidth, bitmap.Width);
				maxHeight = Math.Max(maxHeight, bitmap.Height);
			}
			int delay = frame[WzCanvasProperty.AnimationDelayPropertyName]?.GetInt() ?? 100;
			delays.Add((delay <= 0) ? 100 : delay);
		}
		float minStartX = float.MaxValue;
		float minStartY = float.MaxValue;
		float maxEndX = float.MinValue;
		float maxEndY = float.MinValue;
		for (int i = 0; i < bitmaps.Count; i++)
		{
			Bitmap? bitmap = bitmaps[i];
			float width = ((bitmap == null) ? 1f : bitmap.Width);
			float height = ((bitmap == null) ? 1f : bitmap.Height);
			float startX = maxWidth - origins[i].X;
			float startY = maxHeight - origins[i].Y;
			minStartX = Math.Min(minStartX, startX);
			minStartY = Math.Min(minStartY, startY);
			maxEndX = Math.Max(maxEndX, startX + width);
			maxEndY = Math.Max(maxEndY, startY + height);
		}
		int canvasWidth = Math.Max(1, (int)Math.Ceiling(maxEndX - minStartX));
		int canvasHeight = Math.Max(1, (int)Math.Ceiling(maxEndY - minStartY));
		List<Bitmap> composed = new List<Bitmap>(bitmaps.Count);
		for (int i = 0; i < bitmaps.Count; i++)
		{
			Bitmap frameBitmap = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(frameBitmap))
			{
				graphics.Clear(Color.Transparent);
				graphics.CompositingMode = CompositingMode.SourceOver;
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				Bitmap? source = bitmaps[i];
				if (source != null)
				{
					float startX = maxWidth - origins[i].X;
					float startY = maxHeight - origins[i].Y;
					int drawX = (int)Math.Round(startX - minStartX);
					int drawY = (int)Math.Round(startY - minStartY);
					graphics.DrawImage(source, drawX, drawY, source.Width, source.Height);
				}
			}
			composed.Add(frameBitmap);
		}
		foreach (Bitmap? bitmap in bitmaps)
		{
			bitmap?.Dispose();
		}
		return new WzAnimationClip(info.Name, composed, delays);
	}

	public static Bitmap? GetCanvasBitmap(WzCanvasProperty canvas)
	{
		if (canvas == null)
		{
			return null;
		}
		try
		{
			if (canvas.ContainsInlinkProperty() || canvas.ContainsOutlinkProperty())
			{
				WzImageProperty linked = canvas.GetLinkedWzImageProperty();
				if (linked is WzCanvasProperty linkedCanvas && linkedCanvas.PngProperty != null)
				{
					return linkedCanvas.PngProperty.GetImage(saveInMemory: false);
				}
			}
			if (canvas.PngProperty != null)
			{
				return canvas.PngProperty.GetImage(saveInMemory: false);
			}
		}
		catch
		{
		}
		return null;
	}

	private static void Collect(IPropertyContainer container, string prefix, List<WzAnimationInfo> result)
	{
		foreach (WzImageProperty property in container.WzProperties)
		{
			if (!(property is WzSubProperty subProperty))
			{
				continue;
			}
			if (IsAnimation(subProperty))
			{
				string name = string.IsNullOrEmpty(prefix) ? subProperty.Name : (prefix + "/" + subProperty.Name);
				result.Add(new WzAnimationInfo(name, subProperty, GetSortedFrames(subProperty).Count));
			}
			else
			{
				string childPrefix = string.IsNullOrEmpty(prefix) ? subProperty.Name : (prefix + "/" + subProperty.Name);
				Collect(subProperty, childPrefix, result);
			}
		}
	}

	private static bool IsAnimation(WzSubProperty subProperty)
	{
		List<WzCanvasProperty> frames = GetSortedFrames(subProperty);
		if (frames.Count < 2)
		{
			return false;
		}
		return subProperty.WzProperties.All((WzImageProperty property) => property is WzCanvasProperty && int.TryParse(property.Name, out int _));
	}

	private static List<WzCanvasProperty> GetSortedFrames(WzSubProperty subProperty)
	{
		List<(int Index, WzCanvasProperty Canvas)> frames = new List<(int, WzCanvasProperty)>();
		foreach (WzImageProperty property in subProperty.WzProperties)
		{
			if (property is WzCanvasProperty canvas && int.TryParse(property.Name, out int index))
			{
				frames.Add((index, canvas));
			}
		}
		frames.Sort((a, b) => a.Index.CompareTo(b.Index));
		List<WzCanvasProperty> result = new List<WzCanvasProperty>(frames.Count);
		int expected = 0;
		foreach ((int Index, WzCanvasProperty Canvas) frame in frames)
		{
			if (frame.Index != expected)
			{
				return new List<WzCanvasProperty>();
			}
			result.Add(frame.Canvas);
			expected++;
		}
		return result;
	}
}
