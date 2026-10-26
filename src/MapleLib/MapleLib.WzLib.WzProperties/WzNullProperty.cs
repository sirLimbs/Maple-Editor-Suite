using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzNullProperty : WzImageProperty
{
	internal string name;

	internal WzObject parent;

	public override WzObject Parent
	{
		get
		{
			return parent;
		}
		internal set
		{
			parent = value;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.Null;

	public override string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public override WzObjectType ObjectType => WzObjectType.Property;

	public override void SetValue(object value)
	{
		throw new NotImplementedException();
	}

	public override WzImageProperty DeepClone()
	{
		return new WzNullProperty(name);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)0);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedTag("WzNull", Name));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzNullProperty()
	{
	}

	public WzNullProperty(string propName)
	{
		name = propName;
	}
}
