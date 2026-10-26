using System.IO;
using System.Text;

namespace MapleLib.PacketLib;

public class PacketReader : AbstractPacket
{
	private readonly BinaryReader _binReader;

	public short Length => (short)_buffer.Length;

	public int Position => (int)_buffer.Position;

	public int Remaining => (int)(_buffer.Length - _buffer.Position);

	public PacketReader(byte[] arrayOfBytes)
	{
		_buffer = new MemoryStream(arrayOfBytes, writable: false);
		_binReader = new BinaryReader(_buffer, Encoding.ASCII);
	}

	public void Reset(int length)
	{
		_buffer.Seek(length, SeekOrigin.Begin);
	}

	public void Skip(int length)
	{
		_buffer.Position += length;
	}

	public byte ReadByte()
	{
		return _binReader.ReadByte();
	}

	public byte[] ReadBytes(int count)
	{
		return _binReader.ReadBytes(count);
	}

	public bool ReadBool()
	{
		return _binReader.ReadBoolean();
	}

	public short ReadShort()
	{
		return _binReader.ReadInt16();
	}

	public int ReadInt()
	{
		return _binReader.ReadInt32();
	}

	public long ReadLong()
	{
		return _binReader.ReadInt64();
	}

	public string ReadString(int length)
	{
		return Encoding.ASCII.GetString(ReadBytes(length));
	}

	public string ReadMapleString()
	{
		return ReadString(ReadShort());
	}
}
