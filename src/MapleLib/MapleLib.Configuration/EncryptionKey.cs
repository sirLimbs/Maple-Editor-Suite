using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MapleLib.MapleCryptoLib;
using MapleLib.WzLib;
using MapleLib.WzLib.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapleLib.Configuration;

public sealed class EncryptionKey : INotifyPropertyChanged
{
	private string _name = string.Empty;

	private string _iv = "00 00 00 00";

	private string _aesUserKey = "";

	private WzMutableKey _wzKey;

	[JsonProperty("Name")]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			SetField(ref _name, value, "Name");
		}
	}

	[JsonProperty("MapleVersion")]
	[JsonConverter(typeof(StringEnumConverter))]
	public WzMapleVersion MapleVersion { get; set; } = WzMapleVersion.CUSTOM;

	[JsonProperty("Iv")]
	public string Iv
	{
		get
		{
			return _iv;
		}
		set
		{
			if (value.Length != 11)
			{
				throw new Exception("IV must be 4 bytes");
			}
			if (!string.Equals(_iv, value, StringComparison.Ordinal))
			{
				_iv = value;
				_wzKey = null;
			}
		}
	}

	[JsonProperty("AesUserKey")]
	public string AesUserKey
	{
		get
		{
			return _aesUserKey;
		}
		set
		{
			if (value.Length != 95)
			{
				throw new Exception("AES User Key must be 32 bytes");
			}
			if (!string.Equals(_aesUserKey, value, StringComparison.Ordinal))
			{
				_aesUserKey = value;
				_wzKey = null;
			}
		}
	}

	[JsonIgnore]
	public WzMutableKey WzKey
	{
		get
		{
			if (_wzKey != null)
			{
				return _wzKey;
			}
			byte[] wzIv = (from x in _iv.Split(' ')
				select Convert.ToByte(x, 16)).ToArray();
			byte[] array = (from x in _aesUserKey.Split(' ')
				select Convert.ToByte(x, 16)).ToArray();
			byte[] array2 = new byte[MapleCryptoConstants.MAPLESTORY_USERKEY_DEFAULT.Length];
			for (int num = 0; num < array2.Length; num += 4)
			{
				array2[num] = array[num / 4];
				array2[num + 1] = 0;
				array2[num + 2] = 0;
				array2[num + 3] = 0;
			}
			_wzKey = WzKeyGenerator.GenerateWzKey(wzIv, array2);
			return _wzKey;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public override string ToString()
	{
		return _name;
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
