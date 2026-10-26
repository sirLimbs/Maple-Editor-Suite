using System;
using System.Linq;

namespace MapleLib.WzLib.MSFile;

public class WzMsEntry
{
	private int _checkSum;

	private int _flags;

	private int _size;

	private int _sizeAligned;

	private int _unk1;

	private byte[] _entryKey;

	public byte[] Data { get; set; }

	public string Name { get; }

	public int CheckSum
	{
		get
		{
			return _checkSum;
		}
		set
		{
			_checkSum = value;
		}
	}

	public int Flags => _flags;

	public long StartPos { get; set; }

	public int Size
	{
		get
		{
			return _size;
		}
		set
		{
			_size = value;
		}
	}

	public int SizeAligned
	{
		get
		{
			return _sizeAligned;
		}
		set
		{
			_sizeAligned = value;
		}
	}

	public int Unk1 => _unk1;

	public int Unk2 { get; }

	public byte[] EntryKey => _entryKey;

	public int CalculatedCheckSum { get; private set; }

	public WzMsEntry(string name, int checkSum, int flags, int startPos, int size, int sizeAligned, int unk1, int unk2, byte[] entryKey)
	{
		Data = null;
		Name = name;
		_checkSum = checkSum;
		_flags = flags;
		StartPos = startPos;
		_size = size;
		_sizeAligned = sizeAligned;
		_unk1 = unk1;
		Unk2 = unk2;
		_entryKey = entryKey;
	}

	public void RecalculateFields(int flags, int startPos, int unk1, Random rng = null)
	{
		if (Data == null)
		{
			throw new InvalidOperationException("Data must be set before recalculation.");
		}
		_size = Data.Length;
		_sizeAligned = (_size + 1023) / 1024 * 1024;
		_flags = flags;
		StartPos = startPos;
		_unk1 = unk1;
		if (_entryKey == null)
		{
			if (rng == null)
			{
				rng = new Random();
			}
			_entryKey = new byte[16];
			rng.NextBytes(_entryKey);
		}
		int num = _entryKey.Sum((byte b) => b);
		CalculatedCheckSum = _flags + (int)StartPos + _size + _sizeAligned + _unk1 + num;
		_checkSum = CalculatedCheckSum;
	}
}
