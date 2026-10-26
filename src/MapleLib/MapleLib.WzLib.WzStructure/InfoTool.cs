using System.Drawing;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.WzStructure;

public static class InfoTool
{
	public static string GetString(this WzImageProperty source)
	{
		return source?.GetString();
	}

	public static WzStringProperty SetString(string value)
	{
		return new WzStringProperty("", value);
	}

	public static string GetOptionalString(this WzImageProperty source)
	{
		return source?.GetString();
	}

	public static WzStringProperty SetOptionalString(string value)
	{
		return (value == null) ? null : SetString(value);
	}

	public static double GetDouble(this WzImageProperty source)
	{
		return source?.GetDouble() ?? 0.0;
	}

	public static WzDoubleProperty SetDouble(double value)
	{
		return new WzDoubleProperty("", value);
	}

	public static int GetInt(this WzImageProperty source, int default_ = 0)
	{
		return source?.GetInt() ?? default_;
	}

	public static WzIntProperty SetInt(int value)
	{
		return new WzIntProperty("", value);
	}

	public static int? GetOptionalInt(this WzImageProperty source, int? default_ = null)
	{
		return (source == null) ? default_ : new int?(source.GetInt());
	}

	public static WzIntProperty SetOptionalInt(int? value)
	{
		return value.HasValue ? SetInt(value.Value) : null;
	}

	public static int? GetOptionalTranslatedInt(this WzImageProperty source)
	{
		string optionalString = source.GetOptionalString();
		if (optionalString == null)
		{
			return null;
		}
		return int.Parse(optionalString);
	}

	public static WzStringProperty SetOptionalTranslatedInt(int? value)
	{
		if (value.HasValue)
		{
			return SetString(value.Value.ToString());
		}
		return null;
	}

	public static Rectangle GetLtRbRectangle(this WzImageProperty parentSource)
	{
		WzVectorProperty optionalVector = parentSource["lt"].GetOptionalVector();
		WzVectorProperty optionalVector2 = parentSource["rb"].GetOptionalVector();
		int width = optionalVector2.X.Value - optionalVector.X.Value;
		int height = optionalVector2.Y.Value - optionalVector.Y.Value;
		return new Rectangle(optionalVector.X.Value, optionalVector.Y.Value, width, height);
	}

	public static void SetLtRbRectangle(this WzImageProperty parentSource, Rectangle rect)
	{
		parentSource["lt"] = SetVector(rect.X, rect.Y);
		parentSource["rb"] = SetVector(rect.X + rect.Width, rect.Y + rect.Height);
	}

	public static WzVectorProperty GetVector(this WzImageProperty source)
	{
		return (WzVectorProperty)source;
	}

	public static WzVectorProperty SetVector(float x, float y)
	{
		return new WzVectorProperty("", x, y);
	}

	public static WzVectorProperty GetOptionalVector(this WzImageProperty source)
	{
		if (source == null)
		{
			return new WzVectorProperty(string.Empty, 0, 0);
		}
		return source.GetVector();
	}

	public static long GetLong(this WzImageProperty source)
	{
		return source.GetLong();
	}

	public static WzLongProperty SetLong(long value)
	{
		return new WzLongProperty("", value);
	}

	public static long? GetOptionalLong(this WzImageProperty source)
	{
		return source?.GetLong();
	}

	public static WzLongProperty SetOptionalLong(long? value)
	{
		return value.HasValue ? SetLong(value.Value) : null;
	}

	public static bool GetBool(this WzImageProperty source)
	{
		if (source == null)
		{
			return false;
		}
		return source.GetInt() == 1;
	}

	public static WzIntProperty SetBool(bool value)
	{
		return new WzIntProperty("", value ? 1 : 0);
	}

	public static MapleBool GetOptionalBool(this WzImageProperty source)
	{
		if (source == null)
		{
			return (byte)0;
		}
		return source.GetInt() == 1;
	}

	public static WzIntProperty SetOptionalBool(this MapleBool value)
	{
		if (value.HasValue && value.Value)
		{
			return SetBool(value.Value);
		}
		return null;
	}

	public static float GetFloat(this WzImageProperty source)
	{
		return source?.GetFloat() ?? 0f;
	}

	public static WzFloatProperty SetFloat(float value)
	{
		return new WzFloatProperty("", value);
	}

	public static float? GetOptionalFloat(this WzImageProperty source)
	{
		return source?.GetFloat();
	}

	public static WzFloatProperty SetOptionalFloat(float? value)
	{
		return value.HasValue ? SetFloat(value.Value) : null;
	}
}
