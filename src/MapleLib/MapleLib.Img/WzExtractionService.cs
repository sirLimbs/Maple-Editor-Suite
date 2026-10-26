#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MapleLib.ClientLib;
using MapleLib.WzLib;
using MapleLib.WzLib.MSFile;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class WzExtractionService
{
	public static readonly string[] STANDARD_WZ_FILES = new string[17]
	{
		"Base", "String", "Map", "Mob", "Npc", "Reactor", "Sound", "Skill", "Character", "Item",
		"UI", "Effect", "Etc", "Quest", "Morph", "TamingMob", "List"
	};

	public static readonly string[] REQUIRED_WZ_FILES = new string[2] { "String", "Map" };

	private const string MANIFEST_FILENAME = "manifest.json";

	private const string IMG_CASE_MAP_FILENAME = ".imgcase.json";

	private static readonly int MAX_EXTRACTION_PARALLELISM = Math.Max(1, Environment.ProcessorCount - 1);

	public event EventHandler<ExtractionProgressEventArgs> ProgressChanged;

	public event EventHandler<CategoryExtractionEventArgs> CategoryStarted;

	public event EventHandler<CategoryExtractionEventArgs> CategoryCompleted;

	public event EventHandler<ExtractionErrorEventArgs> ErrorOccurred;

	public async Task<ExtractionResult> ExtractAsync(string mapleStoryPath, string outputVersionPath, string versionId, string displayName, WzMapleVersion encryption, bool resolveLinks = false, CancellationToken cancellationToken = default(CancellationToken), IProgress<ExtractionProgress> progress = null, WzMapleVersion? outputEncryption = null)
	{
		bool is64Bit = WzFileManager.Detect64BitDirectoryWzFileFormat(mapleStoryPath);
		bool isPreBB = WzFileManager.DetectIsPreBBDataWZFileFormat(mapleStoryPath, encryption);
		List<string> allCategories = DiscoverWzFiles(mapleStoryPath, is64Bit, isPreBB);
		return await ExtractAsync(mapleStoryPath, outputVersionPath, versionId, displayName, encryption, allCategories, resolveLinks, cancellationToken, progress, outputEncryption);
	}

	public async Task<ExtractionResult> ExtractAsync(string mapleStoryPath, string outputVersionPath, string versionId, string displayName, WzMapleVersion encryption, IEnumerable<string> categoriesToExtract, bool resolveLinks = false, CancellationToken cancellationToken = default(CancellationToken), IProgress<ExtractionProgress> progress = null, WzMapleVersion? outputEncryption = null)
	{
		ExtractionResult result = new ExtractionResult
		{
			VersionId = versionId,
			OutputPath = outputVersionPath,
			StartTime = DateTime.Now
		};
		ExtractionProgress progressData = new ExtractionProgress
		{
			CurrentPhase = "Initializing",
			TotalFiles = 0,
			ProcessedFiles = 0
		};
		WzMapleVersion effectiveOutputEncryption = outputEncryption ?? WzMapleVersion.BMS;
		try
		{
			bool is64Bit = WzFileManager.Detect64BitDirectoryWzFileFormat(mapleStoryPath);
			bool isPreBB = WzFileManager.DetectIsPreBBDataWZFileFormat(mapleStoryPath, encryption);
			bool isBetaMs = WzFileManager.DetectBetaDataWzFormat(mapleStoryPath);
			bool isBigBang2 = WzFileManager.DetectBigBang2Format(mapleStoryPath, encryption, is64Bit);
			if (!Directory.Exists(outputVersionPath))
			{
				Directory.CreateDirectory(outputVersionPath);
			}
			HashSet<string> listWzEntries = null;
			ConcurrentDictionary<string, byte> extractedListWzImages = null;
			string listWzFilePath = Path.Combine(mapleStoryPath, "List.wz");
			if (File.Exists(listWzFilePath) && WzTool.IsListFile(listWzFilePath))
			{
				try
				{
					List<string> entries = ListFileParser.ParseListFile(listWzFilePath, encryption);
					listWzEntries = new HashSet<string>(entries, StringComparer.OrdinalIgnoreCase);
					extractedListWzImages = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
					Debug.WriteLine($"[WzExtraction] Pre-parsed List.wz with {listWzEntries.Count} entries");
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Debug.WriteLine("[WzExtraction] Failed to pre-parse List.wz: " + ex2.Message);
				}
			}
			List<string> wzFilesToExtract = categoriesToExtract.ToList();
			if (wzFilesToExtract.Contains("Packs", StringComparer.OrdinalIgnoreCase))
			{
				HashSet<string> categoriesInPacks = GetCategoriesInPacks(mapleStoryPath);
				List<string> skippedCategories = new List<string>();
				foreach (string packCategory in categoriesInPacks)
				{
					if (wzFilesToExtract.Contains(packCategory, StringComparer.OrdinalIgnoreCase) && !packCategory.Equals("Packs", StringComparison.OrdinalIgnoreCase))
					{
						wzFilesToExtract.RemoveAll((string c) => c.Equals(packCategory, StringComparison.OrdinalIgnoreCase));
						skippedCategories.Add(packCategory);
					}
				}
				if (skippedCategories.Count > 0)
				{
					Debug.WriteLine($"[WzExtraction] Skipping {skippedCategories.Count} categories covered by Packs: {string.Join(", ", skippedCategories)}");
				}
			}
			progressData.TotalFiles = CountTotalImages(wzFilesToExtract, mapleStoryPath, encryption, is64Bit);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			progress?.Report(progressData);
			Debug.WriteLine($"[WzExtraction] Starting concurrent extraction of {wzFilesToExtract.Count} categories with parallelism {MAX_EXTRACTION_PARALLELISM}");
			ConcurrentDictionary<string, CategoryExtractionResult> extractionResults = new ConcurrentDictionary<string, CategoryExtractionResult>();
			ConcurrentDictionary<string, byte> activeCategories = new ConcurrentDictionary<string, byte>();
			int processedFiles = 0;
			int completedCategories = 0;
			int totalCategories = wzFilesToExtract.Count;
			ParallelOptions parallelOptions = new ParallelOptions
			{
				MaxDegreeOfParallelism = MAX_EXTRACTION_PARALLELISM,
				CancellationToken = cancellationToken
			};
			await Parallel.ForEachAsync(wzFilesToExtract, parallelOptions, async (string wzCategory, CancellationToken ct) =>
			{
				ct.ThrowIfCancellationRequested();
				activeCategories.TryAdd(wzCategory, 0);
				Debug.WriteLine("[WzExtraction] Starting extraction of " + wzCategory);
				OnCategoryStarted(wzCategory);
				CategoryExtractionResult categoryResult2 = await ExtractCategoryAsync(mapleStoryPath, outputVersionPath, wzCategory, encryption, effectiveOutputEncryption, is64Bit, isPreBB, resolveLinks, listWzEntries, extractedListWzImages, ct, (int current, int total, string fileName) =>
				{
					int processedFiles2 = Interlocked.Increment(ref processedFiles);
					progressData.ProcessedFiles = processedFiles2;
					int count = activeCategories.Count;
					int value2 = completedCategories;
					progressData.CurrentPhase = $"Extracting ({count} active, {value2}/{totalCategories} complete)";
					progressData.CurrentFile = string.Join(", ", activeCategories.Keys.Take(4)) + ((count > 4) ? $" +{count - 4} more" : "");
					progress?.Report(progressData);
					OnProgressChanged(progressData);
				});
				activeCategories.TryRemove(wzCategory, out var _);
				Interlocked.Increment(ref completedCategories);
				extractionResults.TryAdd(wzCategory, categoryResult2);
				OnCategoryCompleted(wzCategory, categoryResult2);
				Debug.WriteLine($"[WzExtraction] Completed extraction of {wzCategory}: {categoryResult2.ImagesExtracted} images");
			});
			foreach (KeyValuePair<string, CategoryExtractionResult> kvp in extractionResults)
			{
				result.CategoriesExtracted.Add(kvp.Key, kvp.Value);
			}
			progressData.CurrentPhase = "Generating manifest";
			progress?.Report(progressData);
			short detectedPatchVersion = 0;
			string detectedLocale = null;
			foreach (CategoryExtractionResult categoryResult in result.CategoriesExtracted.Values)
			{
				if (detectedPatchVersion == 0 && categoryResult.DetectedPatchVersion > 0)
				{
					detectedPatchVersion = categoryResult.DetectedPatchVersion;
				}
				if (string.IsNullOrEmpty(detectedLocale) && !string.IsNullOrEmpty(categoryResult.DetectedLocale))
				{
					detectedLocale = categoryResult.DetectedLocale;
				}
				if (detectedPatchVersion > 0 && !string.IsNullOrEmpty(detectedLocale))
				{
					break;
				}
			}
			VersionInfo versionInfo = CreateVersionInfo(versionId, displayName, effectiveOutputEncryption, is64Bit, isPreBB, isBetaMs, isBigBang2, outputVersionPath, result.CategoriesExtracted, detectedPatchVersion, detectedLocale);
			SaveManifest(outputVersionPath, versionInfo);
			result.VersionInfo = versionInfo;
			progressData.CurrentPhase = "Validating";
			progress?.Report(progressData);
			ExtractionValidationResult validationResult = ValidateExtraction(outputVersionPath, result.CategoriesExtracted.Keys.ToList());
			result.ValidationResult = validationResult;
			if (listWzEntries != null && listWzEntries.Count > 0)
			{
				WriteFilteredListJson(outputVersionPath, listWzEntries, extractedListWzImages);
			}
			result.Success = result.TotalImagesExtracted > 0;
			result.EndTime = DateTime.Now;
			progressData.CurrentPhase = "Complete";
			progress?.Report(progressData);
		}
		catch (OperationCanceledException)
		{
			result.Success = false;
			result.ErrorMessage = "Extraction was cancelled";
			progressData.IsCancelled = true;
			progress?.Report(progressData);
		}
		catch (Exception ex)
		{
			Exception ex4 = ex;
			result.Success = false;
			result.ErrorMessage = ex4.Message;
			progressData.Errors.Add(ex4.Message);
			progress?.Report(progressData);
			OnErrorOccurred(ex4);
		}
		return result;
	}

	public async Task<CategoryExtractionResult> ExtractCategoryAsync(string mapleStoryPath, string outputVersionPath, string category, WzMapleVersion encryption, WzMapleVersion outputEncryption, bool is64Bit, bool isPreBB, bool resolveLinks, HashSet<string> listWzEntries, ConcurrentDictionary<string, byte> extractedListWzImages, CancellationToken cancellationToken, Action<int, int, string> progressCallback = null)
	{
		CategoryExtractionResult result = new CategoryExtractionResult
		{
			CategoryName = category,
			StartTime = DateTime.Now
		};
		string categoryOutputPath = Path.Combine(outputVersionPath, category);
		if (!Directory.Exists(categoryOutputPath))
		{
			Directory.CreateDirectory(categoryOutputPath);
		}
		WzLinkResolver linkResolver = (resolveLinks ? new WzLinkResolver() : null);
		bool isBetaDataWz = WzFileManager.DetectBetaDataWzFormat(mapleStoryPath);
		try
		{
			List<string> wzFilePaths = FindCategoryWzFiles(mapleStoryPath, category, is64Bit, isBetaDataWz);
			bool isPacksCategory = category.Equals("Packs", StringComparison.OrdinalIgnoreCase);
			byte[] outputIv = WzTool.GetIvByMapleVersion(outputEncryption);
			WzImgSerializer serializer = new WzImgSerializer(outputIv);
			int processedCount = 0;
			Dictionary<string, string> extractedImageCaseMap = new Dictionary<string, string>(StringComparer.Ordinal);
			List<(WzFile wzFile, bool isCanvasFile, string relativePath)> loadedWzFiles = new List<(WzFile, bool, string)>();
			List<WzMsFile> loadedMsFiles = new List<WzMsFile>();
			string categoryRootPath = (is64Bit ? Path.Combine(mapleStoryPath, "Data", category) : null);
			await Task.Run(() =>
			{
				try
				{
					if (isPacksCategory)
					{
						ExtractPacksMsFiles(wzFilePaths, outputVersionPath, encryption, serializer, linkResolver, listWzEntries, extractedListWzImages, loadedMsFiles, ref processedCount, progressCallback, result, cancellationToken);
					}
					else
					{
						if (!isBetaDataWz)
						{
							foreach (string item4 in wzFilePaths)
							{
								cancellationToken.ThrowIfCancellationRequested();
								if (File.Exists(item4))
								{
									if (WzTool.IsListFile(item4))
									{
										result.ImagesExtracted++;
									}
									else
									{
										WzFile wzFile = new WzFile(item4, encryption);
										WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
										if (wzFileParseStatus != WzFileParseStatus.Success)
										{
											result.Errors.Add($"Failed to parse {item4}: {wzFileParseStatus}");
											wzFile.Dispose();
										}
										else
										{
											if (result.DetectedPatchVersion == 0 && wzFile.Version > 0)
											{
												result.DetectedPatchVersion = wzFile.Version;
												Debug.WriteLine($"[WzExtraction] Detected patch version {wzFile.Version} from {item4}");
											}
											if (string.IsNullOrEmpty(result.DetectedLocale) && wzFile.MapleLocaleVersion != MapleStoryLocalisation.Not_Known)
											{
												result.DetectedLocale = wzFile.MapleLocaleVersion.ToString();
												Debug.WriteLine("[WzExtraction] Detected locale " + result.DetectedLocale + " from " + item4);
											}
											bool item = item4.Contains("_Canvas", StringComparison.OrdinalIgnoreCase);
											string item2 = "";
											if (categoryRootPath != null)
											{
												string directoryName = Path.GetDirectoryName(item4);
												if (directoryName.StartsWith(categoryRootPath, StringComparison.OrdinalIgnoreCase))
												{
													item2 = directoryName.Substring(categoryRootPath.Length).TrimStart(Path.DirectorySeparatorChar);
												}
											}
											loadedWzFiles.Add((wzFile, item, item2));
										}
									}
								}
							}
							if (linkResolver != null && loadedWzFiles.Count > 0)
							{
								linkResolver.SetCategoryWzFiles(loadedWzFiles.Select(((WzFile wzFile, bool isCanvasFile, string relativePath) x) => x.wzFile).ToList(), category);
							}
							{
								foreach (var (wzFile2, flag, text) in loadedWzFiles)
								{
									cancellationToken.ThrowIfCancellationRequested();
									if (!flag)
									{
										string text2 = categoryOutputPath;
										if (!string.IsNullOrEmpty(text))
										{
											text2 = Path.Combine(categoryOutputPath, text);
											if (!Directory.Exists(text2))
											{
												Directory.CreateDirectory(text2);
											}
										}
										ExtractWzNode(wzFile2.WzDirectory, text2, categoryOutputPath, category, serializer, linkResolver, listWzEntries, extractedListWzImages, extractedImageCaseMap, ref processedCount, progressCallback, result);
									}
								}
								return;
							}
						}
						ExtractBetaDataWzCategory(wzFilePaths, category, categoryOutputPath, encryption, serializer, linkResolver, listWzEntries, extractedListWzImages, extractedImageCaseMap, loadedWzFiles, ref processedCount, progressCallback, result, cancellationToken);
					}
				}
				finally
				{
					foreach (var item5 in loadedWzFiles)
					{
						WzFile item3 = item5.wzFile;
						item3.Dispose();
					}
					loadedWzFiles.Clear();
					foreach (WzMsFile item6 in loadedMsFiles)
					{
						item6.Dispose();
					}
					loadedMsFiles.Clear();
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
			}, cancellationToken);
			result.Success = true;
			result.ImagesExtracted = processedCount;
			if (linkResolver != null)
			{
				result.LinksResolved = linkResolver.LinksResolved;
				result.LinksFailed = linkResolver.LinksFailed;
				if (linkResolver.FailedLinks.Count > 0)
				{
					foreach (string failedLink in linkResolver.FailedLinks.Take(10))
					{
						result.Errors.Add("Broken link (missing data in original WZ): " + failedLink);
					}
					if (linkResolver.FailedLinks.Count > 10)
					{
						result.Errors.Add($"... and {linkResolver.FailedLinks.Count - 10} more broken links (missing data in original WZ files)");
					}
				}
			}
			if (!isPacksCategory && extractedImageCaseMap.Count > 0)
			{
				SaveImageCaseMap(categoryOutputPath, extractedImageCaseMap);
			}
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.Errors.Add(ex.Message);
		}
		result.EndTime = DateTime.Now;
		return result;
	}

	public ExtractionValidationResult ValidateExtraction(string versionPath, List<string> extractedCategories = null)
	{
		ExtractionValidationResult extractionValidationResult = new ExtractionValidationResult
		{
			VersionPath = versionPath,
			CheckedAt = DateTime.Now
		};
		bool flag = extractedCategories?.Contains("String", StringComparer.OrdinalIgnoreCase) ?? true;
		bool flag2 = extractedCategories?.Contains("Map", StringComparer.OrdinalIgnoreCase) ?? true;
		if (flag)
		{
			string text = Path.Combine(versionPath, "String");
			if (!Directory.Exists(text))
			{
				extractionValidationResult.Errors.Add("String category is missing");
			}
			else
			{
				string path = Path.Combine(text, "Map.img");
				if (!File.Exists(path))
				{
					extractionValidationResult.Errors.Add("String/Map.img is missing");
				}
			}
		}
		if (flag2)
		{
			string path2 = Path.Combine(versionPath, "Map");
			if (!Directory.Exists(path2))
			{
				extractionValidationResult.Errors.Add("Map category is missing");
			}
			else
			{
				int num = Directory.EnumerateFiles(path2, "*.img", SearchOption.AllDirectories).Count();
				if (num == 0)
				{
					extractionValidationResult.Errors.Add("Map category has no .img files");
				}
				extractionValidationResult.TotalImageCount += num;
			}
		}
		string[] sTANDARD_WZ_FILES = STANDARD_WZ_FILES;
		foreach (string text2 in sTANDARD_WZ_FILES)
		{
			string path3 = Path.Combine(versionPath, text2);
			if (Directory.Exists(path3))
			{
				int num2 = Directory.EnumerateFiles(path3, "*.img", SearchOption.AllDirectories).Count();
				extractionValidationResult.CategoryImageCounts[text2] = num2;
				if (text2 != "Map")
				{
					extractionValidationResult.TotalImageCount += num2;
				}
			}
		}
		extractionValidationResult.IsValid = extractionValidationResult.Errors.Count == 0;
		return extractionValidationResult;
	}

	private List<string> DiscoverWzFiles(string mapleStoryPath, bool is64Bit, bool isPreBB)
	{
		List<string> list = new List<string>();
		if (WzFileManager.DetectBetaDataWzFormat(mapleStoryPath))
		{
			list.Add("Data");
		}
		else if (is64Bit)
		{
			string text = Path.Combine(mapleStoryPath, "Data");
			if (Directory.Exists(text))
			{
				string[] sTANDARD_WZ_FILES = STANDARD_WZ_FILES;
				foreach (string text2 in sTANDARD_WZ_FILES)
				{
					string path = Path.Combine(text, text2);
					if (Directory.Exists(path))
					{
						list.Add(text2);
					}
				}
				foreach (string item in Directory.EnumerateDirectories(text))
				{
					string fileName = Path.GetFileName(item);
					if (!fileName.Equals("Packs", StringComparison.OrdinalIgnoreCase) && !list.Contains(fileName, StringComparer.OrdinalIgnoreCase))
					{
						list.Add(fileName);
					}
				}
				string path2 = Path.Combine(text, "Packs");
				if (Directory.Exists(path2))
				{
					list.Add("Packs");
				}
			}
		}
		else
		{
			string[] sTANDARD_WZ_FILES2 = STANDARD_WZ_FILES;
			foreach (string text3 in sTANDARD_WZ_FILES2)
			{
				string path3 = Path.Combine(mapleStoryPath, text3 + ".wz");
				if (File.Exists(path3))
				{
					list.Add(text3);
				}
			}
			foreach (string item2 in Directory.EnumerateFiles(mapleStoryPath, "*.wz"))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2);
				if (list.Contains(fileNameWithoutExtension, StringComparer.OrdinalIgnoreCase))
				{
					continue;
				}
				bool flag = false;
				foreach (string item3 in list)
				{
					if (fileNameWithoutExtension.StartsWith(item3, StringComparison.OrdinalIgnoreCase) && fileNameWithoutExtension.Length > item3.Length && char.IsDigit(fileNameWithoutExtension[item3.Length]))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(fileNameWithoutExtension);
				}
			}
		}
		return list;
	}

	private List<string> FindCategoryWzFiles(string mapleStoryPath, string category, bool is64Bit, bool isBetaDataWz = false)
	{
		List<string> list = new List<string>();
		if (isBetaDataWz)
		{
			string text = Path.Combine(mapleStoryPath, "Data.wz");
			if (File.Exists(text))
			{
				list.Add(text);
			}
			return list;
		}
		if (category.Equals("Packs", StringComparison.OrdinalIgnoreCase))
		{
			string path = Path.Combine(mapleStoryPath, "Data", "Packs");
			if (Directory.Exists(path))
			{
				list.AddRange(Directory.EnumerateFiles(path, "*.ms", SearchOption.AllDirectories));
			}
			return list;
		}
		if (is64Bit)
		{
			string path2 = Path.Combine(mapleStoryPath, "Data", category);
			if (Directory.Exists(path2))
			{
				list.AddRange(Directory.EnumerateFiles(path2, "*.wz", SearchOption.AllDirectories));
			}
		}
		else
		{
			string text2 = Path.Combine(mapleStoryPath, category + ".wz");
			if (File.Exists(text2))
			{
				list.Add(text2);
			}
			Regex regex = new Regex("^" + Regex.Escape(category) + "\\d+\\.wz$", RegexOptions.IgnoreCase);
			foreach (string item in Directory.EnumerateFiles(mapleStoryPath, "*.wz"))
			{
				string fileName = Path.GetFileName(item);
				if (regex.IsMatch(fileName) && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private HashSet<string> GetCategoriesInPacks(string mapleStoryPath)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string path = Path.Combine(mapleStoryPath, "Data", "Packs");
		if (!Directory.Exists(path))
		{
			return hashSet;
		}
		foreach (string item2 in Directory.EnumerateFiles(path, "*.ms", SearchOption.AllDirectories))
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2);
			int num = fileNameWithoutExtension.IndexOf('_');
			if (num > 0)
			{
				string item = fileNameWithoutExtension.Substring(0, num);
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	private int CountTotalImages(List<string> categories, string mapleStoryPath, WzMapleVersion encryption, bool is64Bit)
	{
		int num = 0;
		bool flag = WzFileManager.DetectBetaDataWzFormat(mapleStoryPath);
		WzFile wzFile = null;
		if (flag)
		{
			string text = Path.Combine(mapleStoryPath, "Data.wz");
			if (File.Exists(text))
			{
				try
				{
					wzFile = new WzFile(text, encryption);
					wzFile.ParseWzFile();
				}
				catch
				{
					wzFile?.Dispose();
					wzFile = null;
				}
			}
		}
		try
		{
			foreach (string category in categories)
			{
				if (flag && wzFile != null)
				{
					if (category.Equals("_Root", StringComparison.OrdinalIgnoreCase))
					{
						num += wzFile.WzDirectory.WzImages?.Count ?? 0;
						continue;
					}
					foreach (WzDirectory wzDirectory in wzFile.WzDirectory.WzDirectories)
					{
						if (wzDirectory.Name.Equals(category, StringComparison.OrdinalIgnoreCase))
						{
							num += wzDirectory.CountImages();
							break;
						}
					}
					continue;
				}
				List<string> list = FindCategoryWzFiles(mapleStoryPath, category, is64Bit, flag);
				if (category.Equals("Packs", StringComparison.OrdinalIgnoreCase))
				{
					foreach (string item in list)
					{
						if (!File.Exists(item))
						{
							continue;
						}
						try
						{
							FileStream fileStream = File.OpenRead(item);
							MemoryStream memoryStream = new MemoryStream();
							fileStream.CopyTo(memoryStream);
							fileStream.Close();
							memoryStream.Position = 0L;
							using WzMsFile wzMsFile = new WzMsFile(memoryStream, Path.GetFileName(item), item, leaveOpen: true);
							wzMsFile.ReadEntries();
							num += wzMsFile.Entries.Count;
						}
						catch
						{
						}
					}
					continue;
				}
				foreach (string item2 in list)
				{
					if (!File.Exists(item2))
					{
						continue;
					}
					if (WzTool.IsListFile(item2))
					{
						num++;
						continue;
					}
					try
					{
						using WzFile wzFile2 = new WzFile(item2, encryption);
						WzFileParseStatus wzFileParseStatus = wzFile2.ParseWzFile();
						if (wzFileParseStatus == WzFileParseStatus.Success && wzFile2.WzDirectory != null)
						{
							num += wzFile2.WzDirectory.CountImages();
						}
					}
					catch
					{
					}
				}
			}
		}
		finally
		{
			wzFile?.Dispose();
		}
		return num;
	}

	private void ExtractWzNode(WzDirectory directory, string outputPath, string categoryOutputRootPath, string categoryName, WzImgSerializer serializer, WzLinkResolver linkResolver, HashSet<string> listWzEntries, ConcurrentDictionary<string, byte> extractedListWzImages, Dictionary<string, string> extractedImageCaseMap, ref int processedCount, Action<int, int, string> progressCallback, CategoryExtractionResult result)
	{
		if (directory == null)
		{
			return;
		}
		if (directory.WzDirectories != null)
		{
			foreach (WzDirectory wzDirectory in directory.WzDirectories)
			{
				if (wzDirectory != null)
				{
					string text = Path.Combine(outputPath, EscapeFileName(wzDirectory.Name));
					if (!Directory.Exists(text))
					{
						Directory.CreateDirectory(text);
					}
					ExtractWzNode(wzDirectory, text, categoryOutputRootPath, categoryName, serializer, linkResolver, listWzEntries, extractedListWzImages, extractedImageCaseMap, ref processedCount, progressCallback, result);
				}
			}
		}
		if (directory.WzImages == null)
		{
			return;
		}
		foreach (WzImage wzImage in directory.WzImages)
		{
			try
			{
				linkResolver?.ResolveLinksInImage(wzImage);
				string text2 = Path.Combine(outputPath, EscapeFileName(wzImage.Name));
				EnsureExactFilePathCase(text2);
				serializer.SerializeImage(wzImage, text2);
				TryRecordImageCaseMap(categoryOutputRootPath, text2, extractedImageCaseMap);
				processedCount++;
				result.TotalSize += new FileInfo(text2).Length;
				if (listWzEntries != null && extractedListWzImages != null)
				{
					string text3 = BuildListWzPath(categoryName, directory, wzImage.Name);
					if (listWzEntries.Contains(text3))
					{
						extractedListWzImages.TryAdd(text3, 0);
					}
				}
				progressCallback?.Invoke(processedCount, 0, wzImage.Name);
			}
			catch (Exception ex)
			{
				result.Errors.Add("Failed to extract " + wzImage.Name + ": " + ex.Message);
			}
			finally
			{
				wzImage.UnparseImage();
			}
		}
	}

	private string BuildListWzPath(string categoryName, WzDirectory directory, string imageName)
	{
		List<string> list = new List<string> { categoryName };
		WzDirectory wzDirectory = directory;
		Stack<string> stack = new Stack<string>();
		while (wzDirectory != null && wzDirectory.Parent is WzDirectory wzDirectory2)
		{
			stack.Push(wzDirectory.Name);
			wzDirectory = wzDirectory2;
		}
		while (stack.Count > 0)
		{
			list.Add(stack.Pop());
		}
		list.Add(imageName);
		return string.Join("/", list);
	}

	private void ExtractBetaDataWzCategory(List<string> wzFilePaths, string category, string categoryOutputPath, WzMapleVersion encryption, WzImgSerializer serializer, WzLinkResolver linkResolver, HashSet<string> listWzEntries, ConcurrentDictionary<string, byte> extractedListWzImages, Dictionary<string, string> extractedImageCaseMap, List<(WzFile wzFile, bool isCanvasFile, string relativePath)> loadedWzFiles, ref int processedCount, Action<int, int, string> progressCallback, CategoryExtractionResult result, CancellationToken cancellationToken)
	{
		if (wzFilePaths.Count == 0)
		{
			result.Errors.Add("No Data.wz file found for beta extraction");
			return;
		}
		string text = wzFilePaths[0];
		if (!File.Exists(text))
		{
			result.Errors.Add("Data.wz file not found: " + text);
			return;
		}
		WzFile wzFile = new WzFile(text, encryption);
		WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
		if (wzFileParseStatus != WzFileParseStatus.Success)
		{
			result.Errors.Add($"Failed to parse Data.wz: {wzFileParseStatus}");
			wzFile.Dispose();
			return;
		}
		if (wzFile.Version > 0)
		{
			result.DetectedPatchVersion = wzFile.Version;
			Debug.WriteLine($"[WzExtraction] Detected patch version {wzFile.Version} from beta Data.wz");
		}
		if (wzFile.MapleLocaleVersion != MapleStoryLocalisation.Not_Known)
		{
			result.DetectedLocale = wzFile.MapleLocaleVersion.ToString();
			Debug.WriteLine("[WzExtraction] Detected locale " + result.DetectedLocale + " from beta Data.wz");
		}
		loadedWzFiles.Add((wzFile, false, ""));
		try
		{
			WzDirectory wzDirectory = null;
			if (category.Equals("_Root", StringComparison.OrdinalIgnoreCase))
			{
				if (wzFile.WzDirectory.WzImages == null || wzFile.WzDirectory.WzImages.Count <= 0)
				{
					return;
				}
				{
					foreach (WzImage wzImage in wzFile.WzDirectory.WzImages)
					{
						cancellationToken.ThrowIfCancellationRequested();
						try
						{
							linkResolver?.ResolveLinksInImage(wzImage);
							string text2 = Path.Combine(categoryOutputPath, ProgressingWzSerializer.EscapeInvalidFilePathNames(wzImage.Name));
							EnsureExactFilePathCase(text2);
							serializer.SerializeImage(wzImage, text2);
							TryRecordImageCaseMap(categoryOutputPath, text2, extractedImageCaseMap);
							processedCount++;
							result.TotalSize += new FileInfo(text2).Length;
							progressCallback?.Invoke(processedCount, 0, wzImage.Name);
						}
						catch (Exception ex)
						{
							result.Errors.Add("Failed to extract " + wzImage.Name + ": " + ex.Message);
						}
						finally
						{
							wzImage.UnparseImage();
						}
					}
					return;
				}
			}
			foreach (WzDirectory wzDirectory2 in wzFile.WzDirectory.WzDirectories)
			{
				if (wzDirectory2.Name.Equals(category, StringComparison.OrdinalIgnoreCase))
				{
					wzDirectory = wzDirectory2;
					break;
				}
			}
			if (wzDirectory == null)
			{
				result.Errors.Add("Category '" + category + "' not found in Data.wz");
				return;
			}
			linkResolver?.SetCategoryWzFiles(new List<WzFile> { wzFile }, category);
			ExtractWzNode(wzDirectory, categoryOutputPath, categoryOutputPath, category, serializer, linkResolver, listWzEntries, extractedListWzImages, extractedImageCaseMap, ref processedCount, progressCallback, result);
		}
		catch (Exception ex2)
		{
			result.Errors.Add("Error extracting category '" + category + "' from Data.wz: " + ex2.Message);
		}
	}

	private void ExtractPacksMsFiles(List<string> msFilePaths, string outputVersionPath, WzMapleVersion encryption, WzImgSerializer serializer, WzLinkResolver linkResolver, HashSet<string> listWzEntries, ConcurrentDictionary<string, byte> extractedListWzImages, List<WzMsFile> loadedMsFiles, ref int processedCount, Action<int, int, string> progressCallback, CategoryExtractionResult result, CancellationToken cancellationToken)
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (string msFilePath in msFilePaths)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(msFilePath);
			int num = fileNameWithoutExtension.IndexOf('_');
			string key = ((num > 0) ? fileNameWithoutExtension.Substring(0, num) : fileNameWithoutExtension);
			if (!dictionary.ContainsKey(key))
			{
				dictionary[key] = new List<string>();
			}
			dictionary[key].Add(msFilePath);
		}
		string text = null;
		if (msFilePaths.Count > 0)
		{
			string directoryName = Path.GetDirectoryName(msFilePaths[0]);
			string directoryName2 = Path.GetDirectoryName(directoryName);
			text = Path.GetDirectoryName(directoryName2);
		}
		List<WzFile> list = new List<WzFile>();
		try
		{
			foreach (KeyValuePair<string, List<string>> item in dictionary)
			{
				string key2 = item.Key;
				List<string> value = item.Value;
				if (linkResolver != null && text != null)
				{
					string path = Path.Combine(text, "Data", key2);
					if (Directory.Exists(path))
					{
						List<WzFile> list2 = new List<WzFile>();
						List<string> list3 = Directory.EnumerateDirectories(path, "_Canvas", SearchOption.AllDirectories).ToList();
						foreach (string item2 in list3)
						{
							List<string> list4 = Directory.EnumerateFiles(item2, "*.wz", SearchOption.AllDirectories).ToList();
							foreach (string item3 in list4)
							{
								try
								{
									WzFile wzFile = new WzFile(item3, encryption);
									WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
									if (wzFileParseStatus == WzFileParseStatus.Success)
									{
										list2.Add(wzFile);
										list.Add(wzFile);
										continue;
									}
									Debug.WriteLine($"[WzExtraction] Failed to parse _Canvas file {item3}: {wzFileParseStatus}");
									wzFile.Dispose();
								}
								catch (Exception ex)
								{
									Debug.WriteLine("[WzExtraction] Exception loading _Canvas file " + item3 + ": " + ex.Message);
								}
							}
						}
						if (list2.Count > 0)
						{
							linkResolver.SetCategoryWzFiles(list2, key2);
						}
					}
				}
				foreach (string item4 in value)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (!File.Exists(item4))
					{
						continue;
					}
					string fileName = Path.GetFileName(item4);
					try
					{
						FileStream fileStream = File.OpenRead(item4);
						MemoryStream memoryStream = new MemoryStream();
						fileStream.CopyTo(memoryStream);
						fileStream.Close();
						memoryStream.Position = 0L;
						WzMsFile wzMsFile = new WzMsFile(memoryStream, fileName, item4, leaveOpen: true);
						wzMsFile.ReadEntries();
						loadedMsFiles.Add(wzMsFile);
						WzFile wzFile2 = wzMsFile.LoadAsWzFile();
						foreach (WzMsEntry entry in wzMsFile.Entries)
						{
							cancellationToken.ThrowIfCancellationRequested();
							try
							{
								string name = entry.Name;
								int num2 = name.IndexOf('/');
								if (num2 < 0)
								{
									result.Errors.Add("Invalid .ms entry name (no category): " + name);
									continue;
								}
								string path2 = name.Substring(0, num2);
								string text2 = name.Substring(num2 + 1);
								string text3 = Path.Combine(outputVersionPath, path2);
								if (!Directory.Exists(text3))
								{
									Directory.CreateDirectory(text3);
								}
								int num3 = text2.LastIndexOf('/');
								string text5;
								if (num3 >= 0)
								{
									string path3 = text2.Substring(0, num3);
									string fileName2 = text2.Substring(num3 + 1);
									string text4 = Path.Combine(text3, path3);
									if (!Directory.Exists(text4))
									{
										Directory.CreateDirectory(text4);
									}
									text5 = Path.Combine(text4, EscapeFileName(fileName2));
								}
								else
								{
									text5 = Path.Combine(text3, EscapeFileName(text2));
								}
								string imgBaseName = Path.GetFileName(name);
								WzImage wzImage = wzFile2.WzDirectory.WzImages.FirstOrDefault((WzImage img) => img.Name == imgBaseName);
								if (wzImage == null)
								{
									result.Errors.Add("WzImage not found for .ms entry: " + name);
									continue;
								}
								linkResolver?.ResolveLinksInImage(wzImage);
								EnsureExactFilePathCase(text5);
								serializer.SerializeImage(wzImage, text5);
								processedCount++;
								result.TotalSize += new FileInfo(text5).Length;
								if (listWzEntries != null && extractedListWzImages != null && listWzEntries.Contains(name))
								{
									extractedListWzImages.TryAdd(name, 0);
								}
								progressCallback?.Invoke(processedCount, 0, imgBaseName);
							}
							catch (Exception ex2)
							{
								result.Errors.Add("Failed to extract .ms entry " + entry.Name + ": " + ex2.Message);
							}
						}
						wzFile2.Dispose();
					}
					catch (Exception ex3)
					{
						result.Errors.Add("Failed to load .ms file " + fileName + ": " + ex3.Message);
					}
				}
			}
		}
		finally
		{
			foreach (WzFile item5 in list)
			{
				item5.Dispose();
			}
			list.Clear();
		}
	}

	private void WriteFilteredListJson(string outputVersionPath, HashSet<string> listWzEntries, ConcurrentDictionary<string, byte> extractedListWzImages)
	{
		string text = Path.Combine(outputVersionPath, "List");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		List<string> list = listWzEntries.Where((string entry) => !extractedListWzImages.ContainsKey(entry)).ToList();
		List<string> list2 = extractedListWzImages.Keys.ToList();
		var value = new
		{
			Type = "ListWz",
			Description = "Pre-Big Bang List.wz - images with different encryption",
			OriginalCount = listWzEntries.Count,
			DecryptedCount = list2.Count,
			RemainingCount = list.Count,
			Entries = list,
			DecryptedEntries = list2,
			RemainingEntries = list
		};
		string contents = JsonConvert.SerializeObject(value, Formatting.Indented);
		File.WriteAllText(Path.Combine(text, "List.json"), contents);
		Debug.WriteLine($"[WzExtraction] List.wz: {listWzEntries.Count} total, {list2.Count} decrypted, {list.Count} remaining");
	}

	private VersionInfo CreateVersionInfo(string versionId, string displayName, WzMapleVersion encryption, bool is64Bit, bool isPreBB, bool isBetaMs, bool isBigBang2, string outputPath, Dictionary<string, CategoryExtractionResult> categories, short detectedPatchVersion = 0, string detectedLocale = null)
	{
		VersionInfo versionInfo = new VersionInfo
		{
			Version = versionId,
			DisplayName = displayName,
			Encryption = encryption.ToString(),
			Is64Bit = is64Bit,
			IsPreBB = isPreBB,
			IsBetaMs = isBetaMs,
			IsBigBang2 = isBigBang2,
			ExtractedDate = DateTime.Now,
			DirectoryPath = outputPath,
			PatchVersion = detectedPatchVersion,
			SourceRegion = detectedLocale
		};
		foreach (KeyValuePair<string, CategoryExtractionResult> category in categories)
		{
			string path = Path.Combine(outputPath, category.Key);
			if (Directory.Exists(path))
			{
				CategoryInfo categoryInfo = new CategoryInfo
				{
					FileCount = category.Value.ImagesExtracted,
					TotalSize = category.Value.TotalSize,
					LastModified = DateTime.Now
				};
				categoryInfo.Subdirectories = Directory.EnumerateDirectories(path).Select(Path.GetFileName).ToList();
				versionInfo.Categories[category.Key] = categoryInfo;
			}
		}
		return versionInfo;
	}

	private void SaveManifest(string outputPath, VersionInfo versionInfo)
	{
		string path = Path.Combine(outputPath, "manifest.json");
		string contents = JsonConvert.SerializeObject(versionInfo, Formatting.Indented);
		File.WriteAllText(path, contents);
	}

	private string EscapeFileName(string fileName)
	{
		return ProgressingWzSerializer.EscapeInvalidFilePathNames(fileName);
	}

	private void EnsureExactFilePathCase(string desiredFilePath)
	{
		string directoryName = Path.GetDirectoryName(desiredFilePath);
		if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName))
		{
			return;
		}
		string desiredFileName = Path.GetFileName(desiredFilePath);
		string text = Directory.EnumerateFiles(directoryName).FirstOrDefault((string path) => string.Equals(Path.GetFileName(path), desiredFileName, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrEmpty(text))
		{
			string fileName = Path.GetFileName(text);
			if (!string.Equals(fileName, desiredFileName, StringComparison.Ordinal))
			{
				string text2 = Path.Combine(directoryName, $"__casefix_{Guid.NewGuid():N}{Path.GetExtension(desiredFileName)}");
				File.Move(text, text2);
				File.Move(text2, Path.Combine(directoryName, desiredFileName));
			}
		}
	}

	private void TryRecordImageCaseMap(string categoryOutputRootPath, string imageOutputPath, Dictionary<string, string> imageCaseMap)
	{
		if (imageCaseMap == null || string.IsNullOrEmpty(categoryOutputRootPath) || string.IsNullOrEmpty(imageOutputPath))
		{
			return;
		}
		string text = Path.GetRelativePath(categoryOutputRootPath, imageOutputPath).Replace(Path.DirectorySeparatorChar, '/');
		string fileName = Path.GetFileName(text);
		if (fileName.Any(char.IsUpper))
		{
			string key = text.ToLowerInvariant();
			if (!imageCaseMap.ContainsKey(key))
			{
				imageCaseMap[key] = text;
			}
		}
	}

	private void SaveImageCaseMap(string categoryOutputPath, Dictionary<string, string> imageCaseMap)
	{
		try
		{
			ImageCaseMapData value = new ImageCaseMapData
			{
				Format = "ImgCaseMapV1",
				Entries = imageCaseMap.OrderBy((KeyValuePair<string, string> kvp) => kvp.Key, StringComparer.Ordinal).ToDictionary((KeyValuePair<string, string> kvp) => kvp.Key, (KeyValuePair<string, string> kvp) => kvp.Value, StringComparer.Ordinal)
			};
			string contents = JsonConvert.SerializeObject(value, Formatting.Indented);
			string path = Path.Combine(categoryOutputPath, ".imgcase.json");
			File.WriteAllText(path, contents);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[WzExtraction] Failed to save .imgcase.json: " + ex.Message);
		}
	}

	protected virtual void OnProgressChanged(ExtractionProgress progress)
	{
		ProgressChanged?.Invoke(this, new ExtractionProgressEventArgs(progress));
	}

	protected virtual void OnCategoryStarted(string category)
	{
		CategoryStarted?.Invoke(this, new CategoryExtractionEventArgs(category, null));
	}

	protected virtual void OnCategoryCompleted(string category, CategoryExtractionResult result)
	{
		CategoryCompleted?.Invoke(this, new CategoryExtractionEventArgs(category, result));
	}

	protected virtual void OnErrorOccurred(Exception ex)
	{
		ErrorOccurred?.Invoke(this, new ExtractionErrorEventArgs(ex));
	}
}
