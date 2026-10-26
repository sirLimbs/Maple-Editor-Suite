namespace MapleLib.WzLib.MSFile;

public class WzMsHeader
{
	private int _hash;

	private int _entryCount;

	private long _HeaderStartPosition;

	private long _EntryStartPosition;

	public string FileName { get; }

	public string Salt { get; }

	public string FileNameWithSalt { get; }

	public int Hash
	{
		get
		{
			return _hash;
		}
		private set
		{
			_hash = value;
		}
	}

	public byte Version { get; }

	public int EntryCount
	{
		get
		{
			return _entryCount;
		}
		private set
		{
			_entryCount = value;
		}
	}

	public long HeaderStartPosition
	{
		get
		{
			return _HeaderStartPosition;
		}
		private set
		{
			_HeaderStartPosition = value;
		}
	}

	public long EntryStartPosition
	{
		get
		{
			return _EntryStartPosition;
		}
		private set
		{
			_EntryStartPosition = value;
		}
	}

	public long DataStartPosition { get; set; }

	public WzMsHeader(string fileName, string salt, string fileNameWithSalt, int hash, byte version, int entryCount, long headerStartPosition, long entryStartPosition)
	{
		FileName = fileName;
		Salt = salt;
		FileNameWithSalt = fileNameWithSalt;
		Hash = hash;
		Version = version;
		EntryCount = entryCount;
		HeaderStartPosition = headerStartPosition;
		EntryStartPosition = entryStartPosition;
	}

	public void UpdateHeader(int hash, int entryCount, long headerStartPosition, long entryStartPosition, long dataStartPosition)
	{
		Hash = hash;
		EntryCount = _entryCount;
		HeaderStartPosition = headerStartPosition;
		EntryStartPosition = entryStartPosition;
		DataStartPosition = dataStartPosition;
	}
}
