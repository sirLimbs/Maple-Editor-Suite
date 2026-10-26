using MapleLib.WzLib.WzProperties;
using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace MapleLib.WzLib.Spine;

public class WzSpineTextureLoader : TextureLoader
{
	private readonly GraphicsDevice graphicsDevice;

	public WzObject ParentNode { get; private set; }

	public WzSpineTextureLoader(WzObject ParentNode, GraphicsDevice graphicsDevice)
	{
		this.ParentNode = ParentNode;
		this.graphicsDevice = graphicsDevice;
	}

	public void Load(AtlasPage page, string path)
	{
		WzObject wzObject = ParentNode[path];
		if (wzObject == null)
		{
			return;
		}
		WzCanvasProperty wzCanvasProperty = null;
		WzImageProperty wzImageProperty = (WzImageProperty)ParentNode[path];
		if (wzImageProperty is WzUOLProperty { LinkValue: var linkValue })
		{
			if (linkValue is WzCanvasProperty wzCanvasProperty2)
			{
				wzCanvasProperty = wzCanvasProperty2;
			}
		}
		else if (wzImageProperty is WzCanvasProperty wzCanvasProperty3)
		{
			wzCanvasProperty = wzCanvasProperty3;
		}
		if (wzCanvasProperty != null && graphicsDevice != null)
		{
			WzCanvasProperty wzCanvasProperty4 = (WzCanvasProperty)wzCanvasProperty.GetLinkedWzImageProperty();
			WzPngProperty pngProperty = wzCanvasProperty4.PngProperty;
			Texture2D texture2D = new Texture2D(graphicsDevice, pngProperty.Width, pngProperty.Height, mipmap: false, wzCanvasProperty4.PngProperty.Format.GetXNASurfaceFormat());
			pngProperty.ParsePng(saveInMemory: true, texture2D);
			page.rendererObject = texture2D;
			page.width = pngProperty.Width;
			page.height = pngProperty.Height;
		}
	}

	public void Unload(object texture)
	{
		(texture as Texture2D)?.Dispose();
	}
}
