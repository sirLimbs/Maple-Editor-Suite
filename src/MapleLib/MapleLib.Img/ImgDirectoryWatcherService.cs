#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MapleLib.Img;

public class ImgDirectoryWatcherService : IDisposable
{
	private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new ConcurrentDictionary<string, FileSystemWatcher>();

	private readonly ConcurrentDictionary<string, ImgFileState> _fileStates = new ConcurrentDictionary<string, ImgFileState>();

	private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new ConcurrentDictionary<string, Timer>();

	private readonly HashSet<string> _ignorePaths = new HashSet<string>();

	private readonly HashSet<string> _ignoreDirectories = new HashSet<string>();

	private readonly object _ignorePathsLock = new object();

	private readonly int _debounceMs;

	private readonly bool _trackContentHash;

	private bool _disposed;

	public IReadOnlyCollection<string> WatchedDirectories => _watchers.Keys.ToList().AsReadOnly();

	public event EventHandler<ImgFileModifiedEventArgs> ImgFileModified;

	public event EventHandler<ImgFileModifiedEventArgs> ImgFileDeleted;

	public event EventHandler<ImgFileModifiedEventArgs> ImgFileAdded;

	public event EventHandler<ImgFileModifiedEventArgs> ImgFileRenamed;

	public event EventHandler<ErrorEventArgs> WatcherError;

	public ImgDirectoryWatcherService(int debounceMs = 500, bool trackContentHash = true)
	{
		_debounceMs = debounceMs;
		_trackContentHash = trackContentHash;
	}

