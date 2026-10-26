using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public sealed class WzArchiveSession : IDisposable
{
	private bool _disposed;

	public string Path { get; }

	public WzFile File { get; }

	public bool IsDirty { get; private set; }

	public short GameVersion => File.Version;

	public string FileName => System.IO.Path.GetFileName(Path);

	public WzArchiveSession(string path, WzFile file)
	{
		Path = path;
		File = file;
	}

	public static WzArchiveSession Open(string path, WzMapleVersion? version = null)
	{
		WzMapleVersion[] candidates = (version.HasValue ? new WzMapleVersion[1] { version.Value } : new WzMapleVersion[3]
		{
			WzMapleVersion.GMS,
			WzMapleVersion.EMS,
			WzMapleVersion.BMS
		});
		WzFileParseStatus lastStatus = WzFileParseStatus.Success;
		foreach (WzMapleVersion candidate in candidates)
		{
			WzFile wzFile = new WzFile(path, candidate);
			try
			{
				lastStatus = wzFile.ParseWzFile();
				if (lastStatus == WzFileParseStatus.Success)
				{
					return new WzArchiveSession(path, wzFile);
				}
			}
			catch
			{
			}
			wzFile.Dispose();
		}
		throw new InvalidOperationException("Could not parse " + System.IO.Path.GetFileName(path) + ": " + lastStatus);
	}

	public void MarkDirty()
	{
		IsDirty = true;
	}

	public WzDirectory? GetDirectory(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return File.WzDirectory;
		}
		WzDirectory? current = File.WzDirectory;
		foreach (string part in relativePath.Split(new char[2] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (current == null)
			{
				return null;
			}
			current = current.WzDirectories.FirstOrDefault((WzDirectory d) => string.Equals(d.Name, part, StringComparison.OrdinalIgnoreCase));
		}
		return current;
	}

	public IEnumerable<WzImage> EnumerateImages(string relativeDirectory = "", bool recursive = true)
	{
		WzDirectory? directory = GetDirectory(relativeDirectory);
		if (directory == null)
		{
			yield break;
		}
		foreach (WzImage image in directory.WzImages)
		{
			yield return image;
		}
		if (!recursive)
		{
			yield break;
		}
		foreach (WzDirectory subDirectory in directory.WzDirectories)
		{
			foreach (WzImage image2 in EnumerateDirectoryImages(subDirectory))
			{
				yield return image2;
			}
		}
	}

	private static IEnumerable<WzImage> EnumerateDirectoryImages(WzDirectory directory)
	{
		foreach (WzImage image in directory.WzImages)
		{
			yield return image;
		}
		foreach (WzDirectory subDirectory in directory.WzDirectories)
		{
			foreach (WzImage image2 in EnumerateDirectoryImages(subDirectory))
			{
				yield return image2;
			}
		}
	}

	public WzImage? FindImage(string imageName, bool recursive = true)
	{
		string target = EnsureImageExtension(imageName);
		foreach (WzImage image in EnumerateImages("", recursive))
		{
			if (string.Equals(image.Name, target, StringComparison.OrdinalIgnoreCase))
			{
				return image;
			}
		}
		return null;
	}

	public WzImage? FindImageByPath(string relativePath)
	{
		string[] parts = relativePath.Replace('\\', '/').Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return null;
		}
		string imageName = EnsureImageExtension(parts[parts.Length - 1]);
		WzDirectory? directory = (parts.Length == 1) ? File.WzDirectory : GetDirectory(string.Join("/", parts, 0, parts.Length - 1));
		if (directory == null)
		{
			return null;
		}
		return directory.WzImages.FirstOrDefault((WzImage i) => string.Equals(i.Name, imageName, StringComparison.OrdinalIgnoreCase));
	}

	private static string EnsureImageExtension(string name)
	{
		if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".img.xml", StringComparison.OrdinalIgnoreCase))
		{
			return name;
		}
		return name + ".img";
	}

	public void Save(bool createBackup = true)
	{
		if (!IsDirty)
		{
			return;
		}
		string tempPath = Path + ".new";
		try
		{
			if (System.IO.File.Exists(tempPath))
			{
				System.IO.File.Delete(tempPath);
			}
			File.SaveToDisk(tempPath);
			if (createBackup && System.IO.File.Exists(Path))
			{
				string backup = Path + ".bak";
				try
				{
					System.IO.File.Copy(Path, backup, overwrite: true);
				}
				catch
				{
				}
			}
			CloseReader();
			try
			{
				System.IO.File.Move(tempPath, Path, overwrite: true);
			}
			catch
			{
				ReopenReader();
				throw;
			}
			ReopenReader();
			ResetChangedFlags(File.WzDirectory);
			IsDirty = false;
		}
		finally
		{
			try
			{
				if (System.IO.File.Exists(tempPath))
				{
					System.IO.File.Delete(tempPath);
				}
			}
			catch
			{
			}
		}
	}

	private void CloseReader()
	{
		try
		{
			File.WzDirectory.reader?.Close();
		}
		catch
		{
		}
	}

	private void ReopenReader()
	{
		System.IO.FileStream stream = System.IO.File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
		WzBinaryReader reader = new WzBinaryReader(stream, File.WzIv, 0L)
		{
			Hash = File.versionHash,
			Header = File.Header
		};
		AssignReader(File.WzDirectory, reader);
	}

	private static void AssignReader(WzDirectory directory, WzBinaryReader reader)
	{
		directory.reader = reader;
		foreach (WzImage image in directory.WzImages)
		{
			image.reader = reader;
		}
		foreach (WzDirectory subDirectory in directory.WzDirectories)
		{
			AssignReader(subDirectory, reader);
		}
	}

	private static void ResetChangedFlags(WzDirectory directory)
	{
		foreach (WzImage image in directory.WzImages)
		{
			image.Changed = false;
		}
		foreach (WzDirectory subDirectory in directory.WzDirectories)
		{
			ResetChangedFlags(subDirectory);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			File.Dispose();
		}
	}
}

