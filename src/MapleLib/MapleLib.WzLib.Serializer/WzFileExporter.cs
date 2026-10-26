#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.Serializer;

public class WzFileExporter
{
	private const string WZ_EXTRACT_ERROR_FILE = "WzExtract_Errors.txt";

	public static bool RunWzFilesExtraction(string[] wzFilesToDump, string baseDir, WzMapleVersion version, IWzFileSerializer serializer, Action<int>? progressCallback = null)
	{
		if (!Directory.Exists(baseDir))
		{
			Directory.CreateDirectory(baseDir);
		}
		foreach (string text in wzFilesToDump)
		{
			if (!WzTool.IsListFile(text))
			{
				using WzFile wzFile = new WzFile(text, version);
				WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
				serializer.SerializeFile(wzFile, Path.Combine(baseDir, wzFile.Name));
				progressCallback?.Invoke(1);
			}
		}
		ErrorLogger.SaveToFile("WzExtract_Errors.txt");
		return true;
	}

	public static void RunWzImgDirsExtraction(List<WzDirectory> dirsToDump, List<WzImage> imgsToDump, string baseDir, IWzImageSerializer serializer, Action<int>? progressCallback = null)
	{
		if (!Directory.Exists(baseDir))
		{
			Directory.CreateDirectory(baseDir);
		}
		foreach (WzImage item in imgsToDump)
		{
			string path = Path.Combine(baseDir, ProgressingWzSerializer.EscapeInvalidFilePathNames(item.Name));
			serializer.SerializeImage(item, path);
			progressCallback?.Invoke(1);
		}
		foreach (WzDirectory item2 in dirsToDump)
		{
			string path2 = Path.Combine(baseDir, ProgressingWzSerializer.EscapeInvalidFilePathNames(item2.Name));
			serializer.SerializeDirectory(item2, path2);
			progressCallback?.Invoke(1);
		}
		ErrorLogger.SaveToFile("WzExtract_Errors.txt");
	}

	public static void RunWzXmlExtraction(List<WzObject> objsToDump, string path, ProgressingWzSerializer serializers, Action<bool, int>? progressCallback = null)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		if (serializers is IWzObjectSerializer wzObjectSerializer)
		{
			progressCallback?.Invoke(arg1: true, objsToDump.Count);
			foreach (WzObject item in objsToDump)
			{
				wzObjectSerializer.SerializeObject(item, path);
				progressCallback?.Invoke(arg1: false, 1);
			}
		}
		else if (serializers is WzNewXmlSerializer wzNewXmlSerializer)
		{
			progressCallback?.Invoke(arg1: true, 1);
			wzNewXmlSerializer.ExportCombinedXml(objsToDump, path);
			progressCallback?.Invoke(arg1: false, 1);
		}
		ErrorLogger.SaveToFile("WzExtract_Errors.txt");
		stopwatch.Stop();
		Debug.WriteLine($"WZ files Extracted. Execution Time: {stopwatch.ElapsedMilliseconds} ms");
	}
}
