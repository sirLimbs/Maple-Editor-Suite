using System;
using System.Globalization;
using System.IO;
using System.Text;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzStringProperty : WzImageProperty
{
	internal string name;

	internal string val;

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

	public override WzPropertyType PropertyType => WzPropertyType.String;

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

	public string Value
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

	public bool IsSpineRelatedResources => name.EndsWith(".atlas") || name.EndsWith(".json") || name.EndsWith(".skel");

	public bool IsSpineAtlasResources => name.EndsWith(".atlas");

	public override void SetValue(object value)
	{
		val = (string)value;
	}

	public override WzImageProperty DeepClone()
	{
		return new WzStringProperty(name, val);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)8);
		writer.WriteStringValue(Value, 0, 1);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzString", Name, Value));
	}

	public override void Dispose()
	{
		name = null;
		val = null;
	}

	public WzStringProperty()
	{
	}

	public WzStringProperty(string name)
	{
		this.name = name;
	}

	public WzStringProperty(string name, string value)
	{
		this.name = name;
		val = value;
	}

	public DateTime? GetDateTime()
	{
		string text = GetString();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string text2;
		if (text.Length == 10)
		{
			text2 = "yyyyMMddHH";
		}
		else
		{
			text2 = ((text.Length == 12) ? "yyyyMMddHHmm" : null);
		}
		if (text2 == null)
		{
			return null;
		}
		if (DateTime.TryParseExact(text, text2, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		return null;
	}

	public void SetDateValue(DateTime value)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(value.Year.ToString().PadLeft(4, '0'));
		stringBuilder.Append(value.Month.ToString().PadLeft(2, '0'));
		stringBuilder.Append(value.Day.ToString().PadLeft(2, '0'));
		stringBuilder.Append(value.Hour.ToString().PadLeft(2, '0'));
		if (value.Minute != 0)
		{
			stringBuilder.Append(value.Minute.ToString().PadLeft(2, '0'));
		}
		Value = stringBuilder.ToString();
	}

	public override int GetInt()
	{
		int result = 0;
		int.TryParse(val, out result);
		return result;
	}

	public override short GetShort()
	{
		short result = 0;
		short.TryParse(val, out result);
		return result;
	}

	public override long GetLong()
	{
		long result = 0L;
		long.TryParse(val, out result);
		return result;
	}

	public override string GetString()
	{
		return val;
	}

	public override string ToString()
	{
		return val;
	}
}
