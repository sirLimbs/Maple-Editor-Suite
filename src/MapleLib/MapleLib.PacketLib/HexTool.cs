using System.Text;

namespace MapleLib.PacketLib;

public static class HexTool
{
	private static char[] HEX = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'A', 'B', 'C', 'D', 'E', 'F'
	};

	public static string ToString(byte byteValue)
	{
		int num = byteValue << 8;
		char[] value = new char[2]
		{
			HEX[(num >> 12) & 0xF],
			HEX[(num >> 8) & 0xF]
		};
		return new string(value);
	}

	public static string ToString(byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < bytes.Length; i++)
		{
			stringBuilder.Append(ToString(bytes[i]));
			stringBuilder.Append(' ');
		}
		return stringBuilder.ToString();
	}

	public static string ToString(this PacketReader reader)
	{
		byte[] array = reader.ToArray();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(ToString(array[i]));
			stringBuilder.Append(' ');
		}
		return stringBuilder.ToString();
	}

	public static string ToString(this PacketWriter writer)
	{
		byte[] array = writer.ToArray();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(ToString(array[i]));
			stringBuilder.Append(' ');
		}
		return stringBuilder.ToString();
	}

	public static string ByteArrayToString(byte[] ba)
	{
		StringBuilder stringBuilder = new StringBuilder(ba.Length * 3);
		foreach (byte b in ba)
		{
			stringBuilder.AppendFormat("{0:x2} ", b);
		}
		return stringBuilder.ToString();
	}
}
