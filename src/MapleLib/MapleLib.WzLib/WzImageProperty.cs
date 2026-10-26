using System;
using System.Collections.Generic;
using System.IO;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public abstract class WzImageProperty : WzObject
{
	public virtual WzPropertyCollection WzProperties => null;

	public new virtual WzImageProperty this[string name]
	{
		get
		{
			return null;
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public abstract WzPropertyType PropertyType { get; }

	public WzImage ParentImage
	{
		get
		{
			for (WzObject parent = Parent; parent != null; parent = parent.Parent)
			{
				if (parent is WzImage)
				{
					return (WzImage)parent;
				}
			}
			return null;
		}
	}

	public override WzObjectType ObjectType => WzObjectType.Property;

	public override WzFile WzFileParent => ParentImage?.WzFileParent;

	public virtual WzImageProperty GetFromPath(string path)
	{
		return null;
	}

	public abstract void WriteValue(WzBinaryWriter writer);

	public abstract WzImageProperty DeepClone();

	public abstract void SetValue(object value);

	public override void Remove()
	{
		((IPropertyContainer)Parent).RemoveProperty(this);
	}

	public virtual void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag(PropertyType.ToString(), Name, finish: true));
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag(PropertyType.ToString()));
	}

	internal static void WritePropertyList(WzBinaryWriter writer, List<WzImageProperty> properties)
	{
		if (properties.Count == 1 && properties[0] is WzLuaProperty)
		{
			properties[0].WriteValue(writer);
			return;
		}
		writer.Write((ushort)0);
		writer.WriteCompressedInt(properties.Count);
		foreach (WzImageProperty property2 in properties)
		{
			writer.WriteStringValue(property2.Name, 0, 1);
			if (property2 is WzExtended property)
			{
				WriteExtendedValue(writer, property);
			}
			else
			{
				property2.WriteValue(writer);
			}
		}
	}

	internal static void DumpPropertyList(StreamWriter writer, int level, List<WzImageProperty> properties)
	{
		foreach (WzImageProperty property in properties)
		{
			property.ExportXml(writer, level + 1);
		}
	}

	internal static WzLuaProperty ParseLuaProperty(long offset, WzBinaryReader reader, WzObject parent, WzImage parentImg)
	{
		int count = reader.ReadCompressedInt();
		byte[] encryptedBytes = reader.ReadBytes(count);
		return new WzLuaProperty("Script", encryptedBytes)
		{
			Parent = parent
		};
	}

	internal static WzPropertyCollection ParsePropertyList(long offset, WzBinaryReader reader, WzObject parent, WzImage parentImg)
	{
		int num = reader.ReadCompressedInt();
		WzPropertyCollection wzPropertyCollection = new WzPropertyCollection(parent);
		for (int i = 0; i < num; i++)
		{
			string text = reader.ReadStringBlock(offset);
			byte b = reader.ReadByte();
			switch (b)
			{
			case 0:
				wzPropertyCollection.Add(new WzNullProperty(text)
				{
					Parent = parent
				});
				break;
			case 2:
			case 11:
				wzPropertyCollection.Add(new WzShortProperty(text, reader.ReadInt16())
				{
					Parent = parent
				});
				break;
			case 3:
			case 19:
				wzPropertyCollection.Add(new WzIntProperty(text, reader.ReadCompressedInt())
				{
					Parent = parent
				});
				break;
			case 20:
				wzPropertyCollection.Add(new WzLongProperty(text, reader.ReadLong())
				{
					Parent = parent
				});
				break;
			case 4:
				switch (reader.ReadByte())
				{
				case 128:
					wzPropertyCollection.Add(new WzFloatProperty(text, reader.ReadSingle())
					{
						Parent = parent
					});
					break;
				case 0:
					wzPropertyCollection.Add(new WzFloatProperty(text, 0f)
					{
						Parent = parent
					});
					break;
				}
				break;
			case 5:
				wzPropertyCollection.Add(new WzDoubleProperty(text, reader.ReadDouble())
				{
					Parent = parent
				});
				break;
			case 8:
				wzPropertyCollection.Add(new WzStringProperty(text, reader.ReadStringBlock(offset))
				{
					Parent = parent
				});
				break;
			case 9:
			{
				long num2 = reader.ReadUInt32() + reader.BaseStream.Position;
				WzImageProperty item = ParseExtendedProp(reader, offset, num2, text, parent, parentImg);
				wzPropertyCollection.Add(item);
				if (reader.BaseStream.Position != num2)
				{
					reader.BaseStream.Position = num2;
				}
				break;
			}
			default:
				throw new Exception("Unknown property type at ParsePropertyList, ptype = " + b);
			}
		}
		return wzPropertyCollection;
	}

	internal static WzExtended ParseExtendedProp(WzBinaryReader reader, long offset, long endOfBlock, string name, WzObject parent, WzImage imgParent)
	{
		byte b = reader.ReadByte();
		byte b2 = b;
		if ((uint)b2 <= 1u)
		{
			if (b2 == 0)
			{
				goto IL_0046;
			}
			if (b2 == 1)
			{
				goto IL_0027;
			}
		}
		else
		{
			if (b2 == 27)
			{
				goto IL_0027;
			}
			if (b2 == 115)
			{
				goto IL_0046;
			}
		}
		throw new Exception("Invalid byte read at ParseExtendedProp");
		IL_0027:
		return ExtractMore(reader, offset, endOfBlock, name, reader.ReadStringAtOffset(offset + reader.ReadInt32()), parent, imgParent);
		IL_0046:
		return ExtractMore(reader, offset, endOfBlock, name, "", parent, imgParent);
	}

	internal static WzExtended ExtractMore(WzBinaryReader reader, long offset, long eob, string name, string iname, WzObject parent, WzImage imgParent)
	{
		if (iname == "")
		{
			iname = reader.ReadString();
		}
		switch (iname)
		{
		case "Property":
		{
			WzSubProperty wzSubProperty = new WzSubProperty(name)
			{
				Parent = parent
			};
			reader.BaseStream.Position += 2L;
			wzSubProperty.AddProperties(ParsePropertyList(offset, reader, wzSubProperty, imgParent));
			return wzSubProperty;
		}
		case "Canvas":
		{
			WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(name)
			{
				Parent = parent
			};
			reader.BaseStream.Position++;
			if (reader.ReadByte() == 1)
			{
				reader.BaseStream.Position += 2L;
				wzCanvasProperty.AddProperties(ParsePropertyList(offset, reader, wzCanvasProperty, imgParent));
			}
			wzCanvasProperty.PngProperty = new WzPngProperty(reader, imgParent.ParseEverything)
			{
				Parent = wzCanvasProperty
			};
			return wzCanvasProperty;
		}
		case "Shape2D#Vector2D":
		{
			WzVectorProperty wzVectorProperty = new WzVectorProperty(name)
			{
				Parent = parent
			};
			wzVectorProperty.X = new WzIntProperty("X", reader.ReadCompressedInt())
			{
				Parent = wzVectorProperty
			};
			wzVectorProperty.Y = new WzIntProperty("Y", reader.ReadCompressedInt())
			{
				Parent = wzVectorProperty
			};
			return wzVectorProperty;
		}
		case "Shape2D#Convex2D":
		{
			WzConvexProperty wzConvexProperty = new WzConvexProperty(name)
			{
				Parent = parent
			};
			int num = reader.ReadCompressedInt();
			wzConvexProperty.WzProperties.Capacity = num;
			for (int i = 0; i < num; i++)
			{
				wzConvexProperty.AddProperty(ParseExtendedProp(reader, offset, 0L, name, wzConvexProperty, imgParent));
			}
			return wzConvexProperty;
		}
		case "Sound_DX8":
			return new WzBinaryProperty(name, reader, imgParent.ParseEverything)
			{
				Parent = parent
			};
		case "UOL":
			reader.BaseStream.Position++;
			return reader.ReadByte() switch
			{
				0 => (WzExtended)new WzUOLProperty(name, reader.ReadString())
				{
					Parent = parent
				}, 
				1 => new WzUOLProperty(name, reader.ReadStringAtOffset(offset + reader.ReadInt32()))
				{
					Parent = parent
				}, 
				_ => throw new Exception("Unsupported UOL type"), 
			};
		case "RawData":
		{
			byte b = reader.ReadByte();
			WzRawDataProperty wzRawDataProperty = new WzRawDataProperty(name, reader, b)
			{
				Parent = parent
			};
			if (b == 1 && reader.ReadByte() == 1)
			{
				reader.BaseStream.Position += 2L;
				wzRawDataProperty.AddProperties(ParsePropertyList(offset, reader, wzRawDataProperty, imgParent));
			}
			wzRawDataProperty.Parse(imgParent.ParseEverything);
			return wzRawDataProperty;
		}
		case "Canvas#Video":
		{
			WzVideoProperty wzVideoProperty = new WzVideoProperty(name, reader);
			wzVideoProperty.Parent = parent;
			reader.BaseStream.Position++;
			if (reader.ReadByte() == 1)
			{
				reader.BaseStream.Position += 2L;
				wzVideoProperty.AddProperties(ParsePropertyList(offset, reader, wzVideoProperty, imgParent));
			}
			wzVideoProperty.Parse(imgParent.ParseEverything);
			return wzVideoProperty;
		}
		default:
			throw new Exception("Unknown iname: " + iname);
		}
	}

	internal static void WriteExtendedValue(WzBinaryWriter writer, WzExtended property)
	{
		writer.Write((byte)9);
		long position = writer.BaseStream.Position;
		writer.Write(0);
		property.WriteValue(writer);
		int num = (int)(writer.BaseStream.Position - position);
		long position2 = writer.BaseStream.Position;
		writer.BaseStream.Position = position;
		writer.Write(num - 4);
		writer.BaseStream.Position = position2;
	}

	public WzImageProperty GetLinkedWzImageProperty()
	{
		WzImageProperty wzImageProperty = this;
		while (wzImageProperty is WzUOLProperty)
		{
			if ((wzImageProperty as WzUOLProperty).LinkValue is WzImageProperty wzImageProperty2)
			{
				wzImageProperty = wzImageProperty2;
				continue;
			}
			return this;
		}
		return wzImageProperty;
	}
}
