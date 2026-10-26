using System;
using System.Collections.Generic;
using System.Threading;

namespace MapleLib.Img;

public class LRUCache<TKey, TValue> : IDisposable where TValue : class
{
	private class CacheItem
	{
		public TKey Key { get; set; }

		public TValue Value { get; set; }

		public long Size { get; set; }

		public DateTime LastAccess { get; set; }
	}

	private readonly int _capacity;

	private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;

	private readonly LinkedList<CacheItem> _lruList;

	private readonly ReaderWriterLockSlim _lock;

	private readonly Func<TValue, long> _sizeEstimator;

	private long _currentSize;

	private long _maxSizeBytes;

	private bool _disposed;

	private long _hitCount;

	private long _missCount;

	private long _evictionCount;

	public int Count
	{
		get
		{
			_lock.EnterReadLock();
			try
			{
				return _cache.Count;
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}
	}

	public long CurrentSizeBytes => Interlocked.Read(in _currentSize);

	public long HitCount => Interlocked.Read(in _hitCount);

	public long MissCount => Interlocked.Read(in _missCount);

	public long EvictionCount => Interlocked.Read(in _evictionCount);

	public double HitRatio
	{
		get
		{
			long num = HitCount + MissCount;
			return (num > 0) ? ((double)HitCount / (double)num) : 0.0;
		}
	}

	public LRUCache(int capacity)
	{
		_capacity = ((capacity > 0) ? capacity : 100);
		_cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(_capacity);
		_lruList = new LinkedList<CacheItem>();
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		_maxSizeBytes = long.MaxValue;
	}

	public LRUCache(long maxSizeBytes, Func<TValue, long> sizeEstimator)
	{
		_capacity = int.MaxValue;
		_maxSizeBytes = ((maxSizeBytes > 0) ? maxSizeBytes : 536870912);
		_sizeEstimator = sizeEstimator ?? throw new ArgumentNullException("sizeEstimator");
		_cache = new Dictionary<TKey, LinkedListNode<CacheItem>>();
		_lruList = new LinkedList<CacheItem>();
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
	}

	public bool TryGet(TKey key, out TValue value)
	{
		_lock.EnterUpgradeableReadLock();
		try
		{
			if (_cache.TryGetValue(key, out var value2))
			{
				_lock.EnterWriteLock();
				try
				{
					_lruList.Remove(value2);
					_lruList.AddFirst(value2);
					value2.Value.LastAccess = DateTime.UtcNow;
				}
				finally
				{
					_lock.ExitWriteLock();
				}
				Interlocked.Increment(ref _hitCount);
				value = value2.Value.Value;
				return true;
			}
			Interlocked.Increment(ref _missCount);
			value = null;
			return false;
		}
		finally
		{
			_lock.ExitUpgradeableReadLock();
		}
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
	{
		if (TryGet(key, out var value))
		{
			return value;
		}
		value = factory(key);
		if (value != null)
		{
			Add(key, value);
		}
		return value;
	}

	public void Add(TKey key, TValue value)
	{
		if (value == null)
		{
			return;
		}
		long num = _sizeEstimator?.Invoke(value) ?? 1;
		_lock.EnterWriteLock();
		try
		{
			if (_cache.TryGetValue(key, out var value2))
			{
				_currentSize -= value2.Value.Size;
				_lruList.Remove(value2);
				_cache.Remove(key);
			}
			while ((_cache.Count >= _capacity || _currentSize + num > _maxSizeBytes) && _lruList.Count > 0)
			{
				EvictLeastRecentlyUsed();
			}
			CacheItem value3 = new CacheItem
			{
				Key = key,
				Value = value,
				Size = num,
				LastAccess = DateTime.UtcNow
			};
			LinkedListNode<CacheItem> linkedListNode = new LinkedListNode<CacheItem>(value3);
			_lruList.AddFirst(linkedListNode);
			_cache[key] = linkedListNode;
			_currentSize += num;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public bool Remove(TKey key)
	{
		_lock.EnterWriteLock();
		try
		{
			if (_cache.TryGetValue(key, out var value))
			{
				_currentSize -= value.Value.Size;
				_lruList.Remove(value);
				_cache.Remove(key);
				return true;
			}
			return false;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public bool ContainsKey(TKey key)
	{
		_lock.EnterReadLock();
		try
		{
			return _cache.ContainsKey(key);
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public IEnumerable<KeyValuePair<TKey, TValue>> GetAllItems()
	{
		_lock.EnterReadLock();
		try
		{
			List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>(_cache.Count);
			foreach (CacheItem lru in _lruList)
			{
				list.Add(new KeyValuePair<TKey, TValue>(lru.Key, lru.Value));
			}
			return list;
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public IEnumerable<TValue> GetAllValues()
	{
		_lock.EnterReadLock();
		try
		{
			List<TValue> list = new List<TValue>(_cache.Count);
			foreach (CacheItem lru in _lruList)
			{
				list.Add(lru.Value);
			}
			return list;
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public void Clear()
	{
		_lock.EnterWriteLock();
		try
		{
			_cache.Clear();
			_lruList.Clear();
			_currentSize = 0L;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public void ResetStatistics()
	{
		Interlocked.Exchange(ref _hitCount, 0L);
		Interlocked.Exchange(ref _missCount, 0L);
		Interlocked.Exchange(ref _evictionCount, 0L);
	}

	public CacheStatistics GetStatistics()
	{
		return new CacheStatistics
		{
			ItemCount = Count,
			SizeBytes = CurrentSizeBytes,
			MaxSizeBytes = _maxSizeBytes,
			HitCount = HitCount,
			MissCount = MissCount,
			EvictionCount = EvictionCount,
			HitRatio = HitRatio
		};
	}

	private void EvictLeastRecentlyUsed()
	{
		LinkedListNode<CacheItem> last = _lruList.Last;
		if (last != null)
		{
			_currentSize -= last.Value.Size;
			_cache.Remove(last.Value.Key);
			_lruList.RemoveLast();
			Interlocked.Increment(ref _evictionCount);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_lock.EnterWriteLock();
		try
		{
			foreach (CacheItem lru in _lruList)
			{
				if (lru.Value is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch
					{
					}
				}
			}
			_cache.Clear();
			_lruList.Clear();
		}
		finally
		{
			_lock.ExitWriteLock();
		}
		_lock.Dispose();
	}
}
