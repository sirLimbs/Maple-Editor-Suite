#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public class WzImage : WzObject, IPropertyContainer
{
	public const int WzImageHeaderByte_WithoutOffset = 115;

	public const int WzImageHeaderByte_WithOffset = 27;

	internal bool parsed = false;

	internal bool bIsImageChanged = false;

	internal string name;

	internal int size;

	private int checksum;

	internal long offset = 0L;

	internal WzBinaryReader reader;

	internal WzPropertyCollection properties;

	internal WzObject parent;

	internal int blockStart = 0;

	internal long tempFileStart = 0L;

	internal long tempFileEnd = 0L;

	private bool parseEverything = false;

	public bool IsLuaWzImage => Name.EndsWith(".lua");

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

	public override WzFile WzFileParent => (Parent != null) ? Parent.WzFileParent : null;

	public bool Parsed
	{
		get
		{
			return parsed;
		}
		set
		{
			parsed = value;
		}
	}

	public bool ParseEverything
	{
		get
		{
			return parseEverything;
		}
		set
		{
			parseEverything = value;
		}
	}

	public bool Changed
	{
		get
		{
			return bIsImageChanged;
		}
		set
		{
			bIsImageChanged = value;
		}
	}

	public int BlockSize
	{
		get
		{
			return size;
		}
		set
		{
			size = value;
		}
	}

	public int Checksum
	{
		get
		{
			return checksum;
		}
		private set
		{
		}
	}

	public long Offset
	{
		get
		{
			return offset;
		}
		set
		{
			offset = value;
		}
	}

	public int BlockStart => blockStart;

	public override WzObjectType ObjectType
	{
		get
		{
			if (reader != null && !parsed)
			{
				ParseImage();
			}
			return WzObjectType.Image;
		}
	}

	public WzPropertyCollection WzProperties
	{
		get
		{
			if (reader != null && !parsed)
			{
				ParseImage();
			}
			return properties;
		}
	}

	public new WzImageProperty this[string name]
	{
		get
		{
			if (reader != null && !parsed)
			{
				ParseImage();
			}
			return properties.FirstOrDefault((WzImageProperty iwp) => iwp.Name.ToLower() == name.ToLower());
		}
		set
		{
			if (value != null)
			{
				value.Name = name;
				AddProperty(value);
			}
		}
	}

	public byte[] DataBlock
	{
		get
		{
			byte[] result = null;
			if (reader != null && size > 0)
			{
				result = reader.ReadBytes(size);
				reader.BaseStream.Position = blockStart;
			}
			return result;
		}
	}

	public WzImage()
	{
		properties = new WzPropertyCollection(this);
	}

	public WzImage(string name)
	{
		this.name = name;
		Changed = true;
		properties = new WzPropertyCollection(this);
	}

	public WzImage(string name, Stream dataStream, WzMapleVersion mapleVersion)
	{
		this.name = name;
		reader = new WzBinaryReader(dataStream, WzTool.GetIvByMapleVersion(mapleVersion), 0L);
		properties = new WzPropertyCollection(this);
	}

	internal WzImage(string name, WzBinaryReader reader)
	{
		this.name = name;
		this.reader = reader;
		blockStart = (int)reader.BaseStream.Position;
		checksum = 0;
		properties = new WzPropertyCollection(this);
	}

	internal WzImage(string name, WzBinaryReader reader, int checksum)
	{
		this.name = name;
		this.reader = reader;
		blockStart = (int)reader.BaseStream.Position;
		this.checksum = checksum;
		properties = new WzPropertyCollection(this);
	}

	public override void Dispose()
	{
		name = null;
		if (properties != null)
		{
			foreach (WzImageProperty property in properties)
			{
				property.Dispose();
			}
			properties.Clear();
			properties = null;
		}
		if (reader != null)
		{
			reader.Close();
			reader.Dispose();
			reader = null;
		}
	}

	public WzImage DeepClone()
	{
		if (reader != null && !parsed)
		{
			ParseImage();
		}
		WzImage wzImage = new WzImage(name)
		{
			bIsImageChanged = true
		};
		foreach (WzImageProperty property in properties)
		{
			wzImage.AddProperty(property.DeepClone());
		}
		return wzImage;
	}

	public WzImageProperty GetFromPath(string path)
	{
		if (reader != null && !parsed)
		{
			ParseImage();
		}
		string[] array = path.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (array[0] == "..")
		{
			return null;
		}
		WzImageProperty wzImageProperty = null;
		string[] array2 = array;
		foreach (string segment in array2)
		{
			wzImageProperty = ((wzImageProperty == null) ? properties : wzImageProperty.WzProperties).FirstOrDefault((WzImageProperty iwp) => iwp.Name == segment);
			if (wzImageProperty == null)
			{
				return null;
			}
		}
		return wzImageProperty;
	}

	public void AddProperty(WzImageProperty prop)
	{
		if (this[prop.Name] != null)
		{
			throw new Exception($"Trying to add a WzImageProperty with a name ['{prop.Name}'] that is already being used by another property.");
		}
		if (reader != null && !parsed)
		{
			ParseImage();
		}
		properties.Add(prop);
		Changed = true;
	}

	public void AddProperties(WzPropertyCollection props)
	{
		foreach (WzImageProperty prop in props)
		{
			AddProperty(prop);
		}
	}

	public void RemoveProperty(WzImageProperty prop)
	{
		if (properties.Contains(prop))
		{
			if (reader != null && !parsed)
			{
				ParseImage();
			}
			prop.Parent = null;
			properties.Remove(prop);
			Changed = true;
		}
	}

	public void RemoveProperty(string propertyName)
	{
		WzImageProperty wzImageProperty = this[propertyName];
		if (wzImageProperty != null)
		{
			RemoveProperty(wzImageProperty);
		}
	}

	public void ClearProperties()
	{
		foreach (WzImageProperty property in properties)
		{
			property.Parent = null;
		}
		properties.Clear();
		Changed = true;
	}

	public override void Remove()
	{
		if (Parent != null)
		{
			((WzDirectory)Parent).RemoveImage(this);
		}
	}

	internal void CalculateAndSetImageChecksum(byte[] bytes)
	{
		checksum = 0;
		foreach (byte b in bytes)
		{
			checksum += b;
		}
	}

	public bool ParseImage(bool forceReadFromData = false)
	{
		if (!forceReadFromData)
		{
			if (Parsed)
			{
				return true;
			}
			if (Changed)
			{
				Parsed = true;
				return true;
			}
		}
		lock (reader)
		{
			long position = reader.BaseStream.Position;
			reader.BaseStream.Position = offset;
			byte b = reader.ReadByte();
			switch (b)
			{
			case 1:
				if (IsLuaWzImage)
				{
					WzLuaProperty item = WzImageProperty.ParseLuaProperty(offset, reader, this, this);
					List<WzImageProperty> collection2 = new List<WzImageProperty> { item };
					properties.AddRange(collection2);
					parsed = true;
					return true;
				}
				return false;
			case 115:
			{
				string text = reader.ReadString();
				ushort num = reader.ReadUInt16();
				if (text != "Property" || num != 0)
				{
					return false;
				}
				List<WzImageProperty> collection = WzImageProperty.ParsePropertyList(offset, reader, this, this);
				properties.AddRange(collection);
				parsed = true;
				break;
			}
			default:
			{
				string message = "[WzImage] New Wz image header found. b = " + b;
				ErrorLogger.Log(ErrorLevel.MissingFeature, message);
				Debug.WriteLine(message);
				return false;
			}
			}
		}
		return true;
	}

	public void MarkWzImageAsParsed()
	{
		Parsed = true;
	}

	public void UnparseImage()
	{
		parsed = false;
		properties.Clear();
		properties = new WzPropertyCollection(this);
	}

	public void SaveImage(WzBinaryWriter writer, bool bIsWzUserKeyDefault = true, bool forceReadFromData = false)
	{
		if ((bIsImageChanged || !bIsWzUserKeyDefault) | forceReadFromData)
		{
			if (reader != null && !parsed)
			{
				ParseEverything = true;
				ParseImage(forceReadFromData);
			}
			WzSubProperty wzSubProperty = new WzSubProperty();
			long position = writer.BaseStream.Position;
			wzSubProperty.AddPropertiesForWzImageDumping(WzProperties);
			wzSubProperty.WriteValue(writer);
			writer.StringCache.Clear();
			size = (int)(writer.BaseStream.Position - position);
		}
		else
		{
			long position2 = reader.BaseStream.Position;
			reader.BaseStream.Position = offset;
			writer.Write(reader.ReadBytes(size));
			reader.BaseStream.Position = position2;
		}
	}

	public void ExportXml(StreamWriter writer, bool oneFile, int level)
	{
		if (oneFile)
		{
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzImage", name, finish: true));
			WzImageProperty.DumpPropertyList(writer, level, WzProperties);
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzImage"));
			return;
		}
		throw new Exception("Under Construction");
	}

	public override string ToString()
	{
		return string.Format("WzImage: '{0}' {1}", Name, (WzFileParent != null) ? (", ver. " + Enum.GetName(typeof(WzMapleVersion), WzFileParent.MapleVersion) + ", v" + WzFileParent.Version) : "");
	}
}
