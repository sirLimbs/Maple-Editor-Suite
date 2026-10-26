using MapleLib.WzLib.WzProperties;
using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace MapleLib.WzLib.Spine;

public class WzSpineAnimationItem
{
	private readonly WzStringProperty wzSpineAtlasPropertyNode;

	public bool PremultipliedAlpha { get; set; }

	public SkeletonData SkeletonData { get; private set; }

	public WzSpineAnimationItem(WzStringProperty wzSpineAtlasPropertyNode)
	{
		this.wzSpineAtlasPropertyNode = wzSpineAtlasPropertyNode;
	}

	public void LoadResources(GraphicsDevice graphicsDevice)
	{
		WzSpineTextureLoader textureLoader = new WzSpineTextureLoader(wzSpineAtlasPropertyNode.Parent, graphicsDevice);
		SkeletonData skeletonData = WzSpineAtlasLoader.LoadSkeleton(wzSpineAtlasPropertyNode, textureLoader);
		if (skeletonData != null)
		{
			bool premultipliedAlpha = ((!(wzSpineAtlasPropertyNode.parent is WzImageProperty wzImageProperty)) ? (((WzImage)wzSpineAtlasPropertyNode.parent)["PMA"].ReadValue(0) > 0) : (wzImageProperty["PMA"].ReadValue(0) > 0));
			SkeletonData = skeletonData;
			PremultipliedAlpha = premultipliedAlpha;
		}
	}
}
