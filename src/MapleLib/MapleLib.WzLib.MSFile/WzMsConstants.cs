namespace MapleLib.WzLib.MSFile;

public class WzMsConstants
{
	public const int SupportedVersion = 2;

	public const int SnowKeyLength = 16;

	public const int BlockAlignment = 1024;

	public const int PageAlignmentMask = 1023;

	public const int PageAlignmentSize = 1024;

	public const int RandByteMod = 312;

	public const int RandByteOffset = 30;

	public const int HeaderPadMod = 212;

	public const int HeaderPadOffset = 33;

	public const uint InitialKeyHash = 2166136261u;

	public const uint KeyHashMultiplier = 16777619u;

	public const int SaltMinLength = 4;

	public const int SaltMaxLength = 12;

	public const int DoubleEncryptInitialBytes = 1024;

	public const int AsciiPrintableMin = 33;

	public const int AsciiPrintableMax = 127;

	public const int EntrySizeAligned = 1024;

	public const int EntryKeySize = 16;
}
