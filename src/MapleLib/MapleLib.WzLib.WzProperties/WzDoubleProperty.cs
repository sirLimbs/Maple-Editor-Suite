using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzDoubleProperty : WzImageProperty
{
	internal string name;

	internal double val;

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

	public override WzPropertyType PropertyType => WzPropertyType.Double;

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

	public double Value
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
		val = Convert.ToDouble(value);
	}

	public override WzImageProperty DeepClone()
	{
		return new WzDoubleProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)5);
		writer.Write(Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzDouble", Name, Value.ToString()));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzDoubleProperty()
	{
	}

	public WzDoubleProperty(string name)
	{
		this.name = name;
	}

	public WzDoubleProperty(string name, double value)
	{
		this.name = name;
		val = value;
	}

	public override float GetFloat()
	{
		return (float)val;
	}

	public override double GetDouble()
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

	public override long GetLong()
	{
		return (long)val;
	}

	public override string ToString()
	{
		return val.ToString();
	}
}