public sealed class WzStringLookup : IDisposable
{
	private readonly WzArchiveSession _session;

	private readonly Dictionary<string, WzImage> _images = new Dictionary<string, WzImage>(StringComparer.OrdinalIgnoreCase);

	public WzArchiveSession Session => _session;

	public short GameVersion => _session.GameVersion;

	public WzStringLookup(WzArchiveSession session)
	{
		_session = session;
	}

	public static WzStringLookup? TryOpen(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return null;
		}
		try
		{
			return new WzStringLookup(WzArchiveSession.Open(path));
		}
		catch
		{
			return null;
		}
	}

	public string? GetMobName(string id)
	{
		return GetName("Mob.img", NormalizeId(id));
	}

	public string? GetConsumeName(string id)
	{
		return GetName("Consume.img", NormalizeId(id));
	}

	public string? GetEtcName(string id)
	{
		return GetName("Etc.img", NormalizeId(id));
	}

	public string? GetInstallName(string id)
	{
		string normalized = NormalizeId(id);
		return GetName("Ins.img", normalized) ?? GetName("Install.img", normalized);
	}

	public string? GetCashName(string id)
	{
		return GetName("Cash.img", NormalizeId(id));
	}

	public string? GetPetName(string id)
	{
		return GetName("Pet.img", NormalizeId(id));
	}

	public string? GetItemName(string category, string id)
	{
		if (string.IsNullOrWhiteSpace(category))
		{
			return null;
		}
		switch (category.ToLowerInvariant())
		{
		case "consume":
			return GetConsumeName(id);
		case "etc":
			return GetEtcName(id);
		case "install":
			return GetInstallName(id);
		case "cash":
			return GetCashName(id);
		case "pet":
			return GetPetName(id);
		case "taming":
		case "tamingmob":
			return GetEquipName("Taming", id);
		default:
			return GetEquipName(category, id);
		}
	}

	public string? GetEquipName(string category, string id)
	{
		WzImage? image = GetImage("Eqp.img");
		if (image == null)
		{
			return null;
		}
		IPropertyContainer? container = image["Eqp"] as IPropertyContainer;
		container = container?[category] as IPropertyContainer;
		if (container == null)
		{
			return null;
		}
		string normalized = NormalizeId(id);
		WzImageProperty? entry = container[id] ?? container[normalized];
		if (entry == null)
		{
			entry = container[EnsureEightDigits(id)];
		}
		return (entry?["name"] as WzStringProperty)?.Value;
	}

	private string? GetName(string imageName, string id)
	{
		WzImage? image = GetImage(imageName);
		if (image == null)
		{
			return null;
		}
		WzImageProperty? entry = image[id];
		if (entry == null)
		{
			entry = image[EnsureEightDigits(id)];
		}
		return (entry?["name"] as WzStringProperty)?.Value;
	}

	private WzImage? GetImage(string name)
	{
		if (_images.TryGetValue(name, out WzImage? image))
		{
			return image;
		}
		image = _session.FindImage(name, recursive: false) ?? _session.FindImage(name);
		if (image != null)
		{
			image.ParseImage();
			_images[name] = image;
		}
		return image;
	}

	public static string NormalizeId(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return id;
		}
		string trimmed = id.TrimStart('0');
		return (trimmed.Length == 0) ? "0" : trimmed;
	}

	private static string EnsureEightDigits(string id)
	{
		if (int.TryParse(id, out int value))
		{
			return value.ToString("D8");
		}
		return id;
	}

	public void Dispose()
	{
		_session.Dispose();
	}
}
