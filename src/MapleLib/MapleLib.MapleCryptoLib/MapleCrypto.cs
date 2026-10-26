using System;
using System.Numerics;

namespace MapleLib.MapleCryptoLib;

public class MapleCrypto
{
	private byte[] _IV;

	private short _mapleVersion;

	public byte[] IV
	{
		get
		{
			return _IV;
		}
		set
		{
			_IV = value;
		}
	}

	public MapleCrypto(byte[] IV, short mapleVersion)
	{
		_IV = IV;
		_mapleVersion = mapleVersion;
	}

	public void UpdateIV()
	{
		_IV = GetNewIV(_IV);
	}

	public void Crypt(byte[] data)
	{
		MapleAESEncryption.AesCrypt(_IV, data, data.Length);
		UpdateIV();
	}

	public static byte[] GetNewIV(byte[] oldIv)
	{
		byte[] array = new byte[4] { 242, 83, 80, 198 };
		for (int i = 0; i < 4; i++)
		{
			Shuffle(oldIv[i], array);
		}
		return array;
	}

	public static byte[] Shuffle(byte inputByte, byte[] start)
	{
		byte b = start[1];
		byte b2 = b;
		b2 = MapleCryptoConstants.bShuffle[b2];
		b2 -= inputByte;
		start[0] += b2;
		b2 = start[2];
		b2 ^= MapleCryptoConstants.bShuffle[inputByte];
		b -= b2;
		start[1] = b;
		b = start[3];
		b2 = b;
		b -= start[0];
		b2 = MapleCryptoConstants.bShuffle[b2];
		b2 += inputByte;
		b2 ^= start[2];
		start[2] = b2;
		b += MapleCryptoConstants.bShuffle[inputByte];
		start[3] = b;
		uint num = (uint)(start[0] + start[1] * 256 + start[2] * 65536 + start[3] * 16777216);
		uint num2 = num;
		num >>= 29;
		num2 <<= 3;
		num |= num2;
		start[0] = (byte)(num % 256);
		num /= 256;
		start[1] = (byte)(num % 256);
		num /= 256;
		start[2] = (byte)(num % 256);
		start[3] = (byte)(num / 256);
		return start;
	}

	public byte[] GetHeaderToClient(int size)
	{
		byte[] array = new byte[4];
		int num = _IV[3] * 256 + _IV[2];
		num ^= -(_mapleVersion + 1);
		int num2 = num ^ size;
		array[0] = (byte)(num % 256);
		array[1] = (byte)((num - array[0]) / 256);
		array[2] = (byte)(num2 ^ 0x100);
		array[3] = (byte)((num2 - array[2]) / 256);
		return array;
	}

	public byte[] GetHeaderToServer(int size)
	{
		byte[] array = new byte[4];
		int num = IV[3] * 256 + IV[2];
		num ^= _mapleVersion;
		int num2 = num ^ size;
		array[0] = Convert.ToByte(num % 256);
		array[1] = Convert.ToByte(num / 256);
		array[2] = Convert.ToByte(num2 % 256);
		array[3] = Convert.ToByte(num2 / 256);
		return array;
	}

	public static int GetPacketLength(int packetHeader)
	{
		return GetPacketLength(BitConverter.GetBytes(packetHeader));
	}

	public static int GetPacketLength(byte[] packetHeader)
	{
		if (packetHeader.Length < 4)
		{
			return -1;
		}
		return (packetHeader[0] + (packetHeader[1] << 8)) ^ (packetHeader[2] + (packetHeader[3] << 8));
	}

	public bool CheckPacketToServer(byte[] packet)
	{
		int num = packet[0] ^ _IV[2];
		int mapleVersion = _mapleVersion;
		int num2 = packet[1] ^ _IV[3];
		int num3 = _mapleVersion >> 8;
		return num == mapleVersion && num2 == num3;
	}

	public static byte[] MultiplyBytes(byte[] input, int count, int mult)
	{
		byte[] array = new byte[count * mult];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = input[i % count];
		}
		return array;
	}

	public static byte[] MultiplyBytes_SIMD(byte[] input, int count, int mult)
	{
		byte[] array = new byte[count * mult];
		int count2 = Vector<byte>.Count;
		int num = count / count2;
		for (int i = 0; i < num; i++)
		{
			Vector<byte> vector = new Vector<byte>(input, i * count2);
			for (int j = 0; j < mult; j++)
			{
				vector.CopyTo(array, i * count2 * mult + j * count2);
			}
		}
		int num2 = count % count2;
		if (num2 > 0)
		{
			for (int k = 0; k < array.Length; k++)
			{
				array[k] = input[k % count];
			}
		}
		return array;
	}
}
