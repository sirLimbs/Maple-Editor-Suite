namespace MapleLib.MapleCryptoLib;

public class MapleCustomEncryption
{
	public static void Encrypt(byte[] data)
	{
		int num = data.Length;
		for (int i = 0; i < 3; i++)
		{
			byte b = 0;
			for (int num2 = num; num2 > 0; num2--)
			{
				byte val = data[num - num2];
				val = rol(val, 3);
				val = (byte)(val + num2);
				val ^= b;
				b = val;
				val = ror(b, num2);
				val ^= 0xFF;
				val += 72;
				data[num - num2] = val;
			}
			b = 0;
			for (int num2 = data.Length; num2 > 0; num2--)
			{
				byte val = data[num2 - 1];
				val = rol(val, 4);
				val = (byte)(val + num2);
				val ^= b;
				b = val;
				val ^= 0x13;
				val = ror(val, 3);
				data[num2 - 1] = val;
			}
		}
	}

	public static void Decrypt(byte[] data)
	{
		int num = data.Length;
		for (int i = 0; i < 3; i++)
		{
			byte b = 0;
			byte b2 = 0;
			for (int num2 = num; num2 > 0; num2--)
			{
				byte val = data[num2 - 1];
				val = rol(val, 3);
				val ^= 0x13;
				b = val;
				val ^= b2;
				val = (byte)(val - num2);
				val = ror(val, 4);
				b2 = b;
				data[num2 - 1] = val;
			}
			b = 0;
			b2 = 0;
			for (int num2 = num; num2 > 0; num2--)
			{
				byte val = data[num - num2];
				val -= 72;
				val ^= 0xFF;
				val = rol(val, num2);
				b = val;
				val ^= b2;
				val = (byte)(val - num2);
				val = ror(val, 3);
				b2 = b;
				data[num - num2] = val;
			}
		}
	}

	public static byte rol(byte val, int num)
	{
		for (int i = 0; i < num; i++)
		{
			val = (byte)((val << 1) | ((val >> 7) & 1));
		}
		return val;
	}

	public static byte ror(byte val, int num)
	{
		for (int i = 0; i < num; i++)
		{
			val = (byte)((val >> 1) | ((val & 1) << 7));
		}
		return val;
	}
}
