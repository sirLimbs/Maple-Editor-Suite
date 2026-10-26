#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class HaCreatorConfig
{
	private const int MaxRecentVersions = 20;

	[JsonProperty("dataSourceMode")]
	public DataSourceMode DataSourceMode { get; set; } = DataSourceMode.ImgFileSystem;

	[JsonProperty("imgRootPath")]
	public string ImgRootPath { get; set; } = HaCreatorPaths.DefaultDataPath;

	[JsonProperty("lastUsedVersion")]
	public string LastUsedVersion { get; set; }

	[JsonProperty("cache")]
	public CacheConfig Cache { get; set; } = new CacheConfig();

	[JsonProperty("extraction")]
	public ExtractionConfig Extraction { get; set; } = new ExtractionConfig();

	[JsonProperty("legacy")]
	public LegacyConfig Legacy { get; set; } = new LegacyConfig();

	[JsonIgnore]
	public HotSwapConfig HotSwap { get; set; } = new HotSwapConfig();

	[JsonProperty("additionalVersionPaths")]
	public List<string> AdditionalVersionPaths { get; set; } = new List<string>();

	[JsonProperty("recentVersionPaths")]
	public List<string> RecentVersionPaths { get; set; } = new List<string>();

	[JsonIgnore]
	public string VersionsPath => HaCreatorPaths.GetVersionsPath(ImgRootPath);

	[JsonIgnore]
	public string CustomPath => HaCreatorPaths.GetCustomPath(ImgRootPath);

	public void AddToRecentVersionPaths(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			string normalizedPath = Path.GetFullPath(path);
			RecentVersionPaths.RemoveAll((string p) => Path.GetFullPath(p).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));
			RecentVersionPaths.Insert(0, path);
			if (RecentVersionPaths.Count > 20)
			{
				RecentVersionPaths.RemoveRange(20, RecentVersionPaths.Count - 20);
			}
		}
	}

	public static HaCreatorConfig Load()
	{
		return Load(HaCreatorPaths.DefaultConfigPath);
	}

	public static HaCreatorConfig Load(string configPath)
	{
		try
		{
			if (File.Exists(configPath))
			{
				string value = File.ReadAllText(configPath);
				return JsonConvert.DeserializeObject<HaCreatorConfig>(value) ?? new HaCreatorConfig();
			}
		}
		catch (Exception)
		{
		}
		return new HaCreatorConfig();
	}

	public void Save()
	{
		Save(HaCreatorPaths.DefaultConfigPath);
	}

	public void Save(string configPath)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(configPath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(configPath, contents);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to save config: " + ex.Message);
		}
	}

	public void EnsureDirectoriesExist()
	{
		if (!Directory.Exists(ImgRootPath))
		{
			Directory.CreateDirectory(ImgRootPath);
		}
		if (!Directory.Exists(VersionsPath))
		{
			Directory.CreateDirectory(VersionsPath);
		}
		if (!Directory.Exists(CustomPath))
		{
			Directory.CreateDirectory(CustomPath);
		}
	}
}
