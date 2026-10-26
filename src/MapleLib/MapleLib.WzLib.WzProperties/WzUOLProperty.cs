using System.Drawing;
using System.IO;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzUOLProperty : WzExtended
{
	internal string name;

	internal string val;

	internal WzObject parent;

	internal WzObject linkVal;

	public override object WzValue => LinkValue;

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

	public override WzPropertyCollection WzProperties => (LinkValue is WzImageProperty) ? ((WzImageProperty)LinkValue).WzProperties : null;

	public override WzImageProperty this[string name]
	{
		get
		{
			WzImageProperty result;
			if (LinkValue is WzImageProperty)
			{
				result = ((WzImageProperty)LinkValue)[name];
			}
			else
			{
				result = ((LinkValue is WzImage) ? ((WzImage)LinkValue)[name] : null);
			}
			return result;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.UOL;

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

	public WzObject LinkValue
	{
		get
		{
			if (linkVal == null)
			{
				string[] array = val.Split('/');
				if (array.Length >= 1)
				{
					if (array[0] != "..")
					{
						linkVal = GetTopMostWzImage();
					}
					else
					{
						linkVal = parent;
					}
					string fullPath = parent.FullPath;
					string[] array2 = array;
					foreach (string text in array2)
					{
						if (text == "..")
						{
							linkVal = linkVal.Parent;
							continue;
						}
						if (linkVal is WzImageProperty)
						{
							linkVal = ((WzImageProperty)linkVal)[text];
							continue;
						}
						if (linkVal is WzImage wzImage)
						{
							linkVal = wzImage[text];
							continue;
						}
						if (linkVal is WzDirectory wzDirectory)
						{
							if (text.EndsWith(".img"))
							{
								linkVal = wzDirectory[text];
							}
							else
							{
								linkVal = wzDirectory[text + ".img"];
							}
							continue;
						}
						ErrorLogger.Log(ErrorLevel.Critical, "UOL got nexon'd at property: " + FullPath);
						return null;
					}
				}
			}
			return linkVal;
		}
	}

	public override void SetValue(object value)
	{
		val = (string)value;
	}

	public override WzImageProperty DeepClone()
	{
		WzUOLProperty wzUOLProperty = new WzUOLProperty(name, val);
		wzUOLProperty.linkVal = null;
		return wzUOLProperty;
	}

	public override WzImageProperty GetFromPath(string path)
	{
		WzImageProperty result;
		if (LinkValue is WzImageProperty)
		{
			result = ((WzImageProperty)LinkValue).GetFromPath(path);
		}
		else
		{
			result = ((LinkValue is WzImage) ? ((WzImage)LinkValue).GetFromPath(path) : null);
		}
		return result;
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.WriteStringValue("UOL", 115, 27);
		writer.Write((byte)0);
		writer.WriteStringValue(Value, 0, 1);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzUOL", Name, Value));
	}

	public override void Dispose()
	{
		name = null;
		val = null;
	}

	public WzUOLProperty()
	{
	}

	public WzUOLProperty(string name)
	{
		this.name = name;
	}

	public WzUOLProperty(string name, string value)
	{
		this.name = name;
		val = value;
	}

	public override int GetInt()
	{
		return LinkValue.GetInt();
	}

	public override short GetShort()
	{
		return LinkValue.GetShort();
	}

	public override long GetLong()
	{
		return LinkValue.GetLong();
	}

	public override float GetFloat()
	{
		return LinkValue.GetFloat();
	}

	public override double GetDouble()
	{
		return LinkValue.GetDouble();
	}

	public override string GetString()
	{
		return LinkValue.GetString();
	}

	public override Point GetPoint()
	{
		return LinkValue.GetPoint();
	}

	public override Bitmap GetBitmap()
	{
		return LinkValue.GetBitmap();
	}

	public override byte[] GetBytes()
	{
		return LinkValue.GetBytes();
	}

	public override string ToString()
	{
		return val;
	}
}
