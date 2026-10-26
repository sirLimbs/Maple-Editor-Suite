using System;
using System.Drawing;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleMobEditor;

internal sealed class ImgItemEntry : IDisposable
{
	private readonly Action<ImgItemEntry> _saveAction;

	private readonly Action? _disposeAction;

	private readonly bool _ownsImage;

	public string FilePath { get; }

	public WzImage Image { get; }

	public WzSubProperty Info { get; }

	public byte[] Iv { get; }

	public string IvLabel { get; }

	public GameFileInfo Detection { get; }

	public string ItemId { get; }

	public string DisplayName { get; }

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
		Thumbnail = CreateThumbnailFromImage(Image);
	}

	private static Bitmap CreateThumbnailFromImage(WzImage image)
	{
		return MainForm.CreateThumbnail(image);
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
