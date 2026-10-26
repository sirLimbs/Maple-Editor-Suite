#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MapleLib.WzLib;
using MapleLib.WzLib.Serializer;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;
using Newtonsoft.Json;

namespace MapleLib.Img;

public class WzPackingService
{
	private class ImgFileInfo
	{
		public string FilePath { get; set; }

		public string RelativePath { get; set; }

		public WzDirectory ParentDirectory { get; set; }

		public long FileSize { get; set; }
	}

	private class ImgProcessingResult
	{
		public bool Success { get; set; }

		public WzImage Image { get; set; }

		public WzDirectory ParentDirectory { get; set; }

		public string RelativePath { get; set; }

		public string ErrorMessage { get; set; }
	}

	private class ReferenceWzMetadata
	{
		public string SourcePath { get; set; }

		public Dictionary<string, string> CaseEntries { get; set; }

		public Dictionary<string, int> DirectoryOrder { get; set; }

		public Dictionary<string, int> ImageOrder { get; set; }
	}

	private class CanvasInfo
	{
		public WzCanvasProperty Canvas { get; set; }

		public WzImage SourceImage { get; set; }

		public string PropertyPath { get; set; }

		public long EstimatedSize { get; set; }

		public string OutlinkPath { get; set; }
	}

	public static readonly string[] STANDARD_CATEGORIES = WzExtractionService.STANDARD_WZ_FILES;

	private const string MANIFEST_FILENAME = "manifest.json";

	private const string IMG_CASE_MAP_FILENAME = ".imgcase.json";

	public const string CANVAS_DIRECTORY_NAME = "_Canvas";

	private const long MAX_WZ_FILE_SIZE = 150000000L;

	private const long MAX_CANVAS_FILE_SIZE = 500000000L;

	private const int MAX_IMAGES_PER_CANVAS = 1000;

	private const long CANVAS_SIZE_THRESHOLD = 100000L;

	private static readonly int MAX_DEGREE_OF_PARALLELISM = Math.Max(1, Environment.ProcessorCount - 1);

	private const int MAX_IMG_PARSE_PARALLELISM = 2;

	public event EventHandler<PackingProgressEventArgs> ProgressChanged;

	public event EventHandler<CategoryPackingEventArgs> CategoryStarted;

	public event EventHandler<CategoryPackingEventArgs> CategoryCompleted;

	public event EventHandler<PackingErrorEventArgs> ErrorOccurred;

