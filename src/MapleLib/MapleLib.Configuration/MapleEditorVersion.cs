using System;
using System.IO;
using MapleLib.WzLib;
using Newtonsoft.Json;

namespace MapleLib.Configuration;

public enum MapleEditorGameVersion
{
	V83 = 83,
	V95 = 95
}

public sealed class MapleVersionProfile
{
	public MapleEditorGameVersion Target { get; init; }

	public short GameVersion => (short)(int)Target;

	public string DisplayName => Target switch
	{
		MapleEditorGameVersion.V83 => "v83 (native)",
		MapleEditorGameVersion.V95 => "v95",
		_ => "v" + (int)Target
	};

	public WzMapleVersion WzMapleVersion => WzMapleVersion.GMS;

	public string MobArchiveName => "Mob.wz";

	public string CharacterArchiveName => "Character.wz";

	public string ItemArchiveName => "Item.wz";

	public string StringArchiveName => "String.wz";

	public override string ToString()
	{
		return DisplayName;
	}
}

public sealed class MapleEditorSettings
{
	public MapleEditorGameVersion Version { get; set; } = MapleEditorGameVersion.V83;

	public string V83DataFolder { get; set; } = string.Empty;

	public string V95WzFolder { get; set; } = string.Empty;

	public string LastMobWz { get; set; } = string.Empty;

	public string LastCharacterWz { get; set; } = string.Empty;

	public string LastItemWz { get; set; } = string.Empty;

	public string LastStringWz { get; set; } = string.Empty;
}

public static class MapleEditorVersionSettings
{
	private static readonly object SyncRoot = new object();

	private static MapleEditorSettings _settings;

	private static string _settingsPath;

	public static event EventHandler? Changed;

	public static MapleEditorSettings Settings
	{
		get
		{
			lock (SyncRoot)
			{
				if (_settings == null)
				{
					Load();
				}
				return _settings;
			}
		}
	}

	public static MapleEditorGameVersion Current
	{
		get
		{
			return Settings.Version;
		}
		set
		{
			if (Settings.Version != value)
			{
				Settings.Version = value;
				Save();
				Changed?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static MapleVersionProfile Profile => GetProfile(Current);

	public static string SettingsPath
	{
		get
		{
			lock (SyncRoot)
			{
				return _settingsPath ??= BuildSettingsPath();
			}
		}
	}

	public static MapleVersionProfile GetProfile(MapleEditorGameVersion version)
	{
		return new MapleVersionProfile { Target = version };
	}

	private static string BuildSettingsPath()
	{
		string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MapleEditor");
		return Path.Combine(folder, "settings.json");
	}

	public static void Load()
	{
		lock (SyncRoot)
		{
			_settingsPath = BuildSettingsPath();
			try
			{
				if (File.Exists(_settingsPath))
				{
					_settings = JsonConvert.DeserializeObject<MapleEditorSettings>(File.ReadAllText(_settingsPath)) ?? new MapleEditorSettings();
				}
				else
				{
					_settings = new MapleEditorSettings();
				}
			}
			catch
			{
				_settings = new MapleEditorSettings();
			}
		}
	}

	public static void Save()
	{
		lock (SyncRoot)
		{
			if (_settings == null)
			{
				return;
			}
			try
			{
				_settingsPath = BuildSettingsPath();
				string directory = Path.GetDirectoryName(_settingsPath);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}
				File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
			}
			catch
			{
			}
		}
	}

	public static string GetDefaultOpenFolder()
	{
		MapleEditorSettings settings = Settings;
		if (Current == MapleEditorGameVersion.V95)
		{
			string folder = !string.IsNullOrWhiteSpace(settings.V95WzFolder) ? settings.V95WzFolder : GetLastArchiveFolder(settings);
			if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
			{
				return folder;
			}
		}
		else if (!string.IsNullOrWhiteSpace(settings.V83DataFolder) && Directory.Exists(settings.V83DataFolder))
		{
			return settings.V83DataFolder;
		}
		return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
	}

	public static string GetDefaultWzFolder()
	{
		string folder = (!string.IsNullOrWhiteSpace(Settings.V95WzFolder) ? Settings.V95WzFolder : GetLastArchiveFolder(Settings));
		if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
		{
			return folder;
		}
		return GetDefaultOpenFolder();
	}

	private static string GetLastArchiveFolder(MapleEditorSettings settings)
	{
		string[] paths = new string[3]
		{
			settings.LastItemWz,
			settings.LastMobWz,
			settings.LastCharacterWz
		};
		foreach (string path in paths)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				continue;
			}
			string? directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
			{
				return directory;
			}
		}
		return string.Empty;
	}

	public static string? FindSiblingWz(string? archivePath, string fileName)
	{
		if (string.IsNullOrWhiteSpace(archivePath))
		{
			return null;
		}
		string? directory = Path.GetDirectoryName(archivePath);
		if (string.IsNullOrWhiteSpace(directory))
		{
			return null;
		}
		string candidate = Path.Combine(directory, fileName);
		if (File.Exists(candidate))
		{
			return candidate;
		}
		if (!string.IsNullOrWhiteSpace(Settings.V95WzFolder))
		{
			candidate = Path.Combine(Settings.V95WzFolder, fileName);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}
		return null;
	}

}
