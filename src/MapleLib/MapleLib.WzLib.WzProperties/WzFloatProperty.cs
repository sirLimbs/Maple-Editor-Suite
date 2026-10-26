using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzFloatProperty : WzImageProperty
{
	internal string name;

	internal float val;

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

	public override WzPropertyType PropertyType => WzPropertyType.Float;

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

	public float Value
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
		val = Convert.ToSingle(value);
	}

	public override WzImageProperty DeepClone()
	{
		return new WzFloatProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)4);
		if (Value == 0f)
		{
			writer.Write((byte)0);
			return;
		}
		writer.Write((byte)128);
		writer.Write(Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzByteFloat", Name, Value.ToString()));
	}

	public override void Dispose()
	{
		name = null;
	}

	public WzFloatProperty()
	{
	}

	public WzFloatProperty(string name)
	{
		this.name = name;
	}

	public WzFloatProperty(string name, float value)
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
