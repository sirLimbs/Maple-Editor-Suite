using System;
using System.Runtime.Serialization;

namespace MapleLib.WzLib.WzStructure;

[DataContract]
public struct MapleBool
{
	public const byte NotExist = 0;

	public const byte False = 1;

	public const byte True = 2;

	[DataMember]
	private byte val { get; set; }

	public bool HasValue => val != 0;

	public bool Value => val switch
	{
		1 => false, 
		2 => true, 
		_ => throw new Exception("Tried to get value of nonexistant MapleBool"), 
	};

	public static implicit operator MapleBool(byte value)
	{
		return new MapleBool
		{
			val = value
		};
	}

	public static implicit operator MapleBool(bool? value)
	{
		return new MapleBool
		{
			val = (byte)(value.HasValue ? ((!value.Value) ? 1 : 2) : 0)
		};
	}

	public static implicit operator bool(MapleBool value)
	{
		return value == (byte)2;
	}

	public static implicit operator byte(MapleBool value)
	{
		return value.val;
	}

	public override bool Equals(object obj)
	{
		return obj is MapleBool { val: var b } && b.Equals(val);
	}

	public override int GetHashCode()
	{
		return val.GetHashCode();
	}

	public static bool operator ==(MapleBool a, MapleBool b)
	{
		return a.val.Equals(b.val);
	}

	public static bool operator ==(MapleBool a, bool b)
	{
		return (b && a.val == 2) || (!b && a.val == 1);
	}

	public static bool operator !=(MapleBool a, MapleBool b)
	{
		return !a.val.Equals(b.val);
	}

	public static bool operator !=(MapleBool a, bool b)
	{
		return (b && a.val != 2) || (!b && a.val != 1);
	}
}
