#define DEBUG
using System.Diagnostics;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleLib;

public static class WzDataReader
{
	public static string ReadString(this WzImageProperty value, string fallback)
	{
		if (value == null || value.GetType() == typeof(WzNullProperty))
		{
			return fallback;
		}
		if (value != null)
		{
			if (value.PropertyType == WzPropertyType.String)
			{
				return ((WzStringProperty)value).Value;
			}
			if (value.PropertyType == WzPropertyType.Int)
			{
				return ((WzIntProperty)value).Value.ToString();
			}
		}
		return ((WzStringProperty)value).Value;
	}

	public static WzVectorProperty ReadVector(this WzImageProperty value)
	{
		if (value == null)
		{
			return null;
		}
		return (WzVectorProperty)value;
	}

	public static long ReadLong(this WzImageProperty value, long def = 0L)
	{
		if (value == null)
		{
			return 0L;
		}
		if (value.PropertyType == WzPropertyType.Int)
		{
			return ((WzIntProperty)value).Value;
		}
		if (value.PropertyType == WzPropertyType.Long)
		{
			return ((WzLongProperty)value).Value;
		}
		if (value.PropertyType == WzPropertyType.String)
		{
			try
			{
				return long.Parse(((WzStringProperty)value).Value);
			}
			catch
			{
				Debug.WriteLine("Error parsing string to long: " + ((WzStringProperty)value).Value);
				return def;
			}
		}
		return 0L;
	}

	public static int ReadValue(this WzSubProperty value)
	{
		return ((WzImageProperty)value).ReadValue();
	}

	public static int ReadValue(this WzImageProperty value)
	{
		if (value == null)
		{
			return 0;
		}
		if (value.PropertyType == WzPropertyType.Int)
		{
			return ((WzIntProperty)value).Value;
		}
		if (value.PropertyType == WzPropertyType.String)
		{
			return int.Parse(((WzStringProperty)value).Value);
		}
		return 0;
	}

	public static int ReadValue(this WzSubProperty value, int def)
	{
		return ((WzImageProperty)value).ReadValue(def);
	}

	public static int ReadValue(this WzImageProperty value, int def)
	{
		if (value == null)
		{
			return def;
		}
		if (value.PropertyType == WzPropertyType.Int)
		{
			return ((WzIntProperty)value).Value;
		}
		if (value.PropertyType == WzPropertyType.String)
		{
			string value2 = ((WzStringProperty)value).Value;
			try
			{
				return int.Parse(value2);
			}
			catch
			{
				if (value2.EndsWith("%"))
				{
					return int.Parse(value2.Substring(0, value2.Length - 1));
				}
			}
		}
		return def;
	}
}
