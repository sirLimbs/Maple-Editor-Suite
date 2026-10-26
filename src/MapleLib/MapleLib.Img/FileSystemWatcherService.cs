#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MapleLib.Img;

public class FileSystemWatcherService : IDisposable
{
	private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new ConcurrentDictionary<string, FileSystemWatcher>();

	private readonly ConcurrentDictionary<string, WatchType> _watchTypes = new ConcurrentDictionary<string, WatchType>();

	private readonly ConcurrentDictionary<string, string> _categoryPaths = new ConcurrentDictionary<string, string>();

	private readonly ConcurrentQueue<FileChangeInfo> _pendingChanges = new ConcurrentQueue<FileChangeInfo>();

	private Timer _debounceTimer;

	private readonly int _debounceMs;

	private readonly object _timerLock = new object();

	private bool _disposed;

	private bool _isProcessing;

	public IReadOnlyCollection<string> WatchedPaths => _watchers.Keys.ToList().AsReadOnly();

	public event EventHandler<ImgFileChangedEventArgs> ImgFileChanged;

	public event EventHandler<VersionDirectoryChangedEventArgs> VersionDirectoryChanged;

	public event EventHandler<ErrorEventArgs> WatcherError;

	public FileSystemWatcherService(int debounceMs = 500)
	{
		_debounceMs = debounceMs;
	}

	public void WatchPath(string path, WatchType watchType, string categoryName = null)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("FileSystemWatcherService");
		}
		if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
		{
			return;
		}
		string text = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
		if (_watchers.ContainsKey(text))
		{
			return;
		}
		try
		{
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(text);
			switch (watchType)
			{
			case WatchType.Category:
				fileSystemWatcher.Filter = "*.img";
				fileSystemWatcher.IncludeSubdirectories = true;
				fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
				_categoryPaths[text] = categoryName ?? Path.GetFileName(text);
				break;
			case WatchType.VersionRoot:
				fileSystemWatcher.Filter = "*";
				fileSystemWatcher.IncludeSubdirectories = false;
				fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
				break;
			}
			fileSystemWatcher.Created += OnFileSystemEvent;
			fileSystemWatcher.Deleted += OnFileSystemEvent;
			fileSystemWatcher.Changed += OnFileSystemEvent;
			fileSystemWatcher.Renamed += OnFileSystemRenamed;
			fileSystemWatcher.Error += OnWatcherError;
			fileSystemWatcher.EnableRaisingEvents = true;
			_watchers[text] = fileSystemWatcher;
			_watchTypes[text] = watchType;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to create watcher for " + path + ": " + ex.Message);
		}
	}

	public void UnwatchPath(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			string key = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
			if (_watchers.TryRemove(key, out var value))
			{
				value.EnableRaisingEvents = false;
				value.Dispose();
			}
			_watchTypes.TryRemove(key, out var _);
			_categoryPaths.TryRemove(key, out var _);
		}
	}

	public void UnwatchAll()
	{
		foreach (string item in _watchers.Keys.ToList())
		{
			UnwatchPath(item);
		}
	}

	public bool IsWatching(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		string key = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
		return _watchers.ContainsKey(key);
	}

	private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
	{
		if (_disposed || !(sender is FileSystemWatcher fileSystemWatcher))
		{
			return;
		}
		string path = fileSystemWatcher.Path;
		if (_watchTypes.TryGetValue(path, out var value))
		{
			string value2 = null;
			string relativePath = null;
			if (value == WatchType.Category)
			{
				_categoryPaths.TryGetValue(path, out value2);
				relativePath = e.FullPath.Substring(path.Length).TrimStart(Path.DirectorySeparatorChar);
			}
			_pendingChanges.Enqueue(new FileChangeInfo
			{
				Path = e.FullPath,
				ChangeType = e.ChangeType,
				Timestamp = DateTime.UtcNow,
				WatchType = value,
				Category = value2,
				RelativePath = relativePath
			});
			ResetDebounceTimer();
		}
	}

	private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
	{
		if (_disposed || !(sender is FileSystemWatcher fileSystemWatcher))
		{
			return;
		}
		string path = fileSystemWatcher.Path;
		if (_watchTypes.TryGetValue(path, out var value))
		{
			string value2 = null;
			string relativePath = null;
			if (value == WatchType.Category)
			{
				_categoryPaths.TryGetValue(path, out value2);
				relativePath = e.FullPath.Substring(path.Length).TrimStart(Path.DirectorySeparatorChar);
			}
			_pendingChanges.Enqueue(new FileChangeInfo
			{
				Path = e.FullPath,
				OldPath = e.OldFullPath,
				ChangeType = WatcherChangeTypes.Renamed,
				Timestamp = DateTime.UtcNow,
				WatchType = value,
				Category = value2,
				RelativePath = relativePath
			});
			ResetDebounceTimer();
		}
	}

	private void OnWatcherError(object sender, ErrorEventArgs e)
	{
		WatcherError?.Invoke(this, e);
		if (sender is FileSystemWatcher fileSystemWatcher)
		{
			string path = fileSystemWatcher.Path;
			if (_watchTypes.TryGetValue(path, out var value))
			{
				UnwatchPath(path);
				string value2 = null;
				_categoryPaths.TryGetValue(path, out value2);
				WatchPath(path, value, value2);
			}
		}
	}

	private void ResetDebounceTimer()
	{
		lock (_timerLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = new Timer(ProcessPendingChanges, null, _debounceMs, -1);
		}
	}

	private void ProcessPendingChanges(object state)
	{
		if (_disposed || _isProcessing)
		{
			return;
		}
		_isProcessing = true;
		try
		{
			List<FileChangeInfo> list = new List<FileChangeInfo>();
			FileChangeInfo result;
			while (_pendingChanges.TryDequeue(out result))
			{
				list.Add(result);
			}
			if (list.Count == 0)
			{
				return;
			}
			List<FileChangeInfo> list2 = (from c in list
				group c by c.Path into g
				select g.OrderByDescending((FileChangeInfo c) => c.Timestamp).First()).ToList();
			foreach (FileChangeInfo item in list2)
			{
				RaiseAppropriateEvent(item);
			}
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private void RaiseAppropriateEvent(FileChangeInfo change)
	{
		try
		{
			switch (change.WatchType)
			{
			case WatchType.Category:
				ImgFileChanged?.Invoke(this, new ImgFileChangedEventArgs(change.Path, change.Category, change.RelativePath, change.ChangeType, change.OldPath));
				break;
			case WatchType.VersionRoot:
				if (change.ChangeType == WatcherChangeTypes.Deleted || Directory.Exists(change.Path))
				{
					VersionDirectoryChanged?.Invoke(this, new VersionDirectoryChangedEventArgs(change.Path, change.ChangeType));
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Error raising file change event: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		lock (_timerLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = null;
		}
		foreach (FileSystemWatcher value in _watchers.Values)
		{
			value.EnableRaisingEvents = false;
			value.Dispose();
		}
		_watchers.Clear();
		_watchTypes.Clear();
		_categoryPaths.Clear();
		FileChangeInfo result;
		while (_pendingChanges.TryDequeue(out result))
		{
		}
	}
}
