using System;
using System.Drawing;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleImgEditor;

internal sealed class ImgItemEntry : IDisposable
{
	private readonly Action<ImgItemEntry> _saveAction;

	private readonly Action? _disposeAction;

	private readonly bool _ownsImage;

	public string FilePath { get; set; }

	public WzImage Image { get; }

	public WzSubProperty Info { get; }

	public byte[] Iv { get; }

	public string IvLabel { get; }

	public GameFileInfo Detection { get; set; }

	public string ItemId { get; set; }

	public string DisplayName { get; set; }

	public WzSubProperty? ItemProperty { get; }

	public bool IsGroupedEntry { get; }

	public Bitmap Thumbnail { get; private set; }

	public int ImageIndex { get; set; }

	public bool IsDirty { get; set; }

	public ImgItemEntry(string filePath, WzImage image, WzSubProperty info, byte[] iv, string ivLabel, GameFileInfo detection, string itemId, string displayName, Bitmap thumbnail, Action<ImgItemEntry> saveAction, Action? disposeAction = null, WzSubProperty? itemProperty = null, bool ownsImage = true, bool isGroupedEntry = false)
	{
		FilePath = filePath;
		Image = image;
		Info = info;
		Iv = iv;
		IvLabel = ivLabel;
		Detection = detection;
		ItemId = itemId;
		DisplayName = displayName;
		Thumbnail = thumbnail;
		_saveAction = saveAction;
		_disposeAction = disposeAction;
		ItemProperty = itemProperty;
		_ownsImage = ownsImage;
		IsGroupedEntry = isGroupedEntry;
	}

	public void Save()
	{
		_saveAction(this);
	}

	public void RefreshThumbnail()
	{
		Thumbnail.Dispose();
		Thumbnail = CreateThumbnailFromInfo(Info);
	}

	private static Bitmap CreateThumbnailFromInfo(WzSubProperty info)
	{
		WzCanvasProperty wzCanvasProperty = MainForm.ResolveCanvas(info, "iconRaw") ?? MainForm.ResolveCanvas(info, "icon");
		if (wzCanvasProperty == null)
		{
			Bitmap bitmap = new Bitmap(48, 48);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Transparent);
			return bitmap;
		}
		using Bitmap original = wzCanvasProperty.PngProperty.GetImage(saveInMemory: false);
		return new Bitmap(original, new Size(48, 48));
	}

	public void Dispose()
	{
		Thumbnail.Dispose();
		if (_ownsImage)
		{
			Image.Dispose();
		}
		_disposeAction?.Invoke();
	}
}
