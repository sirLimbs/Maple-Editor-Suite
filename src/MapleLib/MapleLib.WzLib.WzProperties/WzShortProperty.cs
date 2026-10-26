using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzShortProperty : WzImageProperty
{
	internal string name;

	internal short val;

	internal WzObject parent;

	public override object WzValue => Value;

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

	public override WzPropertyType PropertyType => WzPropertyType.Short;

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

	public short Value
	{
		get
		{
			return val;
		}
		set
		{
			val = value;
		}
	}

	public override void SetValue(object value)
	{
		val = (short)value;
	}

	public override WzImageProperty DeepClone()
	{
		return new WzShortProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)2);
		writer.Write(Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzUnsignedShort", Name, Value.ToString()));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzShortProperty()
	{
	}

	public WzShortProperty(string name)
	{
		this.name = name;
	}

	public WzShortProperty(string name, short value)
	{
		this.name = name;
		val = value;
	}

	public override float GetFloat()
	{
		return val;
	}

	public override double GetDouble()
	{
		return val;
	}

	public override int GetInt()
	{
		return val;
	}

	public override short GetShort()
	{
		return val;
	}

	public override long GetLong()
	{
		return val;
	}

	public override string ToString()
	{
		return val.ToString();
	}
}
