using System.Collections.Generic;

namespace MapleLib.WzLib;

public class WzPropertyCollection : List<WzImageProperty>
{
	private WzObject parent;

	public WzPropertyCollection(WzObject parent)
	{
		this.parent = parent;
	}

	public new void Add(WzImageProperty item)
	{
		if (parent != null)
		{
			item.Parent = parent;
		}
		base.Add(item);
	}

	public new void AddRange(IEnumerable<WzImageProperty> collection)
	{
		foreach (WzImageProperty item in collection)
		{
			Add(item);
		}
	}

	public new void Insert(int index, WzImageProperty item)
	{
		if (parent != null)
		{
			item.Parent = parent;
		}
		base.Insert(index, item);
	}
}
