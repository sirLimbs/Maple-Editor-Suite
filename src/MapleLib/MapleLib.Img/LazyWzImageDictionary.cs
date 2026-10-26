using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class LazyWzImageDictionary : IDictionary<string, WzImage>, ICollection<KeyValuePair<string, WzImage>>, IEnumerable<KeyValuePair<string, WzImage>>, IEnumerable
{
	private readonly HashSet<string> _names;

	private readonly Func<string, WzImage> _loader;

	private readonly Dictionary<string, WzImage> _loadedCache;

	private readonly object _lock = new object();

	public WzImage this[string key]
	{
		get
		{
			if (string.IsNullOrEmpty(key))
			{
				return null;
			}
			lock (_lock)
			{
				if (_loadedCache.TryGetValue(key, out var value))
				{
					return value;
				}
				if (!_names.Contains(key))
				{
					return null;
				}
				try
				{
					WzImage wzImage = _loader(key);
					if (wzImage != null)
					{
						_loadedCache[key] = wzImage;
					}
					return wzImage;
				}
				catch
				{
					return null;
				}
			}
		}
		set
		{
			if (string.IsNullOrEmpty(key))
			{
				return;
			}
			lock (_lock)
			{
				_names.Add(key);
				if (value != null)
				{
					_loadedCache[key] = value;
				}
				else
				{
					_loadedCache.Remove(key);
				}
			}
		}
	}

	public ICollection<string> Keys
	{
		get
		{
			lock (_lock)
			{
				return _names.ToList();
			}
		}
	}

	public ICollection<WzImage> Values
	{
		get
		{
			lock (_lock)
			{
				foreach (string name in _names)
				{
					if (!_loadedCache.ContainsKey(name))
					{
						WzImage wzImage = _loader(name);
						if (wzImage != null)
						{
							_loadedCache[name] = wzImage;
						}
					}
				}
				return _loadedCache.Values.ToList();
			}
		}
	}

	public int Count
	{
		get
		{
			lock (_lock)
			{
				return _names.Count;
			}
		}
	}

	public int LoadedCount
	{
		get
		{
			lock (_lock)
			{
				return _loadedCache.Count;
			}
		}
	}

	public bool IsReadOnly => false;

	public LazyWzImageDictionary(Func<string, WzImage> loader)
	{
		_names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_loader = loader ?? throw new ArgumentNullException("loader");
		_loadedCache = new Dictionary<string, WzImage>(StringComparer.OrdinalIgnoreCase);
	}

	public void RegisterName(string name)
	{
		lock (_lock)
		{
			_names.Add(name);
		}
	}

	public void RegisterNames(IEnumerable<string> names)
	{
		lock (_lock)
		{
			foreach (string name in names)
			{
				_names.Add(name);
			}
		}
	}

	public void Add(string key, WzImage value)
	{
		this[key] = value;
	}

	public void Add(KeyValuePair<string, WzImage> item)
	{
		this[item.Key] = item.Value;
	}

	public void Clear()
	{
		lock (_lock)
		{
			_names.Clear();
			_loadedCache.Clear();
		}
	}

	public bool Contains(KeyValuePair<string, WzImage> item)
	{
		return ContainsKey(item.Key);
	}

	public bool ContainsKey(string key)
	{
		lock (_lock)
		{
			return _names.Contains(key);
		}
	}

	public void CopyTo(KeyValuePair<string, WzImage>[] array, int arrayIndex)
	{
		lock (_lock)
		{
			int num = arrayIndex;
			foreach (string name in _names)
			{
				array[num++] = new KeyValuePair<string, WzImage>(name, this[name]);
			}
		}
	}

	public IEnumerator<KeyValuePair<string, WzImage>> GetEnumerator()
	{
		List<string> namesCopy;
		lock (_lock)
		{
			namesCopy = _names.ToList();
		}
		foreach (string name in namesCopy)
		{
			yield return new KeyValuePair<string, WzImage>(name, null);
		}
	}

	public bool Remove(string key)
	{
		lock (_lock)
		{
			_loadedCache.Remove(key);
			return _names.Remove(key);
		}
	}

	public bool Remove(KeyValuePair<string, WzImage> item)
	{
		return Remove(item.Key);
	}

	public bool TryGetValue(string key, out WzImage value)
	{
		if (string.IsNullOrEmpty(key))
		{
			value = null;
			return false;
		}
		lock (_lock)
		{
			if (!_names.Contains(key))
			{
				value = null;
				return false;
			}
		}
		value = this[key];
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void ClearLoadedCache()
	{
		lock (_lock)
		{
			_loadedCache.Clear();
		}
	}
}
