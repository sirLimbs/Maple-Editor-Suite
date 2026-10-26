using System.IO;
using System.Text;

namespace MapleLib.PacketLib;

public class PacketWriter : AbstractPacket
{
	private readonly BinaryWriter _binWriter;

	public short Length => (short)_buffer.Length;

	public PacketWriter()
		: this(0)
	{
	}

	public PacketWriter(int size)
	{
		_buffer = new MemoryStream(size);
		_binWriter = new BinaryWriter(_buffer, Encoding.ASCII);
	}

	public PacketWriter(byte[] data)
	{
		_buffer = new MemoryStream(data);
		_binWriter = new BinaryWriter(_buffer, Encoding.ASCII);
	}

	public void Reset(int length)
	{
		_buffer.Seek(length, SeekOrigin.Begin);
	}

	public void WriteByte(int @byte)
	{
		_binWriter.Write((byte)@byte);
	}

	public void WriteBytes(byte[] bytes)
	{
		_binWriter.Write(bytes);
	}

	public void WriteBool(bool @bool)
	{
		_binWriter.Write(@bool);
	}

	public void WriteShort(int @short)
	{
		_binWriter.Write((short)@short);
	}

	public void WriteInt(int @int)
	{
		_binWriter.Write(@int);
	}

	public void WriteLong(long @long)
	{
		_binWriter.Write(@long);
	}

	public void WriteString(string @string)
	{
		_binWriter.Write(@string.ToCharArray());
	}

	public void WriteMapleString(string @string)
	{
		WriteShort((short)@string.Length);
		WriteString(@string);
	}

	public void WriteHexString(string hexString)
	{
		WriteBytes(HexEncoding.GetBytes(hexString));
	}

	public void SetByte(long index, int @byte)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteByte((byte)@byte);
		_buffer.Position = position;
	}

	public void SetBytes(long index, byte[] bytes)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteBytes(bytes);
		_buffer.Position = position;
	}

	public void SetBool(long index, bool @bool)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteBool(@bool);
		_buffer.Position = position;
	}

	public void SetShort(long index, int @short)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteShort((short)@short);
		_buffer.Position = position;
	}

	public void SetInt(long index, int @int)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteInt(@int);
		_buffer.Position = position;
	}

	public void SetLong(long index, long @long)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteLong(@long);
		_buffer.Position = position;
	}

	public void SetString(long index, string @string)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteString(@string);
		_buffer.Position = position;
	}

	public void SetMapleString(long index, string @string)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteMapleString(@string);
		_buffer.Position = position;
	}

	public void SetHexString(long index, string @string)
	{
		long position = _buffer.Position;
		_buffer.Position = index;
		WriteHexString(@string);
		_buffer.Position = position;
	}
}
