using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MapleLib.WzLib.Util;

public sealed class WzMutableKey : IEquatable<WzMutableKey>
{
	private static readonly int BatchSize = 4096;

	private readonly byte[] _iv;

	private readonly byte[] _aesUserKey;

	private byte[]? _keys;

	public byte this[int index]
	{
		get
		{
			EnsureKeySize(index + 1);
			return _keys[index];
		}
	}

	public WzMutableKey(byte[] WzIv, byte[] AesKey)
	{
		_iv = WzIv;
		_aesUserKey = AesKey;
	}

	public byte[] GetKeys()
	{
		return _keys?.ToArray() ?? Array.Empty<byte>();
	}

	public void EnsureKeySize(int size)
	{
		if (_keys != null && _keys.Length >= size)
		{
			return;
		}
		size = (int)Math.Ceiling(1.0 * (double)size / (double)BatchSize) * BatchSize;
		byte[] array = new byte[size];
		if (BitConverter.ToInt32(_iv, 0) == 0)
		{
			_keys = array;
			return;
		}
		int num = 0;
		if (_keys != null)
		{
			_keys.CopyTo(array, 0);
			num = _keys.Length;
		}
		_keys = array;
		using Aes aes = Aes.Create();
		aes.KeySize = 256;
		aes.BlockSize = 128;
		aes.Key = _aesUserKey;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.None;
		using ICryptoTransform transform = aes.CreateEncryptor();
		using MemoryStream stream = new MemoryStream(array, num, array.Length - num, writable: true);
		using CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write);
		Span<byte> span = stackalloc byte[16];
		for (int i = num; i < size; i += 16)
		{
			if (i == 0)
			{
				for (int j = 0; j < span.Length; j++)
				{
					span[j] = _iv[j % 4];
				}
				cryptoStream.Write(span);
			}
			else
			{
				cryptoStream.Write(array.AsSpan(i - 16, 16));
			}
		}
		_keys = array;
	}

	public bool Equals(WzMutableKey? other)
	{
		return other != null && ((ReadOnlySpan<byte>)_iv.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)other._iv) && ((ReadOnlySpan<byte>)_aesUserKey.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)other._aesUserKey);
	}

	public override bool Equals(object? obj)
	{
		return this == obj || (obj is WzMutableKey other && Equals(other));
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(MemoryMarshal.Read<int>(_iv), MemoryMarshal.Read<int>(_aesUserKey));
	}

	public static bool operator ==(WzMutableKey? left, WzMutableKey? right)
	{
		return (object)left == right || (left?.Equals(right) ?? false);
	}

	public static bool operator !=(WzMutableKey? left, WzMutableKey? right)
	{
		return !(left == right);
	}
}
