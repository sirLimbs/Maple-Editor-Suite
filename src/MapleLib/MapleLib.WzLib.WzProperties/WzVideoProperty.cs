using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzVideoProperty : WzExtended, IPropertyContainer
{
	public const string CANVAS_VIDEO_HEADER = "Canvas#Video";

	internal string name;

	internal WzObject parent;

	internal WzBinaryReader wzReader;

	internal WzPropertyCollection properties;

	internal long _offset;

	internal int _length;

	internal int type;

	internal byte[] _bytes;

	public override object WzValue => GetBytes(saveInMemory: false);

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

	public override WzPropertyType PropertyType => WzPropertyType.Raw;

	public override WzPropertyCollection WzProperties => properties;

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

	public WzVideoProperty(string name, WzBinaryReader reader)
	{
		this.name = name;
		wzReader = reader;
		properties = new WzPropertyCollection(this);
	}

	private WzVideoProperty(WzVideoProperty copy)
	{
		name = copy.name;
		_bytes = new byte[copy._length];
		copy.GetBytes(saveInMemory: false).CopyTo(_bytes, 0);
		_length = copy._length;
		properties = copy.properties;
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

	public void RemoveProperty(WzImageProperty prop)
	{
		prop.Parent = null;
		properties.Remove(prop);
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
	}

	public override WzImageProperty DeepClone()
	{
		return new WzVideoProperty(this);
	}

	internal void Parse(bool parseNow)
	{
		type = wzReader.ReadByte();
		_length = wzReader.ReadCompressedInt();
		_offset = wzReader.BaseStream.Position;
		if (parseNow)
		{
			GetBytes(saveInMemory: true);
		}
		else
		{
			wzReader.BaseStream.Position = _offset + _length;
		}
	}

	public override void SetValue(object value)
	{
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		byte[] bytes = GetBytes(saveInMemory: false);
		writer.WriteStringValue("Canvas#Video", 115, 27);
		writer.Write((byte)0);
		if (properties.Count > 0)
		{
			writer.Write((byte)1);
			WzImageProperty.WritePropertyList(writer, properties);
		}
		else
		{
			writer.Write((byte)0);
		}
		writer.Write((byte)type);
		writer.WriteCompressedInt(bytes.Length);
		writer.Write(bytes);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("Canvas#Video", Name, finish: false, empty: false));
		WzImageProperty.DumpPropertyList(writer, level, WzProperties);
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("Canvas#Video"));
	}

	public override void Dispose()
	{
		name = null;
		_bytes = null;
		foreach (WzImageProperty property in properties)
		{
			property.Dispose();
		}
		properties.Clear();
		properties = null;
	}

	public byte[] GetBytes(bool saveInMemory)
	{
		if (_bytes != null)
		{
			return _bytes;
		}
		if (wzReader == null)
		{
			return null;
		}
		long position = wzReader.BaseStream.Position;
		wzReader.BaseStream.Position = _offset;
		_bytes = wzReader.ReadBytes(_length);
		wzReader.BaseStream.Position = position;
		if (saveInMemory)
		{
			return _bytes;
		}
		byte[] bytes = _bytes;
		_bytes = null;
		return bytes;
	}
}
