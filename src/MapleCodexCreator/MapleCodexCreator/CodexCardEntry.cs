using System;
using System.Drawing;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleCodexCreator;

internal sealed class CodexCardEntry : IDisposable
{
	public string FilePath { get; }

	public WzImage RootImage { get; }

	public WzSubProperty CardProperty { get; }

	public WzSubProperty Info { get; }

	public byte[] Iv { get; }

	public string IvLabel { get; }

	public bool IsXml { get; }

	public string CardId { get; }

	public int MobId { get; set; }

	public bool IsDirty { get; set; }

	public Bitmap Thumbnail { get; private set; }

	public int ImageIndex { get; set; }

	public CodexCardEntry(string filePath, WzImage rootImage, WzSubProperty cardProperty, WzSubProperty info, byte[] iv, string ivLabel, bool isXml)
	{
		FilePath = filePath;
		RootImage = rootImage;
		CardProperty = cardProperty;
		Info = info;
		Iv = iv;
		IvLabel = ivLabel;
		IsXml = isXml;
		CardId = cardProperty.Name;
		MobId = (info["mob"] as WzIntProperty)?.Value ?? 0;
		Thumbnail = BuildThumb();
	}

	public void RefreshThumbnail()
	{
		Thumbnail.Dispose();
		Thumbnail = BuildThumb();
	}

	private Bitmap BuildThumb()
	{
		WzCanvasProperty wzCanvasProperty = MainForm.ResolveCanvas(Info, "iconRaw") ?? MainForm.ResolveCanvas(Info, "icon");
		if (wzCanvasProperty == null)
		{
			Bitmap bitmap = new Bitmap(48, 48);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Transparent);
			return bitmap;
		}
		using Bitmap? original = WzAnimationReader.GetCanvasBitmap(wzCanvasProperty);
		if (original == null)
		{
			Bitmap empty = new Bitmap(48, 48);
			using Graphics graphics = Graphics.FromImage(empty);
			graphics.Clear(Color.Transparent);
			return empty;
		}
		return new Bitmap(original, new Size(48, 48));
	}

	public void Dispose()
	{
		Thumbnail.Dispose();
	}
}
