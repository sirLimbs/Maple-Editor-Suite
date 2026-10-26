using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.WzProperties;
using Spine;

namespace MapleLib.WzLib.Spine;

public class WzSpineAtlasLoader
{
	public static SkeletonData LoadSkeleton(WzStringProperty atlasNode, TextureLoader textureLoader)
	{
		string text = atlasNode.GetString();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		StringReader reader = new StringReader(text);
		Atlas atlas = new Atlas(reader, string.Empty, textureLoader);
		if (!TryLoadSkeletonJsonOrBinary(atlasNode, atlas, out var data))
		{
			atlas.Dispose();
			return null;
		}
		return data;
	}

	private static bool TryLoadSkeletonJsonOrBinary(WzImageProperty atlasNode, Atlas atlas, out SkeletonData data)
	{
		data = null;
		if (atlasNode == null || atlasNode.Parent == null || atlas == null)
		{
			return false;
		}
		WzObject parent = atlasNode.Parent;
		List<WzImageProperty> list = ((!(parent is WzImageProperty)) ? ((WzImage)parent).WzProperties : ((WzImageProperty)parent).WzProperties);
		if (list != null)
		{
			WzStringProperty wzStringProperty = (WzStringProperty)list.Where((WzImageProperty child) => child.Name.EndsWith(".json")).FirstOrDefault();
			if (wzStringProperty != null)
			{
				StringReader reader = new StringReader(wzStringProperty.GetString());
				SkeletonJson skeletonJson = new SkeletonJson(atlas);
				data = skeletonJson.ReadSkeletonData(reader);
				return true;
			}
			foreach (WzImageProperty item in list)
			{
				WzImageProperty linkedWzImageProperty = item.GetLinkedWzImageProperty();
				if (linkedWzImageProperty is WzBinaryProperty wzBinaryProperty)
				{
					using MemoryStream input = new MemoryStream(wzBinaryProperty.GetBytes(saveInMemory: false));
					SkeletonBinary skeletonBinary = new SkeletonBinary(atlas);
					data = skeletonBinary.ReadSkeletonData(input);
					return true;
				}
			}
		}
		return false;
	}
}