	public async Task<PackingResult> PackAsync(string versionPath, string outputPath, bool saveAs64Bit = false, CancellationToken cancellationToken = default(CancellationToken), IProgress<PackingProgress> progress = null)
	{
		PackingResult result = new PackingResult
		{
			VersionPath = versionPath,
			OutputPath = outputPath,
			StartTime = DateTime.Now
		};
		PackingProgress progressData = new PackingProgress
		{
			CurrentPhase = "Initializing",
			TotalFiles = 0,
			ProcessedFiles = 0
		};
		try
		{
			VersionInfo versionInfo = LoadVersionInfo(versionPath);
			if (versionInfo == null)
			{
				throw new InvalidOperationException("No manifest.json found in " + versionPath + ". This directory may not be a valid IMG filesystem.");
			}
			result.VersionInfo = versionInfo;
			ResolvePackingSettings(versionInfo, 0, null, out var encryption, out var patchVersion);
			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}
			progressData.TotalFiles = CountTotalImages(versionPath);
			progress?.Report(progressData);
			List<string> categoriesToPack = GetCategoriesToPack(versionPath);
			foreach (string category in categoriesToPack)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progressData.CurrentPhase = "Packing " + category;
				progressData.CurrentFile = category + ".wz";
				progress?.Report(progressData);
				OnCategoryStarted(category);
				CategoryPackingResult categoryResult = await PackCategoryAsync(versionPath, outputPath, category, encryption, patchVersion, saveAs64Bit, cancellationToken, (int current, int total, string fileName) =>
				{
					progressData.CurrentFile = fileName;
					progressData.ProcessedFiles++;
					progress?.Report(progressData);
					OnProgressChanged(progressData);
				});
				result.CategoriesPacked.Add(category, categoryResult);
				OnCategoryCompleted(category, categoryResult);
			}
			result.Success = true;
			result.EndTime = DateTime.Now;
			progressData.CurrentPhase = "Complete";
			progress?.Report(progressData);
		}
		catch (OperationCanceledException)
		{
			result.Success = false;
			result.ErrorMessage = "Packing was cancelled";
			progressData.IsCancelled = true;
			progress?.Report(progressData);
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			result.Success = false;
			result.ErrorMessage = ex3.Message;
			progressData.Errors.Add(ex3.Message);
			progress?.Report(progressData);
			OnErrorOccurred(ex3);
		}
		return result;
	}

	public async Task<CategoryPackingResult> PackCategoryAsync(string versionPath, string outputPath, string category, WzMapleVersion encryption, short patchVersion, bool saveAs64Bit, CancellationToken cancellationToken, Action<int, int, string> progressCallback = null, bool separateCanvas = false)
	{
		CategoryPackingResult result = new CategoryPackingResult
		{
			CategoryName = category,
			StartTime = DateTime.Now
		};
		string categoryPath = Path.Combine(versionPath, category);
		if (!Directory.Exists(categoryPath))
		{
			result.Success = false;
			result.Errors.Add("Category directory not found: " + categoryPath);
			result.EndTime = DateTime.Now;
			return result;
		}
		try
		{
			bool hasCaseMapMetadata = ApplyImageCaseMap(categoryPath);
			ReferenceWzMetadata referenceMetadata = null;
			if (!hasCaseMapMetadata)
			{
				referenceMetadata = TryLoadReferenceWzMetadata(outputPath, category, encryption, patchVersion);
				if (referenceMetadata != null)
				{
					int renamedCount = ApplyImageCaseMapEntries(categoryPath, referenceMetadata.CaseEntries);
					if (renamedCount > 0)
					{
						Debug.WriteLine($"[PackCategory] Applied {renamedCount} case fixes from reference WZ: {referenceMetadata.SourcePath}");
					}
				}
			}
			byte[] wzIv = WzTool.GetIvByMapleVersion(encryption);
			byte[] readIv = WzAESConstant.WZ_BMSCLASSIC;
			if (category.Equals("List", StringComparison.OrdinalIgnoreCase))
			{
				string listJsonPath = Path.Combine(categoryPath, "List.json");
				if (File.Exists(listJsonPath))
				{
					try
					{
						string jsonContent = File.ReadAllText(listJsonPath);
						if (jsonContent.TrimStart().StartsWith("{"))
						{
							ListWzJsonFormat listData = JsonConvert.DeserializeObject<ListWzJsonFormat>(jsonContent);
							if (listData == null)
							{
								result.Success = false;
								result.Errors.Add("List.json could not be parsed");
								result.EndTime = DateTime.Now;
								return result;
							}
							List<string> listEntries = GetListEntriesForPacking(listData);
							if (listEntries == null || listEntries.Count == 0)
							{
								result.Success = true;
								result.ImagesPacked = 0;
								result.OutputFilePath = null;
								result.OutputFileSize = 0L;
								result.EndTime = DateTime.Now;
								Debug.WriteLine("[PackCategory] List: No entries to pack, skipping List.wz creation");
								return result;
							}
							string listWzPath;
							if (saveAs64Bit)
							{
								string categoryOutputPath = Path.Combine(outputPath, "Data", category);
								if (!Directory.Exists(categoryOutputPath))
								{
									Directory.CreateDirectory(categoryOutputPath);
								}
								listWzPath = Path.Combine(categoryOutputPath, "List_000.wz");
							}
							else
							{
								listWzPath = Path.Combine(outputPath, "List.wz");
							}
							ListFileParser.SaveToDisk(listWzPath, encryption, listEntries);
							result.Success = true;
							result.ImagesPacked = 1;
							result.OutputFilePath = listWzPath;
							if (File.Exists(listWzPath))
							{
								result.OutputFileSize = new FileInfo(listWzPath).Length;
							}
							result.EndTime = DateTime.Now;
							Debug.WriteLine($"[PackCategory] List: Created List.wz from JSON format ({result.OutputFileSize} bytes)");
							return result;
						}
					}
					catch (JsonException)
					{
						Debug.WriteLine("[PackCategory] List.img is not in JSON format, attempting normal WZ processing");
					}
				}
			}
			if (saveAs64Bit)
			{
				string categoryOutputPath2 = Path.Combine(outputPath, "Data", category);
				if (!Directory.Exists(categoryOutputPath2))
				{
					Directory.CreateDirectory(categoryOutputPath2);
				}
				List<ImgFileInfo> allImgFiles = CollectImgFiles(categoryPath);
				long totalSize = allImgFiles.Sum((ImgFileInfo f) => f.FileSize);
				Debug.WriteLine($"[PackCategory] {category}: Found {allImgFiles.Count} IMG files, total size: {totalSize / 1024 / 1024}MB");
				List<List<ImgFileInfo>> chunks = GroupImgFilesBySize(allImgFiles, 150000000L);
				int lastWzIndex = chunks.Count - 1;
				Debug.WriteLine($"[PackCategory] {category}: Split into {chunks.Count} WZ file(s)");
				string iniFilePath = Path.Combine(categoryOutputPath2, category + ".ini");
				File.WriteAllText(iniFilePath, $"LastWzIndex|{lastWzIndex}");
				int totalCount = allImgFiles.Count;
				int processedCount = 0;
				long totalOutputSize = 0L;
				await Task.Run(() =>
				{
					for (int i = 0; i < chunks.Count; i++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						List<ImgFileInfo> chunk = chunks[i];
						string text = $"{category}_{i:D3}.wz";
						string text2 = Path.Combine(categoryOutputPath2, text);
						Debug.WriteLine($"[PackCategory] {category}: Creating {text} with {chunk.Count} files");
						using WzFile wzFile = new WzFile(patchVersion, encryption);
						wzFile.Name = text;
						BuildDirectoryStructureForFiles(wzFile.WzDirectory, chunk, wzIv);
						ImgProcessingResult[] processingResults = new ImgProcessingResult[chunk.Count];
						ParallelOptions parallelOptions = new ParallelOptions
						{
							MaxDegreeOfParallelism = GetImgProcessingParallelism(chunk.Count),
							CancellationToken = cancellationToken
						};
						Parallel.For(0, chunk.Count, parallelOptions, (int index) =>
						{
							cancellationToken.ThrowIfCancellationRequested();
							ImgFileInfo imgFileInfo = chunk[index];
							ImgProcessingResult imgProcessingResult2 = ProcessSingleImgFile(imgFileInfo, readIv);
							processingResults[index] = imgProcessingResult2;
							int arg = Interlocked.Increment(ref processedCount);
							progressCallback?.Invoke(arg, totalCount, imgFileInfo.RelativePath);
						});
						int num = 0;
						ImgProcessingResult[] array = processingResults;
						foreach (ImgProcessingResult imgProcessingResult in array)
						{
							if (imgProcessingResult.Success && imgProcessingResult.Image != null)
							{
								imgProcessingResult.ParentDirectory.AddImage(imgProcessingResult.Image);
							}
							else
							{
								num++;
								result.Errors.Add(imgProcessingResult.ErrorMessage ?? ("Failed to process: " + imgProcessingResult.RelativePath));
							}
						}
						if (num > 0)
						{
							throw new InvalidOperationException($"Failed to process {num} IMG files in {text}.");
						}
						if (separateCanvas && i == 0)
						{
							string outputPath2 = Path.Combine(outputPath, "Data");
							ProcessCanvasSeparation(wzFile.WzDirectory, outputPath2, category, encryption, patchVersion, wzIv, result);
						}
						Debug.WriteLine("[PackCategory] " + category + ": Saving " + text);
						wzFile.SaveToDisk(text2, saveAs64Bit, encryption);
						if (File.Exists(text2))
						{
							long length = new FileInfo(text2).Length;
							totalOutputSize += length;
							Debug.WriteLine($"[PackCategory] {category}: Created {text} ({length / 1024 / 1024}MB)");
						}
					}
				}, cancellationToken);
				result.ImagesPacked = processedCount;
				result.OutputFilePath = categoryOutputPath2;
				result.OutputFileSize = totalOutputSize;
			}
			else
			{
				string wzFileName = category + ".wz";
				string wzFilePath = Path.Combine(outputPath, wzFileName);
				await Task.Run(() =>
				{
					using WzFile wzFile = new WzFile(patchVersion, encryption);
					wzFile.Name = wzFileName;
					List<ImgFileInfo> imgFiles = new List<ImgFileInfo>();
					BuildDirectoryStructureAndCollectImgFiles(categoryPath, wzFile.WzDirectory, categoryPath, wzIv, imgFiles, referenceMetadata?.DirectoryOrder, referenceMetadata?.ImageOrder);
					int totalCount2 = imgFiles.Count;
					int processedCount2 = 0;
					Debug.WriteLine($"[PackCategory] {category}: Found {totalCount2} IMG files");
					ImgProcessingResult[] processingResults = new ImgProcessingResult[imgFiles.Count];
					ParallelOptions parallelOptions = new ParallelOptions
					{
						MaxDegreeOfParallelism = GetImgProcessingParallelism(imgFiles.Count),
						CancellationToken = cancellationToken
					};
					Parallel.For(0, imgFiles.Count, parallelOptions, (int index) =>
					{
						cancellationToken.ThrowIfCancellationRequested();
						ImgFileInfo imgFileInfo = imgFiles[index];
						ImgProcessingResult imgProcessingResult2 = ProcessSingleImgFile(imgFileInfo, readIv);
						processingResults[index] = imgProcessingResult2;
						int arg = Interlocked.Increment(ref processedCount2);
						progressCallback?.Invoke(arg, totalCount2, imgFileInfo.RelativePath);
					});
					int num = 0;
					ImgProcessingResult[] array = processingResults;
					foreach (ImgProcessingResult imgProcessingResult in array)
					{
						if (imgProcessingResult.Success && imgProcessingResult.Image != null)
						{
							imgProcessingResult.ParentDirectory.AddImage(imgProcessingResult.Image);
						}
						else
						{
							num++;
							result.Errors.Add(imgProcessingResult.ErrorMessage ?? ("Failed to process: " + imgProcessingResult.RelativePath));
						}
					}
					if (num > 0)
					{
						throw new InvalidOperationException($"Failed to process {num} IMG files in {wzFileName}.");
					}
					Debug.WriteLine("[PackCategory] " + category + ": Saving to " + wzFilePath);
					wzFile.SaveToDisk(wzFilePath, saveAs64Bit, encryption);
					result.ImagesPacked = processedCount2;
					result.OutputFilePath = wzFilePath;
					if (File.Exists(wzFilePath))
					{
						result.OutputFileSize = new FileInfo(wzFilePath).Length;
						Debug.WriteLine($"[PackCategory] {category}: Created {wzFilePath} ({result.OutputFileSize / 1024 / 1024}MB)");
					}
				}, cancellationToken);
			}
			result.Success = true;
		}
		catch (Exception ex2)
		{
			result.Success = false;
			result.Errors.Add(ex2.Message);
			Debug.WriteLine($"Error packing category {category}: {ex2}");
		}
		result.EndTime = DateTime.Now;
		return result;
	}

	public async Task<PackingResult> PackCategoriesAsync(string versionPath, string outputPath, IEnumerable<string> categories, bool saveAs64Bit = false, CancellationToken cancellationToken = default(CancellationToken), IProgress<PackingProgress> progress = null, short overridePatchVersion = 0, bool separateCanvas = false, WzMapleVersion? overrideEncryption = null)
	{
		PackingResult result = new PackingResult
		{
			VersionPath = versionPath,
			OutputPath = outputPath,
			StartTime = DateTime.Now
		};
		PackingProgress progressData = new PackingProgress
		{
			CurrentPhase = "Initializing",
			TotalFiles = 0,
			ProcessedFiles = 0
		};
		try
		{
			VersionInfo versionInfo = (result.VersionInfo = LoadVersionInfo(versionPath));
			ResolvePackingSettings(versionInfo, overridePatchVersion, overrideEncryption, out var encryption, out var patchVersion);
			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}
			foreach (string category in categories)
			{
				string categoryPath = Path.Combine(versionPath, category);
				if (Directory.Exists(categoryPath))
				{
					progressData.TotalFiles += Directory.EnumerateFiles(categoryPath, "*.img", SearchOption.AllDirectories).Count();
				}
			}
			progress?.Report(progressData);
			foreach (string category2 in categories)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progressData.CurrentPhase = "Packing " + category2;
				progress?.Report(progressData);
				OnCategoryStarted(category2);
				CategoryPackingResult categoryResult = await PackCategoryAsync(versionPath, outputPath, category2, encryption, patchVersion, saveAs64Bit, cancellationToken, (int current, int total, string fileName) =>
				{
					progressData.CurrentFile = fileName;
					progressData.ProcessedFiles++;
					progress?.Report(progressData);
				}, separateCanvas);
				result.CategoriesPacked.Add(category2, categoryResult);
				OnCategoryCompleted(category2, categoryResult);
			}
			result.Success = true;
			result.EndTime = DateTime.Now;
		}
		catch (OperationCanceledException)
		{
			result.Success = false;
			result.ErrorMessage = "Packing was cancelled";
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			result.Success = false;
			result.ErrorMessage = ex3.Message;
			OnErrorOccurred(ex3);
		}
		return result;
	}

	public async Task<PackingResult> PackBetaDataWzAsync(string versionPath, string outputPath, IEnumerable<string> categories, CancellationToken cancellationToken = default(CancellationToken), IProgress<PackingProgress> progress = null, short overridePatchVersion = 0, WzMapleVersion? overrideEncryption = null)
	{
		PackingResult result = new PackingResult
		{
			VersionPath = versionPath,
			OutputPath = outputPath,
			StartTime = DateTime.Now
		};
		PackingProgress progressData = new PackingProgress
		{
			CurrentPhase = "Initializing",
			TotalFiles = 0,
			ProcessedFiles = 0
		};
		try
		{
			VersionInfo versionInfo = LoadVersionInfo(versionPath);
			result.VersionInfo = versionInfo;
			ResolvePackingSettings(versionInfo, overridePatchVersion, overrideEncryption, out var encryption, out var patchVersion);
			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}
			byte[] wzIv = WzTool.GetIvByMapleVersion(encryption);
			byte[] readIv = WzAESConstant.WZ_BMSCLASSIC;
			List<string> categoriesList = categories.ToList();
			bool hasListCategory = categoriesList.RemoveAll((string c) => c.Equals("List", StringComparison.OrdinalIgnoreCase)) > 0;
			using (List<string>.Enumerator enumerator = categoriesList.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string categoryPath = Path.Combine(path2: enumerator.Current, path1: versionPath);
					if (Directory.Exists(categoryPath))
					{
						progressData.TotalFiles += Directory.EnumerateFiles(categoryPath, "*.img", SearchOption.AllDirectories).Count();
					}
				}
			}
			progress?.Report(progressData);
			string dataWzPath = Path.Combine(outputPath, "Data.wz");
			progressData.CurrentPhase = "Packing Data.wz";
			progress?.Report(progressData);
			int totalProcessed = 0;
			long totalOutputSize = 0L;
			await Task.Run(() =>
			{
				using WzFile wzFile = new WzFile(patchVersion, encryption);
				wzFile.Name = "Data.wz";
				foreach (string item in categoriesList)
				{
					cancellationToken.ThrowIfCancellationRequested();
					string text = Path.Combine(versionPath, item);
					if (Directory.Exists(text))
					{
						progressData.CurrentPhase = "Packing " + item;
						progress?.Report(progressData);
						OnCategoryStarted(item);
						CategoryPackingResult categoryPackingResult = new CategoryPackingResult
						{
							CategoryName = item,
							StartTime = DateTime.Now
						};
						WzDirectory parentDir;
						if (item.Equals("_Root", StringComparison.OrdinalIgnoreCase))
						{
							parentDir = wzFile.WzDirectory;
						}
						else
						{
							WzDirectory wzDirectory = new WzDirectory(item)
							{
								WzIv = wzIv
							};
							wzFile.WzDirectory.AddDirectory(wzDirectory);
							parentDir = wzDirectory;
						}
						List<ImgFileInfo> imgFiles = new List<ImgFileInfo>();
						BuildDirectoryStructureAndCollectImgFiles(text, parentDir, text, wzIv, imgFiles);
						int count = imgFiles.Count;
						Debug.WriteLine($"[PackBetaDataWz] {item}: Found {count} IMG files");
						ImgProcessingResult[] processingResults = new ImgProcessingResult[imgFiles.Count];
						ParallelOptions parallelOptions = new ParallelOptions
						{
							MaxDegreeOfParallelism = GetImgProcessingParallelism(imgFiles.Count),
							CancellationToken = cancellationToken
						};
						Parallel.For(0, imgFiles.Count, parallelOptions, (int index) =>
						{
							cancellationToken.ThrowIfCancellationRequested();
							ImgFileInfo imgFileInfo = imgFiles[index];
							ImgProcessingResult imgProcessingResult2 = ProcessSingleImgFile(imgFileInfo, readIv);
							processingResults[index] = imgProcessingResult2;
							int processedFiles = Interlocked.Increment(ref totalProcessed);
							progressData.ProcessedFiles = processedFiles;
							progressData.CurrentFile = imgFileInfo.RelativePath;
							progress?.Report(progressData);
						});
						int num = 0;
						int num2 = 0;
						ImgProcessingResult[] array = processingResults;
						foreach (ImgProcessingResult imgProcessingResult in array)
						{
							if (imgProcessingResult.Success && imgProcessingResult.Image != null)
							{
								imgProcessingResult.ParentDirectory.AddImage(imgProcessingResult.Image);
								num++;
							}
							else
							{
								num2++;
								categoryPackingResult.Errors.Add(imgProcessingResult.ErrorMessage ?? ("Failed to process: " + imgProcessingResult.RelativePath));
							}
						}
						if (num2 > 0)
						{
							throw new InvalidOperationException($"Failed to process {num2} IMG files in beta category {item}.");
						}
						categoryPackingResult.ImagesPacked = num;
						categoryPackingResult.Success = true;
						categoryPackingResult.EndTime = DateTime.Now;
						result.CategoriesPacked.Add(item, categoryPackingResult);
						OnCategoryCompleted(item, categoryPackingResult);
					}
				}
				Debug.WriteLine("[PackBetaDataWz] Saving Data.wz to " + dataWzPath);
				wzFile.SaveToDisk(dataWzPath, false, encryption);
				if (File.Exists(dataWzPath))
				{
					totalOutputSize = new FileInfo(dataWzPath).Length;
					Debug.WriteLine($"[PackBetaDataWz] Created Data.wz ({totalOutputSize / 1024 / 1024}MB)");
				}
			}, cancellationToken);
			if (hasListCategory)
			{
				progressData.CurrentPhase = "Packing List.wz";
				progress?.Report(progressData);
				CategoryPackingResult listResult = PackListCategorySeparately(versionPath, outputPath, encryption, patchVersion);
				if (listResult != null)
				{
					result.CategoriesPacked.Add("List", listResult);
					totalOutputSize += listResult.OutputFileSize;
				}
			}
			result.Success = true;
			result.EndTime = DateTime.Now;
			progressData.CurrentPhase = "Complete";
			progress?.Report(progressData);
		}
		catch (OperationCanceledException)
		{
			result.Success = false;
			result.ErrorMessage = "Packing was cancelled";
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			result.Success = false;
			result.ErrorMessage = ex3.Message;
			OnErrorOccurred(ex3);
		}
		return result;
	}

	private CategoryPackingResult PackListCategorySeparately(string versionPath, string outputPath, WzMapleVersion encryption, short patchVersion)
	{
		CategoryPackingResult categoryPackingResult = new CategoryPackingResult
		{
			CategoryName = "List",
			StartTime = DateTime.Now
		};
		try
		{
			string path = Path.Combine(versionPath, "List");
			string path2 = Path.Combine(path, "List.json");
			if (!File.Exists(path2))
			{
				Debug.WriteLine("[PackListCategorySeparately] List.json not found, skipping List.wz");
				categoryPackingResult.Success = false;
				categoryPackingResult.Errors.Add("List.json not found");
				categoryPackingResult.EndTime = DateTime.Now;
				return categoryPackingResult;
			}
			string text = File.ReadAllText(path2);
			if (!text.TrimStart().StartsWith("{"))
			{
				categoryPackingResult.Success = false;
				categoryPackingResult.Errors.Add("List.json is not in expected JSON format");
				categoryPackingResult.EndTime = DateTime.Now;
				return categoryPackingResult;
			}
			ListWzJsonFormat listWzJsonFormat = JsonConvert.DeserializeObject<ListWzJsonFormat>(text);
			List<string> listEntriesForPacking = GetListEntriesForPacking(listWzJsonFormat);
			if (listWzJsonFormat == null)
			{
				categoryPackingResult.Success = false;
				categoryPackingResult.Errors.Add("List.json could not be parsed");
				categoryPackingResult.EndTime = DateTime.Now;
				return categoryPackingResult;
			}
			if (listEntriesForPacking == null || listEntriesForPacking.Count == 0)
			{
				categoryPackingResult.Success = true;
				categoryPackingResult.ImagesPacked = 0;
				categoryPackingResult.OutputFilePath = null;
				categoryPackingResult.OutputFileSize = 0L;
				categoryPackingResult.EndTime = DateTime.Now;
				return categoryPackingResult;
			}
			string text2 = Path.Combine(outputPath, "List.wz");
			ListFileParser.SaveToDisk(text2, encryption, listEntriesForPacking);
			categoryPackingResult.Success = true;
			categoryPackingResult.ImagesPacked = 1;
			categoryPackingResult.OutputFilePath = text2;
			if (File.Exists(text2))
			{
				categoryPackingResult.OutputFileSize = new FileInfo(text2).Length;
			}
			Debug.WriteLine($"[PackListCategorySeparately] Created List.wz ({categoryPackingResult.OutputFileSize} bytes)");
		}
		catch (Exception ex)
		{
			categoryPackingResult.Success = false;
			categoryPackingResult.Errors.Add("Error packing List.wz: " + ex.Message);
			Debug.WriteLine($"[PackListCategorySeparately] Error: {ex}");
		}
		categoryPackingResult.EndTime = DateTime.Now;
		return categoryPackingResult;
	}

	private VersionInfo LoadVersionInfo(string versionPath)
	{
		string path = Path.Combine(versionPath, "manifest.json");
		if (!File.Exists(path))
		{
			return null;
		}
		try
		{
			string value = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<VersionInfo>(value);
		}
		catch
		{
			return null;
		}
	}

	private static void ResolvePackingSettings(VersionInfo versionInfo, short overridePatchVersion, WzMapleVersion? overrideEncryption, out WzMapleVersion encryption, out short patchVersion)
	{
		encryption = WzMapleVersion.BMS;
		patchVersion = 1;
		if (versionInfo != null)
		{
			if (!string.IsNullOrEmpty(versionInfo.Encryption) && Enum.TryParse<WzMapleVersion>(versionInfo.Encryption, ignoreCase: true, out var result))
			{
				encryption = result;
			}
			short patchVersion2;
			if (versionInfo.PatchVersion > 0 && versionInfo.PatchVersion <= 32767)
			{
				patchVersion = (short)versionInfo.PatchVersion;
			}
			else if (TryExtractPatchVersion(versionInfo.Version, out patchVersion2))
			{
				patchVersion = patchVersion2;
			}
			else if (TryExtractPatchVersion(versionInfo.DisplayName, out patchVersion2))
			{
				patchVersion = patchVersion2;
			}
		}
		if (overridePatchVersion > 0)
		{
			patchVersion = overridePatchVersion;
		}
		if (overrideEncryption.HasValue)
		{
			encryption = overrideEncryption.Value;
		}
		if (patchVersion <= 0)
		{
			patchVersion = 1;
		}
	}

	private static bool TryExtractPatchVersion(string input, out short patchVersion)
	{
		patchVersion = 0;
		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}
		Match match = Regex.Match(input, "v(\\d+)", RegexOptions.IgnoreCase);
		if (match.Success && short.TryParse(match.Groups[1].Value, out patchVersion) && patchVersion > 0)
		{
			return true;
		}
		match = Regex.Match(input, "(\\d+)");
		return match.Success && short.TryParse(match.Groups[1].Value, out patchVersion) && patchVersion > 0;
	}

	private static List<string> GetListEntriesForPacking(ListWzJsonFormat listData)
	{
		if (listData == null)
		{
			return null;
		}
		if (listData.Entries != null && listData.Entries.Count > 0)
		{
			return listData.Entries;
		}
		if (listData.RemainingEntries != null && listData.RemainingEntries.Count > 0)
		{
			return listData.RemainingEntries;
		}
		return new List<string>();
	}

	private bool ApplyImageCaseMap(string categoryPath)
	{
		string path = Path.Combine(categoryPath, ".imgcase.json");
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			string value = File.ReadAllText(path);
			ImageCaseMapData imageCaseMapData = JsonConvert.DeserializeObject<ImageCaseMapData>(value);
			if (imageCaseMapData?.Entries == null || imageCaseMapData.Entries.Count == 0)
			{
				return false;
			}
			int num = ApplyImageCaseMapEntries(categoryPath, imageCaseMapData.Entries);
			if (num > 0)
			{
				Debug.WriteLine($"[PackCategory] Applied {num} case fixes from {".imgcase.json"} in {categoryPath}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[PackCategory] Failed to apply .imgcase.json: " + ex.Message);
			return false;
		}
	}

	private ReferenceWzMetadata TryLoadReferenceWzMetadata(string outputPath, string category, WzMapleVersion encryption, short patchVersion)
	{
		foreach (string item in EnumerateReferenceWzCandidates(outputPath, category))
		{
			if (!File.Exists(item))
			{
				continue;
			}
			try
			{
				using WzFile wzFile = ((patchVersion > 0) ? new WzFile(item, patchVersion, encryption) : new WzFile(item, encryption));
				WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
				if (wzFileParseStatus == WzFileParseStatus.Success && wzFile.WzDirectory != null)
				{
					ReferenceWzMetadata referenceWzMetadata = new ReferenceWzMetadata
					{
						SourcePath = item,
						CaseEntries = new Dictionary<string, string>(StringComparer.Ordinal),
						DirectoryOrder = new Dictionary<string, int>(StringComparer.Ordinal),
						ImageOrder = new Dictionary<string, int>(StringComparer.Ordinal)
					};
					int dirIndex = 0;
					int imageIndex = 0;
					CollectReferenceMetadata(wzFile.WzDirectory, string.Empty, referenceWzMetadata.CaseEntries, referenceWzMetadata.DirectoryOrder, referenceWzMetadata.ImageOrder, ref dirIndex, ref imageIndex);
					if (referenceWzMetadata.CaseEntries.Count > 0 || referenceWzMetadata.ImageOrder.Count > 0)
					{
						return referenceWzMetadata;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("[PackCategory] Failed to load reference metadata from " + item + ": " + ex.Message);
			}
		}
		return null;
	}

	private static IEnumerable<string> EnumerateReferenceWzCandidates(string outputPath, string category)
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = new string[4]
		{
			Path.Combine(outputPath, category + ".wz"),
			Path.Combine(outputPath, "Data", category, category + "_000.wz"),
			Path.Combine(Directory.GetParent(outputPath)?.FullName ?? string.Empty, category + ".wz"),
			Path.Combine(Directory.GetParent(outputPath)?.FullName ?? string.Empty, "Data", category, category + "_000.wz")
		};
		foreach (string candidate in array)
		{
			if (!string.IsNullOrWhiteSpace(candidate) && seen.Add(candidate))
			{
				yield return candidate;
			}
		}
	}

	private static void CollectReferenceMetadata(WzDirectory directory, string relativeDirPath, Dictionary<string, string> caseMap, Dictionary<string, int> directoryOrder, Dictionary<string, int> imageOrder, ref int dirIndex, ref int imageIndex)
	{
		foreach (WzImage wzImage in directory.WzImages)
		{
			string text = (string.IsNullOrEmpty(relativeDirPath) ? wzImage.Name : (relativeDirPath + "/" + wzImage.Name));
			string key = text.ToLowerInvariant();
			if (!caseMap.ContainsKey(key))
			{
				caseMap[key] = text;
			}
			if (!imageOrder.ContainsKey(key))
			{
				imageOrder[key] = imageIndex++;
			}
		}
		foreach (WzDirectory wzDirectory in directory.WzDirectories)
		{
			string text2 = (string.IsNullOrEmpty(relativeDirPath) ? wzDirectory.Name : (relativeDirPath + "/" + wzDirectory.Name));
			string key2 = text2.ToLowerInvariant();
			if (!directoryOrder.ContainsKey(key2))
			{
				directoryOrder[key2] = dirIndex++;
			}
			CollectReferenceMetadata(wzDirectory, text2, caseMap, directoryOrder, imageOrder, ref dirIndex, ref imageIndex);
		}
	}

	private static int ApplyImageCaseMapEntries(string categoryPath, IDictionary<string, string> entries)
	{
		int num = 0;
		List<string> list = Directory.EnumerateFiles(categoryPath, "*.img", SearchOption.AllDirectories).ToList();
		foreach (string item in list)
		{
			if (!File.Exists(item))
			{
				continue;
			}
			string text = Path.GetRelativePath(categoryPath, item).Replace(Path.DirectorySeparatorChar, '/');
			string key = text.ToLowerInvariant();
			if (entries.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) && !string.Equals(text, value, StringComparison.Ordinal))
			{
				string targetPath = Path.Combine(categoryPath, value.Replace('/', Path.DirectorySeparatorChar));
				if (MoveFileUsingTemp(item, targetPath))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static int GetImgProcessingParallelism(int itemCount)
	{
		if (itemCount <= 0)
		{
			return 1;
		}
		return Math.Max(1, Math.Min(MAX_DEGREE_OF_PARALLELISM, Math.Min(2, itemCount)));
	}

	private static bool MoveFileUsingTemp(string sourcePath, string targetPath)
	{
		try
		{
			if (string.Equals(sourcePath, targetPath, StringComparison.Ordinal))
			{
				return true;
			}
			string directoryName = Path.GetDirectoryName(targetPath);
			if (string.IsNullOrEmpty(directoryName))
			{
				return false;
			}
			Directory.CreateDirectory(directoryName);
			if (File.Exists(targetPath) && !string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string path = Path.GetDirectoryName(sourcePath) ?? directoryName;
			string text = Path.Combine(path, $"__casefix_{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
			File.Move(sourcePath, text);
			if (File.Exists(targetPath))
			{
				File.Delete(targetPath);
			}
			File.Move(text, targetPath);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private int CountTotalImages(string versionPath)
	{
		int num = 0;
		foreach (string item in Directory.EnumerateDirectories(versionPath))
		{
			string fileName = Path.GetFileName(item);
			if (!fileName.StartsWith(".") && !fileName.Equals("manifest", StringComparison.OrdinalIgnoreCase))
			{
				num += Directory.EnumerateFiles(item, "*.img", SearchOption.AllDirectories).Count();
				if (fileName.Equals("List", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(item, "List.json")))
				{
					num++;
				}
			}
		}
		return num;
	}

	private List<string> GetCategoriesToPack(string versionPath)
	{
		List<string> list = new List<string>();
		string[] sTANDARD_CATEGORIES = STANDARD_CATEGORIES;
		foreach (string text in sTANDARD_CATEGORIES)
		{
			string text2 = Path.Combine(versionPath, text);
			if (Directory.Exists(text2))
			{
				bool flag = Directory.EnumerateFiles(text2, "*.img", SearchOption.AllDirectories).Any();
				bool flag2 = text.Equals("List", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(text2, "List.json"));
				if (flag | flag2)
				{
					list.Add(text);
				}
			}
		}
		foreach (string item in Directory.EnumerateDirectories(versionPath))
		{
			string fileName = Path.GetFileName(item);
			if (!list.Contains(fileName, StringComparer.OrdinalIgnoreCase) && !fileName.StartsWith(".") && !fileName.Equals("manifest", StringComparison.OrdinalIgnoreCase))
			{
				int num = Directory.EnumerateFiles(item, "*.img", SearchOption.AllDirectories).Count();
				bool flag3 = fileName.Equals("List", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(item, "List.json"));
				int num2 = Directory.EnumerateDirectories(item, "*", SearchOption.AllDirectories).Count();
				Debug.WriteLine($"[GetCategoriesToPack] Checking '{fileName}': {num} .img files, {num2} subdirs");
				if ((num > 0 || num2 > 0) | flag3)
				{
					list.Add(fileName);
					Debug.WriteLine("[GetCategoriesToPack] Added category: " + fileName);
				}
				else
				{
					Debug.WriteLine("[GetCategoriesToPack] Skipping '" + fileName + "' - no .img files or subdirs");
				}
			}
		}
		Debug.WriteLine("[GetCategoriesToPack] Total categories to pack: " + string.Join(", ", list));
		return list;
	}

	[Obsolete("Use BuildDirectoryStructureAndCollectImgFiles with Parallel.ForEach for concurrent processing")]
	private void LoadImagesRecursively(string currentPath, WzDirectory parentDir, string basePath, WzImgDeserializer deserializer, byte[] wzIv, ref int processedCount, int totalCount, Action<int, int, string> progressCallback, CategoryPackingResult result, CancellationToken cancellationToken)
	{
		foreach (string item in Directory.EnumerateDirectories(currentPath))
		{
			cancellationToken.ThrowIfCancellationRequested();
			string fileName = Path.GetFileName(item);
			if (!fileName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
			{
				WzDirectory wzDirectory = new WzDirectory(fileName)
				{
					WzIv = wzIv
				};
				parentDir.AddDirectory(wzDirectory);
				LoadImagesRecursively(item, wzDirectory, basePath, deserializer, wzIv, ref processedCount, totalCount, progressCallback, result, cancellationToken);
			}
		}
		foreach (string item2 in Directory.EnumerateFiles(currentPath, "*.img"))
		{
			cancellationToken.ThrowIfCancellationRequested();
			string fileName2 = Path.GetFileName(item2);
			string text = item2.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
			try
			{
				byte[] array = File.ReadAllBytes(item2);
				using MemoryStream input = new MemoryStream(array);
				WzBinaryReader reader = new WzBinaryReader(input, wzIv, 0L);
				WzImage wzImage = new WzImage(fileName2, reader)
				{
					BlockSize = array.Length
				};
				wzImage.CalculateAndSetImageChecksum(array);
				wzImage.Offset = 0L;
				wzImage.ParseEverything = true;
				if (wzImage.ParseImage(forceReadFromData: true))
				{
					wzImage.Changed = true;
					WzImage wzImage2 = CloneWzImage(wzImage, fileName2);
					if (wzImage2 != null)
					{
						parentDir.AddImage(wzImage2);
						processedCount++;
						progressCallback?.Invoke(processedCount, totalCount, text);
					}
					else
					{
						result.Errors.Add("Failed to clone: " + text);
					}
				}
				else
				{
					result.Errors.Add("Failed to parse: " + text);
				}
			}
			catch (Exception ex)
			{
				result.Errors.Add("Error loading " + text + ": " + ex.Message);
				Debug.WriteLine($"Error loading IMG file {item2}: {ex}");
			}
		}
	}

	private WzImage CloneWzImage(WzImage original, string name)
	{
		try
		{
			WzImage wzImage = new WzImage(name)
			{
				Changed = true
			};
			foreach (WzImageProperty wzProperty in original.WzProperties)
			{
				WzImageProperty item = wzProperty.DeepClone();
				wzImage.WzProperties.Add(item);
			}
			return wzImage;
		}
		catch (Exception value)
		{
			Debug.WriteLine($"Error cloning WzImage {name}: {value}");
			return null;
		}
	}

	private List<ImgFileInfo> CollectImgFiles(string categoryPath)
	{
		List<ImgFileInfo> list = new List<ImgFileInfo>();
		foreach (string item in Directory.EnumerateFiles(categoryPath, "*.img", SearchOption.AllDirectories))
		{
			string relativePath = item.Substring(categoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
			FileInfo fileInfo = new FileInfo(item);
			list.Add(new ImgFileInfo
			{
				FilePath = item,
				RelativePath = relativePath,
				FileSize = fileInfo.Length
			});
		}
		return list;
	}

	private List<List<ImgFileInfo>> GroupImgFilesBySize(List<ImgFileInfo> imgFiles, long maxSize)
	{
		List<List<ImgFileInfo>> list = new List<List<ImgFileInfo>>();
		List<ImgFileInfo> list2 = new List<ImgFileInfo>();
		long num = 0L;
		List<ImgFileInfo> list3 = imgFiles.OrderBy((ImgFileInfo f) => f.RelativePath).ToList();
		foreach (ImgFileInfo item in list3)
		{
			if (num + item.FileSize > maxSize && list2.Count > 0)
			{
				list.Add(list2);
				list2 = new List<ImgFileInfo>();
				num = 0L;
			}
			list2.Add(item);
			num += item.FileSize;
		}
		if (list2.Count > 0)
		{
			list.Add(list2);
		}
		return list;
	}

	private void BuildDirectoryStructureForFiles(WzDirectory rootDir, List<ImgFileInfo> imgFiles, byte[] wzIv)
	{
		foreach (ImgFileInfo imgFile in imgFiles)
		{
			string directoryName = Path.GetDirectoryName(imgFile.RelativePath);
			WzDirectory wzDirectory = rootDir;
			if (!string.IsNullOrEmpty(directoryName))
			{
				InlineArray2<char> buffer = default;
				buffer[0] = Path.DirectorySeparatorChar;
				buffer[1] = Path.AltDirectorySeparatorChar;
				string[] array = directoryName.Split(buffer);
				string[] array2 = array;
				foreach (string part in array2)
				{
					if (!string.IsNullOrEmpty(part))
					{
						WzDirectory wzDirectory2 = wzDirectory.WzDirectories.FirstOrDefault((WzDirectory d) => d.Name == part);
						if (wzDirectory2 != null)
						{
							wzDirectory = wzDirectory2;
							continue;
						}
						WzDirectory wzDirectory3 = new WzDirectory(part)
						{
							WzIv = wzIv
						};
						wzDirectory.AddDirectory(wzDirectory3);
						wzDirectory = wzDirectory3;
					}
				}
			}
			imgFile.ParentDirectory = wzDirectory;
		}
	}

	private void BuildDirectoryStructureAndCollectImgFiles(string currentPath, WzDirectory parentDir, string basePath, byte[] wzIv, List<ImgFileInfo> imgFiles, Dictionary<string, int> referenceDirectoryOrder = null, Dictionary<string, int> referenceImageOrder = null)
	{
		foreach (string item in (from path in Directory.EnumerateDirectories(currentPath)
			orderby ResolveReferenceOrder(path, referenceDirectoryOrder)
			select path).ThenBy((string path) => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
		{
			string fileName = Path.GetFileName(item);
			if (!fileName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
			{
				WzDirectory wzDirectory = new WzDirectory(fileName)
				{
					WzIv = wzIv
				};
				parentDir.AddDirectory(wzDirectory);
				BuildDirectoryStructureAndCollectImgFiles(item, wzDirectory, basePath, wzIv, imgFiles, referenceDirectoryOrder, referenceImageOrder);
			}
		}
		foreach (string item2 in (from path in Directory.EnumerateFiles(currentPath, "*.img")
			orderby ResolveReferenceOrder(path, referenceImageOrder)
			select path).ThenBy((string path) => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
		{
			string relativePath = item2.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
			FileInfo fileInfo = new FileInfo(item2);
			imgFiles.Add(new ImgFileInfo
			{
				FilePath = item2,
				RelativePath = relativePath,
				ParentDirectory = parentDir,
				FileSize = fileInfo.Length
			});
		}
		int ResolveReferenceOrder(string path, Dictionary<string, int> orderMap)
		{
			if (orderMap == null)
			{
				return int.MaxValue;
			}
			string key = Path.GetRelativePath(basePath, path).Replace(Path.DirectorySeparatorChar, '/').ToLowerInvariant();
			int value;
			return orderMap.TryGetValue(key, out value) ? value : int.MaxValue;
		}
	}

	private ImgProcessingResult ProcessSingleImgFile(ImgFileInfo imgFileInfo, byte[] readIv)
	{
		ImgProcessingResult imgProcessingResult = new ImgProcessingResult
		{
			RelativePath = imgFileInfo.RelativePath,
			ParentDirectory = imgFileInfo.ParentDirectory
		};
		try
		{
			string fileName = Path.GetFileName(imgFileInfo.FilePath);
			byte[] array = File.ReadAllBytes(imgFileInfo.FilePath);
			using MemoryStream input = new MemoryStream(array);
			WzBinaryReader reader = new WzBinaryReader(input, readIv, 0L);
			WzImage wzImage = new WzImage(fileName, reader)
			{
				BlockSize = array.Length
			};
			wzImage.CalculateAndSetImageChecksum(array);
			wzImage.Offset = 0L;
			wzImage.ParseEverything = true;
			if (wzImage.ParseImage(forceReadFromData: true))
			{
				wzImage.Changed = true;
				WzImage wzImage2 = CloneWzImage(wzImage, fileName);
				if (wzImage2 != null)
				{
					imgProcessingResult.Success = true;
					imgProcessingResult.Image = wzImage2;
				}
				else
				{
					imgProcessingResult.Success = false;
					imgProcessingResult.ErrorMessage = "Failed to clone: " + imgFileInfo.RelativePath;
				}
			}
			else
			{
				imgProcessingResult.Success = false;
				imgProcessingResult.ErrorMessage = "Failed to parse: " + imgFileInfo.RelativePath;
			}
		}
		catch (Exception ex)
		{
			imgProcessingResult.Success = false;
			imgProcessingResult.ErrorMessage = "Error loading " + imgFileInfo.RelativePath + ": " + ex.Message;
			Debug.WriteLine($"Error loading IMG file {imgFileInfo.FilePath}: {ex}");
		}
		return imgProcessingResult;
	}

	private void ProcessCanvasSeparation(WzDirectory directory, string outputPath, string category, WzMapleVersion encryption, short patchVersion, byte[] wzIv, CategoryPackingResult result)
	{
		try
		{
			List<CanvasInfo> list = new List<CanvasInfo>();
			FindLargeCanvasProperties(directory, category, "", list);
			if (list.Count == 0)
			{
				Debug.WriteLine("[Canvas] No canvases to separate for category " + category);
				return;
			}
			Debug.WriteLine($"[Canvas] Found {list.Count} canvases to separate for category {category}");
			string text = Path.Combine(outputPath, category, "_Canvas");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			List<List<CanvasInfo>> list2 = GroupCanvasesIntoFiles(list);
			int num = 0;
			foreach (List<CanvasInfo> item in list2)
			{
				CreateCanvasWzFile(text, category, num, item, encryption, patchVersion, wzIv);
				num++;
			}
			UpdateCanvasIndexFile(text, num - 1);
			foreach (CanvasInfo item2 in list)
			{
				ReplaceCanvasWithOutlink(item2);
			}
			Debug.WriteLine($"[Canvas] Created {num} Canvas WZ files for category {category}");
		}
		catch (Exception ex)
		{
			result.Errors.Add("Error during canvas separation: " + ex.Message);
			Debug.WriteLine($"[Canvas] Error in ProcessCanvasSeparation: {ex}");
		}
	}

	private void FindLargeCanvasProperties(WzObject node, string category, string currentPath, List<CanvasInfo> canvases)
	{
		if (!(node is WzDirectory wzDirectory))
		{
			return;
		}
		foreach (WzDirectory wzDirectory2 in wzDirectory.WzDirectories)
		{
			string currentPath2 = (string.IsNullOrEmpty(currentPath) ? wzDirectory2.Name : (currentPath + "/" + wzDirectory2.Name));
			FindLargeCanvasProperties(wzDirectory2, category, currentPath2, canvases);
		}
		foreach (WzImage wzImage in wzDirectory.WzImages)
		{
			string imagePath = (string.IsNullOrEmpty(currentPath) ? wzImage.Name : (currentPath + "/" + wzImage.Name));
			FindLargeCanvasPropertiesInImage(wzImage, category, imagePath, canvases);
		}
	}

	private void FindLargeCanvasPropertiesInImage(WzImage image, string category, string imagePath, List<CanvasInfo> canvases)
	{
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			FindLargeCanvasPropertiesRecursive(wzProperty, image, category, imagePath, wzProperty.Name, canvases);
		}
	}

	private void FindLargeCanvasPropertiesRecursive(WzImageProperty prop, WzImage sourceImage, string category, string imagePath, string propertyPath, List<CanvasInfo> canvases)
	{
		if (prop is WzCanvasProperty canvas)
		{
			long num = EstimateCanvasSize(canvas);
			if (num >= 100000)
			{
				string outlinkPath = $"{category}/{"_Canvas"}/{imagePath}/{propertyPath}";
				canvases.Add(new CanvasInfo
				{
					Canvas = canvas,
					SourceImage = sourceImage,
					PropertyPath = propertyPath,
					EstimatedSize = num,
					OutlinkPath = outlinkPath
				});
			}
		}
		if (prop is IPropertyContainer propertyContainer)
		{
			{
				foreach (WzImageProperty wzProperty in propertyContainer.WzProperties)
				{
					string propertyPath2 = propertyPath + "/" + wzProperty.Name;
					FindLargeCanvasPropertiesRecursive(wzProperty, sourceImage, category, imagePath, propertyPath2, canvases);
				}
				return;
			}
		}
		if (!(prop is WzConvexProperty wzConvexProperty))
		{
			return;
		}
		foreach (WzImageProperty wzProperty2 in wzConvexProperty.WzProperties)
		{
			string propertyPath3 = propertyPath + "/" + wzProperty2.Name;
			FindLargeCanvasPropertiesRecursive(wzProperty2, sourceImage, category, imagePath, propertyPath3, canvases);
		}
	}

	private long EstimateCanvasSize(WzCanvasProperty canvas)
	{
		if (canvas.PngProperty == null)
		{
			return 0L;
		}
		int width = canvas.PngProperty.Width;
		int height = canvas.PngProperty.Height;
		long num = width * height * 4;
		if (canvas.WzProperties != null)
		{
			num += canvas.WzProperties.Count * 50;
		}
		return num;
	}

	private List<List<CanvasInfo>> GroupCanvasesIntoFiles(List<CanvasInfo> canvases)
	{
		List<List<CanvasInfo>> list = new List<List<CanvasInfo>>();
		List<CanvasInfo> list2 = new List<CanvasInfo>();
		long num = 0L;
		List<CanvasInfo> list3 = canvases.OrderByDescending((CanvasInfo c) => c.EstimatedSize).ToList();
		foreach (CanvasInfo item in list3)
		{
			if ((num + item.EstimatedSize > 500000000 || list2.Count >= 1000) && list2.Count > 0)
			{
				list.Add(list2);
				list2 = new List<CanvasInfo>();
				num = 0L;
			}
			list2.Add(item);
			num += item.EstimatedSize;
		}
		if (list2.Count > 0)
		{
			list.Add(list2);
		}
		return list;
	}

	private void CreateCanvasWzFile(string canvasDir, string category, int canvasIndex, List<CanvasInfo> canvases, WzMapleVersion encryption, short patchVersion, byte[] wzIv)
	{
		string text = $"{"_Canvas"}_{canvasIndex:D3}.wz";
		string text2 = Path.Combine(canvasDir, text);
		using (WzFile wzFile = new WzFile(patchVersion, encryption))
		{
			wzFile.Name = text;
			IEnumerable<IGrouping<string, CanvasInfo>> enumerable = from c in canvases
				group c by c.SourceImage.Name;
			foreach (IGrouping<string, CanvasInfo> item in enumerable)
			{
				WzImage wzImage = new WzImage(item.Key)
				{
					Changed = true
				};
				foreach (CanvasInfo item2 in item)
				{
					WzCanvasProperty wzCanvasProperty = (WzCanvasProperty)item2.Canvas.DeepClone();
					string[] array = item2.PropertyPath.Split('/');
					WzObject wzObject = wzImage;
					for (int num = 0; num < array.Length - 1; num++)
					{
						string name = array[num];
						WzImageProperty wzImageProperty = null;
						if (wzObject is WzImage wzImage2)
						{
							wzImageProperty = wzImage2[name];
							if (wzImageProperty == null)
							{
								WzSubProperty wzSubProperty = new WzSubProperty(name);
								wzImage2.WzProperties.Add(wzSubProperty);
								wzObject = wzSubProperty;
							}
							else
							{
								wzObject = wzImageProperty;
							}
						}
						else if (wzObject is WzSubProperty wzSubProperty2)
						{
							wzImageProperty = wzSubProperty2[name];
							if (wzImageProperty == null)
							{
								WzSubProperty wzSubProperty3 = new WzSubProperty(name);
								wzSubProperty2.AddProperty(wzSubProperty3);
								wzObject = wzSubProperty3;
							}
							else
							{
								wzObject = wzImageProperty;
							}
						}
					}
					wzCanvasProperty.Name = array[^1];
					if (wzObject is WzImage wzImage3)
					{
						wzImage3.WzProperties.Add(wzCanvasProperty);
					}
					else if (wzObject is WzSubProperty wzSubProperty4)
					{
						wzSubProperty4.AddProperty(wzCanvasProperty);
					}
				}
				wzFile.WzDirectory.AddImage(wzImage);
			}
			wzFile.SaveToDisk(text2, true, encryption);
		}
		Debug.WriteLine($"[Canvas] Created {text2} with {canvases.Count} canvases");
	}

	private void ReplaceCanvasWithOutlink(CanvasInfo canvasInfo)
	{
		WzCanvasProperty canvas = canvasInfo.Canvas;
		WzObject parent = canvas.Parent;
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(canvas.Name);
		wzCanvasProperty.AddProperty(new WzStringProperty("_outlink", canvasInfo.OutlinkPath));
		Bitmap bitmap = new Bitmap(1, 1);
		bitmap.SetPixel(0, 0, Color.Transparent);
		wzCanvasProperty.PngProperty = new WzPngProperty();
		wzCanvasProperty.PngProperty.PNG = bitmap;
		foreach (WzImageProperty wzProperty in canvas.WzProperties)
		{
			if (wzProperty.Name != "_outlink" && wzProperty.Name != "_inlink")
			{
				wzCanvasProperty.AddProperty(wzProperty.DeepClone());
			}
		}
		if (parent is WzImage wzImage)
		{
			wzImage.WzProperties.Remove(canvas);
			wzImage.WzProperties.Add(wzCanvasProperty);
		}
		else if (parent is WzSubProperty wzSubProperty)
		{
			wzSubProperty.RemoveProperty(canvas);
			wzSubProperty.AddProperty(wzCanvasProperty);
		}
		else if (parent is WzConvexProperty wzConvexProperty)
		{
			wzConvexProperty.WzProperties.Remove(canvas);
			wzConvexProperty.AddProperty(wzCanvasProperty);
		}
	}

	private void UpdateCanvasIndexFile(string canvasDir, int maxIndex)
	{
		string text = Path.Combine(canvasDir, "Canvas.ini");
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("LastWzIndex|");
		handler.AppendFormatted(maxIndex);
		stringBuilder2.AppendLine(ref handler);
		File.WriteAllText(text, stringBuilder.ToString());
		Debug.WriteLine("[Canvas] Created index file: " + text);
	}

	protected virtual void OnProgressChanged(PackingProgress progress)
	{
		ProgressChanged?.Invoke(this, new PackingProgressEventArgs(progress));
	}

	protected virtual void OnCategoryStarted(string category)
	{
		CategoryStarted?.Invoke(this, new CategoryPackingEventArgs(category, null));
	}

	protected virtual void OnCategoryCompleted(string category, CategoryPackingResult result)
	{
		CategoryCompleted?.Invoke(this, new CategoryPackingEventArgs(category, result));
	}

	protected virtual void OnErrorOccurred(Exception ex)
	{
		ErrorOccurred?.Invoke(this, new PackingErrorEventArgs(ex));
	}
}
