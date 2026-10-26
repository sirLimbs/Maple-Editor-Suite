using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class CategoryIndex
{
	[JsonProperty("version")]
	public int Version { get; set; } = 1;

	[JsonProperty("category")]
	public string Category { get; set; }

	[JsonProperty("generatedAt")]
	public DateTime GeneratedAt { get; set; }

	[JsonProperty("totalImageCount")]
	public int TotalImageCount { get; set; }

	[JsonProperty("totalSizeBytes")]
	public long TotalSizeBytes { get; set; }

	[JsonProperty("images")]
	public List<ImageIndexEntry> Images { get; set; } = new List<ImageIndexEntry>();

	[JsonProperty("subdirectories")]
	public List<SubdirectoryEntry> Subdirectories { get; set; } = new List<SubdirectoryEntry>();

	[JsonIgnore]
	public IEnumerable<string> AllImagePaths
	{
		get
		{
			foreach (ImageIndexEntry img in Images)
			{
				yield return img.RelativePath;
			}
			foreach (SubdirectoryEntry subdir in Subdirectories)
			{
				using List<ImageIndexEntry>.Enumerator enumerator3 = subdir.Images.GetEnumerator();
				while (enumerator3.MoveNext())
				{
					yield return Path.Combine(path2: enumerator3.Current.RelativePath, path1: subdir.Name);
				}
			}
		}
	}

	public static CategoryIndex BuildFromDirectory(string categoryPath, string categoryName)
	{
		CategoryIndex categoryIndex = new CategoryIndex
		{
			Category = categoryName,
			GeneratedAt = DateTime.UtcNow
		};
		if (!Directory.Exists(categoryPath))
		{
			return categoryIndex;
		}
		foreach (string item in Directory.EnumerateFiles(categoryPath, "*.img"))
		{
			FileInfo fileInfo = new FileInfo(item);
			categoryIndex.Images.Add(new ImageIndexEntry
			{
				Name = fileInfo.Name,
				RelativePath = fileInfo.Name,
				SizeBytes = fileInfo.Length,
				LastModified = fileInfo.LastWriteTimeUtc
			});
			categoryIndex.TotalSizeBytes += fileInfo.Length;
			categoryIndex.TotalImageCount++;
		}
		foreach (string item2 in Directory.EnumerateDirectories(categoryPath))
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(item2);
			SubdirectoryEntry subdirectoryEntry = new SubdirectoryEntry
			{
				Name = directoryInfo.Name
			};
			IndexDirectoryRecursive(item2, directoryInfo.Name, subdirectoryEntry, categoryIndex);
			categoryIndex.Subdirectories.Add(subdirectoryEntry);
		}
		return categoryIndex;
	}

	private static void IndexDirectoryRecursive(string dirPath, string relativePath, SubdirectoryEntry entry, CategoryIndex index)
	{
		foreach (string item in Directory.EnumerateFiles(dirPath, "*.img"))
		{
			FileInfo fileInfo = new FileInfo(item);
			string fileName = Path.GetFileName(item);
			entry.Images.Add(new ImageIndexEntry
			{
				Name = fileInfo.Name,
				RelativePath = fileName,
				SizeBytes = fileInfo.Length,
				LastModified = fileInfo.LastWriteTimeUtc
			});
			index.TotalSizeBytes += fileInfo.Length;
			index.TotalImageCount++;
		}
		foreach (string item2 in Directory.EnumerateDirectories(dirPath))
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(item2);
			SubdirectoryEntry subdirectoryEntry = new SubdirectoryEntry
			{
				Name = directoryInfo.Name
			};
			IndexDirectoryRecursive(item2, Path.Combine(relativePath, directoryInfo.Name), subdirectoryEntry, index);
			entry.Subdirectories.Add(subdirectoryEntry);
		}
	}

	public void Save(string indexPath)
	{
		string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
		File.WriteAllText(indexPath, contents);
	}

	public static CategoryIndex Load(string indexPath)
	{
		if (!File.Exists(indexPath))
		{
			return null;
		}
		try
		{
			string value = File.ReadAllText(indexPath);
			return JsonConvert.DeserializeObject<CategoryIndex>(value);
		}
		catch
		{
			return null;
		}
	}

	public bool IsStale(string categoryPath)
	{
		if (!Directory.Exists(categoryPath))
		{
			return true;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(categoryPath);
		return directoryInfo.LastWriteTimeUtc > GeneratedAt;
	}
}
