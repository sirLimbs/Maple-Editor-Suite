using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzIntProperty : WzImageProperty
{
	internal string name;

	internal int val;

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

	public override WzPropertyType PropertyType => WzPropertyType.Int;

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

	public int Value
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
		val = Convert.ToInt32(value);
	}

	public override WzImageProperty DeepClone()
	{
		return new WzIntProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)3);
		writer.WriteCompressedInt(Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzCompressedInt", Name, Value.ToString()));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzIntProperty()
	{
	}

	public WzIntProperty(string name)
	{
		this.name = name;
	}

	public WzIntProperty(string name, int value)
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
		return (short)val;
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
