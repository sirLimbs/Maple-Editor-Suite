using System;
using System.IO;
using System.Text;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzLuaProperty : WzImageProperty
{
	internal string name;

	internal byte[] encryptedBytes;

	internal WzObject parent;

	internal WzMutableKey WzKey;

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

	public override WzPropertyType PropertyType => WzPropertyType.Lua;

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

	public byte[] Value
	{
		get
		{
			return encryptedBytes;
		}
		set
		{
			encryptedBytes = value;
		}
	}

	public override void SetValue(object value)
	{
		encryptedBytes = (byte[])value;
	}

	public override WzImageProperty DeepClone()
	{
		byte[] dst = new byte[encryptedBytes.Length];
		Buffer.BlockCopy(encryptedBytes, 0, dst, 0, encryptedBytes.Length);
		return new WzLuaProperty(name, dst);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.Write((byte)1);
		writer.WriteCompressedInt(encryptedBytes.Length);
		writer.Write(encryptedBytes);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzLua", Name, GetString()));
	}

	public override void Dispose()
	{
		name = null;
		encryptedBytes = null;
	}

	public WzLuaProperty(string name, byte[] encryptedBytes)
	{
		this.name = name;
		this.encryptedBytes = encryptedBytes;
		WzKey = WzKeyGenerator.GenerateLuaWzKey();
	}

	public override string GetString()
	{
		byte[] bytes = EncodeDecode(encryptedBytes);
		return Encoding.ASCII.GetString(bytes);
	}

	public override string ToString()
	{
		return GetString();
	}

	public byte[] EncodeDecode(byte[] input)
	{
		byte[] array = new byte[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			byte b = (byte)(input[i] ^ WzKey[i]);
			array[i] = b;
		}
		return array;
	}
}
