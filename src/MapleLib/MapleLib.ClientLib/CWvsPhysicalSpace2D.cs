namespace MapleLib.ClientLib;

public class CWvsPhysicalSpace2D
{
	public static uint GetConstantCRC(int mapleVersion)
	{
		int input = mapleVersion;
		uint crc = CCrc32.GetCrc32(input, 0u, xorInitialCrc: false, flag2: false);
		input = 140000;
		uint crc2 = CCrc32.GetCrc32(input, crc, xorInitialCrc: false, flag2: false);
		input = 125;
		uint crc3 = CCrc32.GetCrc32(input, crc2, xorInitialCrc: false, flag2: false);
		input = 80000;
		uint crc4 = CCrc32.GetCrc32(input, crc3, xorInitialCrc: false, flag2: false);
		input = 60000;
		uint crc5 = CCrc32.GetCrc32(input, crc4, xorInitialCrc: false, flag2: false);
		input = 120;
		uint crc6 = CCrc32.GetCrc32(input, crc5, xorInitialCrc: false, flag2: false);
		input = 100000;
		uint crc7 = CCrc32.GetCrc32(input, crc6, xorInitialCrc: false, flag2: false);
		input = 0;
		uint crc8 = CCrc32.GetCrc32(input, crc7, xorInitialCrc: false, flag2: false);
		input = 120000;
		uint crc9 = CCrc32.GetCrc32(input, crc8, xorInitialCrc: false, flag2: false);
		input = 140;
		uint crc10 = CCrc32.GetCrc32(input, crc9, xorInitialCrc: false, flag2: false);
		input = 120000;
		uint crc11 = CCrc32.GetCrc32(input, crc10, xorInitialCrc: false, flag2: false);
		input = 200;
		uint crc12 = CCrc32.GetCrc32(input, crc11, xorInitialCrc: false, flag2: false);
		input = 2000;
		uint crc13 = CCrc32.GetCrc32(input, crc12, xorInitialCrc: false, flag2: false);
		input = 670;
		uint crc14 = CCrc32.GetCrc32(input, crc13, xorInitialCrc: false, flag2: false);
		input = 555;
		uint crc15 = CCrc32.GetCrc32(input, crc14, xorInitialCrc: false, flag2: false);
		input = 2;
		uint crc16 = CCrc32.GetCrc32(input, crc15, xorInitialCrc: false, flag2: false);
		input = 0;
		uint crc17 = CCrc32.GetCrc32(input, crc16, xorInitialCrc: false, flag2: false);
		input = 0;
		uint crc18 = CCrc32.GetCrc32(input, crc17, xorInitialCrc: false, flag2: false);
		input = 0;
		return CCrc32.GetCrc32(input, crc18, xorInitialCrc: false, flag2: false);
	}
}
