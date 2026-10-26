using System;
using System.IO;

namespace MapleLib.Img;

public static class HaCreatorPaths
{
	public const string ApplicationName = "HaCreator";

	public const string ConfigFileName = "config.json";

	public const string DataFolderName = "Data";

	public const string VersionsFolderName = "versions";

	public const string CustomFolderName = "custom";

	public static string AppDataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HaCreator");

	public static string DefaultConfigPath => Path.Combine(AppDataRoot, "config.json");

	public static string DefaultDataPath => Path.Combine(AppDataRoot, "Data");

	public static string GetVersionsPath(string dataRoot)
	{
		return Path.Combine(dataRoot, "versions");
	}

	public static string GetCustomPath(string dataRoot)
	{
		return Path.Combine(dataRoot, "custom");
	}
}
