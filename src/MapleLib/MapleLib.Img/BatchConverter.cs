using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapleLib.WzLib;

namespace MapleLib.Img;

public class BatchConverter
{
	private readonly WzExtractionService _extractionService;

	public event EventHandler<BatchProgress> ProgressChanged;

	public event EventHandler<string> VersionStarted;

	public event EventHandler<VersionConversionResult> VersionCompleted;

	public BatchConverter()
	{
		_extractionService = new WzExtractionService();
	}

	public async Task<BatchConversionResult> ConvertMultipleVersionsAsync(IEnumerable<WzSourceConfig> sources, string outputRoot, IProgress<BatchProgress> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		List<WzSourceConfig> sourceList = sources.ToList();
		DateTime startTime = DateTime.Now;
		BatchConversionResult result = new BatchConversionResult();
		Directory.CreateDirectory(outputRoot);
		int currentVersion = 0;
		foreach (WzSourceConfig source in sourceList)
		{
			cancellationToken.ThrowIfCancellationRequested();
			int num = currentVersion;
			currentVersion = num + 1;
			DateTime versionStartTime = DateTime.Now;
			BatchProgress batchProgress = new BatchProgress
			{
				CurrentVersion = currentVersion,
				TotalVersions = sourceList.Count,
				CurrentVersionName = source.VersionName,
				Message = "Starting conversion of " + source.VersionName + "..."
			};
			progress?.Report(batchProgress);
			VersionStarted?.Invoke(this, source.VersionName);
			VersionConversionResult versionResult = new VersionConversionResult
			{
				VersionName = source.VersionName,
				OutputPath = Path.Combine(outputRoot, source.VersionName)
			};
			try
			{
				Directory.CreateDirectory(versionResult.OutputPath);
				Progress<ExtractionProgress> extractionProgress = new Progress<ExtractionProgress>((ExtractionProgress ep) =>
				{
					batchProgress.CurrentCategory = ep.CurrentPhase;
					batchProgress.VersionProgress = ep.ProgressPercentage;
					batchProgress.OverallProgress = ((double)(currentVersion - 1) * 100.0 + ep.ProgressPercentage) / (double)sourceList.Count;
					batchProgress.Message = ep.CurrentPhase + ": " + ep.CurrentFile;
					progress?.Report(batchProgress);
					ProgressChanged?.Invoke(this, batchProgress);
				});
				ExtractionResult extractionResult = await _extractionService.ExtractAsync(source.SourcePath, versionResult.OutputPath, source.VersionName, source.DisplayName ?? source.VersionName, source.Encryption, resolveLinks: false, cancellationToken, extractionProgress);
				versionResult.Success = extractionResult.Success;
				versionResult.CategoriesExtracted = extractionResult.CategoriesExtracted.Count;
				versionResult.ImagesExtracted = extractionResult.TotalImagesExtracted;
				if (!extractionResult.Success)
				{
					versionResult.ErrorMessage = extractionResult.ErrorMessage ?? "Unknown error";
					result.Errors.Add(source.VersionName + ": " + versionResult.ErrorMessage);
					result.FailureCount++;
				}
				else
				{
					result.SuccessCount++;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex2)
			{
				versionResult.Success = false;
				versionResult.ErrorMessage = ex2.Message;
				result.Errors.Add(source.VersionName + ": " + ex2.Message);
				result.FailureCount++;
			}
			versionResult.ElapsedTime = DateTime.Now - versionStartTime;
			result.VersionResults.Add(versionResult);
			VersionCompleted?.Invoke(this, versionResult);
		}
		result.ElapsedTime = DateTime.Now - startTime;
		result.Success = result.FailureCount == 0;
		progress?.Report(new BatchProgress
		{
			CurrentVersion = sourceList.Count,
			TotalVersions = sourceList.Count,
			OverallProgress = 100.0,
			IsComplete = true,
			Message = $"Batch conversion complete. {result.SuccessCount} succeeded, {result.FailureCount} failed."
		});
		return result;
	}

	public static List<DetectedMapleInstallation> ScanForMapleInstallations(string searchPath)
	{
		List<DetectedMapleInstallation> list = new List<DetectedMapleInstallation>();
		if (!Directory.Exists(searchPath))
		{
			return list;
		}
		if (IsMapleStoryFolder(searchPath))
		{
			list.Add(AnalyzeInstallation(searchPath));
		}
		try
		{
			foreach (string item in Directory.EnumerateDirectories(searchPath))
			{
				if (IsMapleStoryFolder(item))
				{
					list.Add(AnalyzeInstallation(item));
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
		}
		return list;
	}

	private static bool IsMapleStoryFolder(string path)
	{
		return File.Exists(Path.Combine(path, "String.wz")) || File.Exists(Path.Combine(path, "Map.wz")) || File.Exists(Path.Combine(path, "Data.wz")) || Directory.Exists(Path.Combine(path, "Data"));
	}

	private static DetectedMapleInstallation AnalyzeInstallation(string path)
	{
		DetectedMapleInstallation detectedMapleInstallation = new DetectedMapleInstallation
		{
			Path = path,
			FolderName = Path.GetFileName(path)
		};
		detectedMapleInstallation.WzFileCount = Directory.EnumerateFiles(path, "*.wz").Count();
		string path2 = Path.Combine(path, "Data");
		detectedMapleInstallation.Is64Bit = Directory.Exists(path2) && Directory.EnumerateFiles(path2, "*.wz", SearchOption.AllDirectories).Any();
		detectedMapleInstallation.SuggestedVersionName = SuggestVersionName(path, detectedMapleInstallation.FolderName);
		detectedMapleInstallation.SuggestedEncryption = DetectEncryption(path);
		return detectedMapleInstallation;
	}

	private static string SuggestVersionName(string path, string folderName)
	{
		string text = folderName.ToLowerInvariant();
		if (text.Contains("v") && char.IsDigit(text[text.IndexOf('v') + 1]))
		{
			int num = text.IndexOf('v');
			string text2 = "v";
			for (int i = num + 1; i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'); i++)
			{
				text2 += text[i];
			}
			return text2;
		}
		return folderName.Replace(" ", "_").Replace(".", "_");
	}

	private static WzMapleVersion DetectEncryption(string path)
	{
		string path2 = Path.Combine(path, "String.wz");
		if (File.Exists(path2))
		{
			return WzMapleVersion.GMS;
		}
		return WzMapleVersion.GMS;
	}
}
