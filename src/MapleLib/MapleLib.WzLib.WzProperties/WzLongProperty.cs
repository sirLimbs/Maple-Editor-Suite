using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzLongProperty : WzImageProperty
{
	internal string name;

	internal long val;

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

	public override WzPropertyType PropertyType => WzPropertyType.Long;

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

	public long Value
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
		val = Convert.ToInt64(value);
	}

	public override WzImageProperty DeepClone()
	{
		return new WzLongProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)20);
		writer.WriteCompressedLong(Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzLong", Name, Value.ToString()));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzLongProperty()
	{
	}

	public WzLongProperty(string name)
	{
		this.name = name;
	}

	public WzLongProperty(string name, long value)
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

	public override long GetLong()
	{
		return val;
	}

	public override int GetInt()
	{
		return (int)val;
	}

	public override short GetShort()
	{
		return (short)val;
	}

	public override string ToString()
	{
		return val.ToString();
	}
}
