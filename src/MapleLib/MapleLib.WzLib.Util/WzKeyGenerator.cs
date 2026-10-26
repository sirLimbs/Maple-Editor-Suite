using System;
using System.IO;
using MapleLib.MapleCryptoLib;

namespace MapleLib.WzLib.Util;

public class WzKeyGenerator
{
	public static byte[] GetIvFromZlz(FileStream zlzStream)
	{
		byte[] array = new byte[4];
		zlzStream.Seek(65600L, SeekOrigin.Begin);
		zlzStream.Read(array, 0, 4);
		return array;
	}

	private static byte[] GetAesKeyFromZlz(FileStream zlzStream)
	{
		byte[] array = new byte[32];
		zlzStream.Seek(65632L, SeekOrigin.Begin);
		for (int i = 0; i < 8; i++)
		{
			zlzStream.Read(array, i * 4, 4);
			zlzStream.Seek(12L, SeekOrigin.Current);
		}
		return array;
	}

	public static WzMutableKey GenerateLuaWzKey()
	{
		return new WzMutableKey(WzAESConstant.WZ_MSEAIV, MapleCryptoConstants.GetTrimmedUserKey(ref MapleCryptoConstants.MAPLESTORY_USERKEY_DEFAULT));
	}

	public static WzMutableKey GenerateWzKey(byte[] WzIv)
	{
		return new WzMutableKey(WzIv, MapleCryptoConstants.GetTrimmedUserKey(ref MapleCryptoConstants.UserKey_WzLib));
	}

	public static WzMutableKey GenerateWzKey(byte[] WzIv, byte[] AesUserKey)
	{
		if (AesUserKey.Length != 128)
		{
			throw new Exception("AesUserkey expects 128 bytes, not " + AesUserKey.Length);
		}
		return new WzMutableKey(WzIv, MapleCryptoConstants.GetTrimmedUserKey(ref AesUserKey));
	}
}
