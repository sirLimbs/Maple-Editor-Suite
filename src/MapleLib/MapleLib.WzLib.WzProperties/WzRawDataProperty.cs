using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzRawDataProperty : WzExtended, IPropertyContainer
{
	public const string RAW_DATA_HEADER = "RawData";

	internal string _name;

	internal WzObject _parent;

	internal WzBinaryReader _wzReader;

	internal byte _type;

	internal long _rawDataOffset;

	internal int _length;

	internal byte[] _bytes;

	internal WzPropertyCollection properties;

	public override object WzValue => GetBytes(saveInMemory: false);

	public override WzObject Parent
	{
		get
		{
			return _parent;
		}
		internal set
		{
			_parent = value;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.Raw;

	public override string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public override WzPropertyCollection WzProperties => properties;

	public WzRawDataProperty(string name, WzBinaryReader reader, byte type)
	{
		_name = name;
		_wzReader = reader;
		_type = type;
		properties = new WzPropertyCollection(this);
	}

	public override WzImageProperty DeepClone()
	{
		WzRawDataProperty wzRawDataProperty = new WzRawDataProperty(_name, null, _type);
		foreach (WzImageProperty property in properties)
		{
			wzRawDataProperty.AddProperty(property.DeepClone());
		}
		wzRawDataProperty._length = _length;
		wzRawDataProperty._bytes = new byte[_length];
		GetBytes(saveInMemory: false).CopyTo(wzRawDataProperty._bytes, 0);
		return wzRawDataProperty;
	}

	public override void SetValue(object value)
	{
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		byte[] bytes = GetBytes(saveInMemory: false);
		writer.WriteStringValue("RawData", 115, 27);
		writer.Write(_type);
		if (_type == 1)
		{
			if (properties.Count > 0)
			{
				writer.Write((byte)1);
				WzImageProperty.WritePropertyList(writer, properties);
			}
			else
			{
				writer.Write((byte)0);
			}
		}
		writer.WriteCompressedInt(bytes.Length);
		writer.Write(bytes);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.Write(XmlUtil.Indentation(level));
		writer.WriteLine(XmlUtil.EmptyNamedTag("RawData", Name));
	}

	public override void Dispose()
	{
		_name = null;
		_bytes = null;
		foreach (WzImageProperty property in properties)
		{
			property.Dispose();
		}
		properties.Clear();
		properties = null;
	}

	internal void Parse(bool parseNow)
	{
		_length = _wzReader.ReadCompressedInt();
		_rawDataOffset = _wzReader.BaseStream.Position;
		if (parseNow)
		{
			GetBytes(saveInMemory: true);
		}
		else
		{
			_wzReader.BaseStream.Position = _rawDataOffset + _length;
		}
	}

	public byte[] GetBytes(bool saveInMemory)
	{
		if (_bytes != null)
		{
			return _bytes;
		}
		if (_wzReader == null)
		{
			return null;
		}
		long position = _wzReader.BaseStream.Position;
		_wzReader.BaseStream.Position = _rawDataOffset;
		_bytes = _wzReader.ReadBytes(_length);
		_wzReader.BaseStream.Position = position;
		if (saveInMemory)
		{
			return _bytes;
		}
		byte[] bytes = _bytes;
		_bytes = null;
		return bytes;
	}

	public void AddProperty(WzImageProperty prop)
	{
		prop.Parent = this;
		properties.Add(prop);
	}

	public void AddProperties(WzPropertyCollection props)
	{
		foreach (WzImageProperty prop in props)
		{
			AddProperty(prop);
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

	public void RemoveProperty(WzImageProperty prop)
	{
		prop.Parent = null;
		properties.Remove(prop);
	}

	public void ClearProperties()
	{
		foreach (WzImageProperty property in properties)
		{
			property.Parent = null;
		}
		properties.Clear();
	}
}
