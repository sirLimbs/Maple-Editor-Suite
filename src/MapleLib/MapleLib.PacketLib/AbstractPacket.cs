using System.IO;

namespace MapleLib.PacketLib;

public abstract class AbstractPacket
{
	protected MemoryStream _buffer;

	public byte[] ToArray()
	{
		return _buffer.ToArray();
	}
}