	public void WatchDirectory(string directoryPath)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("ImgDirectoryWatcherService");
		}
		if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
		{
			return;
		}
		string text = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar);
		if (_watchers.ContainsKey(text))
		{
			return;
		}
		try
		{
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(text)
			{
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite),
				Filter = "*.img",
				IncludeSubdirectories = true,
				EnableRaisingEvents = true
			};
			fileSystemWatcher.Changed += OnFileChanged;
			fileSystemWatcher.Created += OnFileCreated;
			fileSystemWatcher.Deleted += OnFileDeleted;
			fileSystemWatcher.Renamed += OnFileRenamed;
			fileSystemWatcher.Error += OnWatcherError;
			_watchers[text] = fileSystemWatcher;
			RecordDirectoryState(text);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to create watcher for " + directoryPath + ": " + ex.Message);
		}
	}

	public void UnwatchDirectory(string directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}
		string normalizedPath = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar);
		if (_watchers.TryRemove(normalizedPath, out var value))
		{
			value.EnableRaisingEvents = false;
			value.Dispose();
		}
		List<string> list = _fileStates.Keys.Where((string k) => k.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();
		foreach (string item in list)
		{
			_fileStates.TryRemove(item, out var _);
		}
	}

	public void UnwatchAll()
	{
		foreach (string item in _watchers.Keys.ToList())
		{
			UnwatchDirectory(item);
		}
		_fileStates.Clear();
	}

	public void RecordFileState(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(filePath);
			ImgFileState imgFileState = new ImgFileState
			{
				FilePath = filePath,
				FileSize = fileInfo.Length,
				LastWriteTime = fileInfo.LastWriteTimeUtc,
				RecordedAt = DateTime.UtcNow
			};
			if (_trackContentHash)
			{
				imgFileState.ContentHash = ComputeFileHash(filePath);
			}
			_fileStates[filePath] = imgFileState;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to record state for " + filePath + ": " + ex.Message);
		}
	}

	public void ClearTracking(string filePath)
	{
		if (!string.IsNullOrEmpty(filePath))
		{
			_fileStates.TryRemove(filePath, out var _);
		}
	}

	public void IgnorePath(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}
		lock (_ignorePathsLock)
		{
			_ignorePaths.Add(Path.GetFullPath(filePath));
		}
	}

	public void UnignorePath(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}
		lock (_ignorePathsLock)
		{
			_ignorePaths.Remove(Path.GetFullPath(filePath));
		}
	}

	public async Task UnignorePathDelayed(string filePath, int delayMs = 500)
	{
		await Task.Delay(delayMs);
		UnignorePath(filePath);
	}

	public void IgnoreDirectory(string directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}
		lock (_ignorePathsLock)
		{
			_ignoreDirectories.Add(Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar));
		}
	}

	public void UnignoreDirectory(string directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}
		lock (_ignorePathsLock)
		{
			_ignoreDirectories.Remove(Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar));
		}
	}

	public async Task UnignoreDirectoryDelayed(string directoryPath, int delayMs = 500)
	{
		await Task.Delay(delayMs);
		UnignoreDirectory(directoryPath);
	}

	public ImgChangeType GetChangeType(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return ImgChangeType.None;
		}
		if (!File.Exists(filePath))
		{
			return _fileStates.ContainsKey(filePath) ? ImgChangeType.Deleted : ImgChangeType.None;
		}
		if (!_fileStates.TryGetValue(filePath, out var value))
		{
			return ImgChangeType.Added;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(filePath);
			if (fileInfo.Length != value.FileSize)
			{
				return ImgChangeType.SizeChanged;
			}
			if (fileInfo.LastWriteTimeUtc != value.LastWriteTime)
			{
				if (!_trackContentHash)
				{
					return ImgChangeType.ContentChanged;
				}
				string text = ComputeFileHash(filePath);
				if (text != value.ContentHash)
				{
					return ImgChangeType.ContentChanged;
				}
			}
			return ImgChangeType.None;
		}
		catch
		{
			return ImgChangeType.None;
		}
	}

	public bool IsWatching(string directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return false;
		}
		string key = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar);
		return _watchers.ContainsKey(key);
	}

	private void RecordDirectoryState(string directoryPath)
	{
		try
		{
			string[] files = Directory.GetFiles(directoryPath, "*.img", SearchOption.AllDirectories);
			string[] array = files;
			foreach (string filePath in array)
			{
				RecordFileState(filePath);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to record directory state for " + directoryPath + ": " + ex.Message);
		}
	}

	private string ComputeFileHash(string filePath)
	{
		try
		{
			using MD5 mD = MD5.Create();
			using FileStream inputStream = File.OpenRead(filePath);
			byte[] array = mD.ComputeHash(inputStream);
			return BitConverter.ToString(array).Replace("-", "").ToLowerInvariant();
		}
		catch
		{
			return null;
		}
	}

	private bool IsPathIgnored(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return false;
		}
		string fullPath = Path.GetFullPath(filePath);
		lock (_ignorePathsLock)
		{
			if (_ignorePaths.Contains(fullPath))
			{
				return true;
			}
			foreach (string ignoreDirectory in _ignoreDirectories)
			{
				if (fullPath.StartsWith(ignoreDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool IsFileWriteComplete(string path)
	{
		try
		{
			using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				return true;
			}
		}
		catch (IOException)
		{
			return false;
		}
	}

	private void ProcessChangeWithDebounce(string filePath, Action<string> processAction)
	{
		if (_debounceTimers.TryRemove(filePath, out var value))
		{
			value.Dispose();
		}
		Timer value2 = new Timer((object? _) =>
		{
			_debounceTimers.TryRemove(filePath, out var _);
			processAction(filePath);
		}, null, _debounceMs, -1);
		_debounceTimers[filePath] = value2;
	}

	private void OnFileChanged(object sender, FileSystemEventArgs e)
	{
		if (_disposed || IsPathIgnored(e.FullPath))
		{
			return;
		}
		ProcessChangeWithDebounce(e.FullPath, (string path) =>
		{
			int num = 0;
			while (!IsFileWriteComplete(path) && num < 10)
			{
				Thread.Sleep(100);
				num++;
			}
			ImgChangeType changeType = GetChangeType(path);
			if (changeType != ImgChangeType.None)
			{
				ImgFileModified?.Invoke(this, new ImgFileModifiedEventArgs(path, changeType));
			}
		});
	}

	private void OnFileCreated(object sender, FileSystemEventArgs e)
	{
		if (_disposed || IsPathIgnored(e.FullPath))
		{
			return;
		}
		ProcessChangeWithDebounce(e.FullPath, (string path) =>
		{
			int num = 0;
			while (!IsFileWriteComplete(path) && num < 10)
			{
				Thread.Sleep(100);
				num++;
			}
			RecordFileState(path);
			ImgFileAdded?.Invoke(this, new ImgFileModifiedEventArgs(path, ImgChangeType.Added));
		});
	}

	private void OnFileDeleted(object sender, FileSystemEventArgs e)
	{
		if (!_disposed && !IsPathIgnored(e.FullPath) && _fileStates.TryRemove(e.FullPath, out var _))
		{
			ImgFileDeleted?.Invoke(this, new ImgFileModifiedEventArgs(e.FullPath, ImgChangeType.Deleted));
		}
	}

	private void OnFileRenamed(object sender, RenamedEventArgs e)
	{
		if (!_disposed && !IsPathIgnored(e.FullPath) && !IsPathIgnored(e.OldFullPath))
		{
			if (_fileStates.TryRemove(e.OldFullPath, out var value))
			{
				value.FilePath = e.FullPath;
				_fileStates[e.FullPath] = value;
			}
			ImgFileRenamed?.Invoke(this, new ImgFileModifiedEventArgs(e.FullPath, ImgChangeType.Renamed, e.OldFullPath));
		}
	}

	private void OnWatcherError(object sender, ErrorEventArgs e)
	{
		WatcherError?.Invoke(this, e);
		if (sender is FileSystemWatcher fileSystemWatcher)
		{
			string path = fileSystemWatcher.Path;
			UnwatchDirectory(path);
			try
			{
				WatchDirectory(path);
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (Timer value in _debounceTimers.Values)
		{
			value.Dispose();
		}
		_debounceTimers.Clear();
		foreach (FileSystemWatcher value2 in _watchers.Values)
		{
			value2.EnableRaisingEvents = false;
			value2.Dispose();
		}
		_watchers.Clear();
		_fileStates.Clear();
		lock (_ignorePathsLock)
		{
			_ignorePaths.Clear();
			_ignoreDirectories.Clear();
		}
	}
}
