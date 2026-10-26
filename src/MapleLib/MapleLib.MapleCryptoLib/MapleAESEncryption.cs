using System;
using System.IO;
using System.Security.Cryptography;

namespace MapleLib.MapleCryptoLib;

public class MapleAESEncryption
{
	public static byte[] AesCrypt(byte[] IV, byte[] data, int length)
	{
		return AesCrypt(IV, data, length, MapleCryptoConstants.GetTrimmedUserKey(ref MapleCryptoConstants.UserKey_WzLib));
	}

	public static byte[] AesCrypt(byte[] IV, byte[] data, int length, byte[] key)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = 256;
		aes.Key = key;
		aes.Mode = CipherMode.ECB;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using ICryptoTransform transform = aes.CreateEncryptor();
			using CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			int num = length;
			int num2 = 1456;
			int num3 = 0;
			while (num > 0)
			{
				byte[] array = MapleCrypto.MultiplyBytes(IV, 4, 4);
				if (num < num2)
				{
					num2 = num;
				}
				for (int i = num3; i < num3 + num2; i++)
				{
					if ((i - num3) % array.Length == 0)
					{
						cryptoStream.Write(array, 0, array.Length);
						byte[] sourceArray = memoryStream.ToArray();
						Array.Copy(sourceArray, array, array.Length);
						memoryStream.Position = 0L;
					}
					data[i] ^= array[(i - num3) % array.Length];
				}
				num3 += num2;
				num -= num2;
				num2 = 1460;
			}
		}
		return data;
	}
}
