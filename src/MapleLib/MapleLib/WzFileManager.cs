#define DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using MapleLib.Helpers;
using MapleLib.WzLib;
using MapleLib.WzLib.MSFile;
using MapleLib.WzLib.Util;

namespace MapleLib;

public class WzFileManager : IDisposable
{
	private static readonly string[] EXCLUDED_DIRECTORY_FROM_WZ_LIST = new string[8] { "bak", "backup", "original", "xml", "hshield", "blackcipher", "harepacker", "hacreator" };

	public static readonly string[] COMMON_MAPLESTORY_DIRECTORY = new string[6] { "C:\\Nexon\\MapleStory", "D:\\Nexon\\Maple", "C:\\Program Files\\WIZET\\MapleStory", "C:\\MapleStory", "C:\\NEXPACE\\MapleStoryN", "C:\\Program Files (x86)\\Wizet\\MapleStorySEA" };

	public static string CANVAS_DIRECTORY_NAME = "_Canvas";

	public static WzFileManager fileManager;

	private readonly string baseDir;

	private readonly bool _bIsStandAloneWzFile;

	private readonly bool _bInitAs64Bit;

	private readonly bool _bIsPreBBDataWzFormat;

	private readonly bool _bIsMapleStoryClassicWorlds;

	private readonly ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

	private readonly Dictionary<string, WzFile> _wzFiles = new Dictionary<string, WzFile>();

	private readonly Dictionary<WzFile, bool> _wzFilesUpdated = new Dictionary<WzFile, bool>();

	private readonly Dictionary<string, bool> _wzCanvasSectionLoaded = new Dictionary<string, bool>();

	private readonly Dictionary<string, WzMainDirectory> _wzDirs = new Dictionary<string, WzMainDirectory>();

	private readonly Dictionary<string, WzImage> _wzImages = new Dictionary<string, WzImage>();

	private readonly Dictionary<WzImage, bool> _wzImagesUpdated = new Dictionary<WzImage, bool>();

	private readonly HashSet<WzImage> _updatedWzImages = new HashSet<WzImage>();

	private readonly List<string> _listWzPaths = new List<string>();

	private readonly Dictionary<string, List<string>> _wzFilesList = new Dictionary<string, List<string>>();

	private readonly Dictionary<string, string> _wzFilesDirectoryList = new Dictionary<string, string>();

	private readonly Dictionary<string, WzMsFile> _msFiles = new Dictionary<string, WzMsFile>();

	public const string BIG_BANG_MARKER = "BigBang!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!";

	public const string BIG_BANG_2_MARKER = "BigBang2!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!";

	private bool _builtWzFileDirectoryList = false;

	private bool _loadedPacksFiles = false;

	public string WzBaseDirectory
	{
		get
		{
			return _bInitAs64Bit ? (baseDir + "\\Data\\") : baseDir;
		}
		private set
		{
		}
	}

	public bool Is64Bit
	{
		get
		{
			return _bInitAs64Bit;
		}
		private set
		{
		}
	}

	public bool IsPreBBDataWzFormat
	{
		get
		{
			return _bIsPreBBDataWzFormat;
		}
		private set
		{
		}
	}

	public bool IsMapleStoryClassicWorlds
	{
		get
		{
			return _bIsMapleStoryClassicWorlds;
		}
		private set
		{
		}
	}

	public WzDirectory this[string name] => _wzDirs.ContainsKey(name.ToLower()) ? _wzDirs[name.ToLower()].MainDir : null;

	public ReadOnlyCollection<WzFile> WzFileList
	{
		get
		{
			return new List<WzFile>(_wzFiles.Values).AsReadOnly();
		}
		private set
		{
		}
	}

	public ReadOnlyCollection<WzImage> WzUpdatedImageList
	{
		get
		{
			return new List<WzImage>(_updatedWzImages).AsReadOnly();
		}
		private set
		{
		}
	}

	public ReadOnlyCollection<WzImage> WzImagesList
	{
		get
		{
			return new List<WzImage>(_wzImages.Values).AsReadOnly();
		}
		private set
		{
		}
	}

	public IReadOnlyDictionary<string, WzMsFile> MsFiles => _msFiles;

	private static bool IsListWz(string inBaseName)
	{
		return inBaseName == "list";
	}

	public WzFileManager()
	{
		baseDir = string.Empty;
		_bInitAs64Bit = false;
		_bIsStandAloneWzFile = false;
		fileManager = this;
	}

	public WzFileManager(string directory, bool bIsStandAloneWzFile)
	{
		baseDir = directory;
		_bIsStandAloneWzFile = bIsStandAloneWzFile;
		if (bIsStandAloneWzFile)
		{
			_bInitAs64Bit = false;
			_bIsPreBBDataWzFormat = false;
			_bIsMapleStoryClassicWorlds = false;
		}
		else
		{
			_bInitAs64Bit = Detect64BitDirectoryWzFileFormat(baseDir);
			_bIsPreBBDataWzFormat = DetectIsPreBBDataWZFileFormat(baseDir);
			_bIsMapleStoryClassicWorlds = _bInitAs64Bit && !_bIsPreBBDataWzFormat;
		}
		fileManager = this;
	}

	public static bool Detect64BitDirectoryWzFileFormat(string baseDirectoryPath)
	{
		if (!Directory.Exists(baseDirectoryPath))
		{
			throw new Exception("Non-existent directory provided.");
		}
		string path = Path.Combine(baseDirectoryPath, "Data");
		if (Directory.Exists(path))
		{
			string searchPattern = "*.wz";
			int num = Directory.EnumerateFileSystemEntries(path, searchPattern, SearchOption.AllDirectories).Count();
			if (num > 40)
			{
				return true;
			}
		}
		return false;
	}

	public static bool DetectIsPreBBDataWZFileFormat(string baseDirectoryPath)
	{
		return DetectIsPreBBDataWZFileFormat(baseDirectoryPath, null);
	}

	public static bool DetectIsPreBBDataWZFileFormat(string baseDirectoryPath, WzMapleVersion? encryption)
	{
		if (!Directory.Exists(baseDirectoryPath))
		{
			throw new Exception("Non-existent directory provided.");
		}
		string path = Path.Combine(baseDirectoryPath, "List.wz");
		if (File.Exists(path) && WzTool.IsListFile(path))
		{
			return true;
		}
		string path2 = Path.Combine(baseDirectoryPath, "Data.wz");
		if (File.Exists(path2))
		{
			string path3 = Path.Combine(baseDirectoryPath, "Skill.wz");
			string path4 = Path.Combine(baseDirectoryPath, "String.wz");
			string path5 = Path.Combine(baseDirectoryPath, "Character.wz");
			bool flag = File.Exists(path3);
			bool flag2 = File.Exists(path4);
			bool flag3 = File.Exists(path5);
			if (!flag && !flag2 && !flag3)
			{
				string path6 = Path.Combine(baseDirectoryPath, "Data", "Skill");
				string path7 = Path.Combine(baseDirectoryPath, "Data", "String");
				string path8 = Path.Combine(baseDirectoryPath, "Data", "Character");
				bool flag4 = Directory.Exists(path6);
				bool flag5 = Directory.Exists(path7);
				bool flag6 = Directory.Exists(path8);
				if (!flag4 && !flag5 && !flag6)
				{
					return true;
				}
			}
		}
		if (encryption.HasValue)
		{
			string text = Path.Combine(baseDirectoryPath, "UI.wz");
			if (File.Exists(text))
			{
				try
				{
					using WzFile wzFile = new WzFile(text, encryption.Value);
					WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
					if (wzFileParseStatus == WzFileParseStatus.Success)
					{
						WzImage wzImage = wzFile.WzDirectory?.GetImageByName("UIWindow2.img");
						if (wzImage == null)
						{
							return true;
						}
						wzImage.ParseImage();
						WzImageProperty wzImageProperty = wzImage["BigBang!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"];
						if (wzImageProperty == null)
						{
							return true;
						}
					}
				}
				catch
				{
				}
			}
		}
		return false;
	}

	public static bool DetectBetaDataWzFormat(string baseDirectoryPath)
	{
		if (!Directory.Exists(baseDirectoryPath))
		{
			return false;
		}
		string path = Path.Combine(baseDirectoryPath, "Data.wz");
		if (!File.Exists(path))
		{
			return false;
		}
		string path2 = Path.Combine(baseDirectoryPath, "Skill.wz");
		string path3 = Path.Combine(baseDirectoryPath, "String.wz");
		string path4 = Path.Combine(baseDirectoryPath, "Character.wz");
		if (File.Exists(path2) || File.Exists(path3) || File.Exists(path4))
		{
			return false;
		}
		string text = Path.Combine(baseDirectoryPath, "Data");
		if (Directory.Exists(text))
		{
			string path5 = Path.Combine(text, "Skill");
			string path6 = Path.Combine(text, "String");
			string path7 = Path.Combine(text, "Character");
			if (Directory.Exists(path5) || Directory.Exists(path6) || Directory.Exists(path7))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsBigBangUpdate(WzImage uiWindow2Image)
	{
		return uiWindow2Image?["BigBang!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"] != null;
	}

	public static bool IsBigBang2Update(WzImage uiWindow2Image)
	{
		return uiWindow2Image?["BigBang2!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"] != null;
	}

	public static bool DetectBigBang2Format(string mapleStoryPath, WzMapleVersion encryption, bool is64Bit)
	{
		if (DetectBetaDataWzFormat(mapleStoryPath))
		{
			return false;
		}
		try
		{
			string text = null;
			if (is64Bit)
			{
				string path = Path.Combine(mapleStoryPath, "Data", "UI");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "UI_*.wz");
					text = files.FirstOrDefault();
				}
			}
			else
			{
				text = Path.Combine(mapleStoryPath, "UI.wz");
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				return false;
			}
			using WzFile wzFile = new WzFile(text, encryption);
			WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
			if (wzFileParseStatus != WzFileParseStatus.Success)
			{
				return false;
			}
			WzImage wzImage = wzFile.WzDirectory?.GetImageByName("UIWindow2.img");
			if (wzImage == null)
			{
				return false;
			}
			wzImage.ParseImage();
			return IsBigBang2Update(wzImage);
		}
		catch
		{
			return false;
		}
	}

	public (string iniFilePath, int fileIndex) GetIniWzIndexInfo(string directoryPath)
	{
		if (!Path.Exists(directoryPath))
		{
			return (iniFilePath: null, fileIndex: -1);
		}
		string[] files = Directory.GetFiles(directoryPath, "*.ini");
		if (files.Length == 0 || files.Length > 1)
		{
			throw new Exception(".ini file at the directory '" + directoryPath + "' is missing, or unavailable.");
		}
		string text = files[0];
		if (!File.Exists(text))
		{
			throw new Exception(".ini file at the directory '" + directoryPath + "' is missing.");
		}
		string[] array = File.ReadAllLines(text);
		if (array.Length == 0)
		{
			throw new Exception(".ini file does not contain LastWzIndex information.");
		}
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string[] array3 = array[0].Split('|');
			if (array3[0] == "LastWzIndex")
			{
				if (array3.Length <= 1)
				{
					throw new Exception(".ini file does not contain LastWzIndex information.");
				}
				return (iniFilePath: text, fileIndex: int.Parse(array3[1]));
			}
		}
		throw new Exception(".ini file does not contain LastWzIndex information.");
	}

	public void BuildWzFileList()
	{
		if (_builtWzFileDirectoryList)
		{
			return;
		}
		_builtWzFileDirectoryList = true;
		bool bInitAs64Bit = _bInitAs64Bit;
		string wzBaseDirectory = WzBaseDirectory;
		if (bInitAs64Bit)
		{
			IEnumerable<string> enumerable = from dir in Directory.EnumerateDirectories(wzBaseDirectory, "*", SearchOption.AllDirectories)
				where !EXCLUDED_DIRECTORY_FROM_WZ_LIST.Any((string x) => dir.ToLower().Contains(x))
				select dir;
			{
				foreach (string item3 in enumerable)
				{
					string name = new DirectoryInfo(item3).Name;
					if (name == "Packs")
					{
						continue;
					}
					(string iniFilePath, int fileIndex) iniWzIndexInfo = GetIniWzIndexInfo(item3);
					string item = iniWzIndexInfo.iniFilePath;
					int item2 = iniWzIndexInfo.fileIndex;
					for (int num = 0; num <= item2; num++)
					{
						string path = string.Format(item.Replace(".ini", "_{0}.wz"), num.ToString("D3"));
						string fileName = Path.GetFileName(path);
						string fileName2 = fileName.Replace(".wz", "");
						string text = item3.Replace(wzBaseDirectory, "").ToLower();
						if (EXCLUDED_DIRECTORY_FROM_WZ_LIST.Any((string value) => fileName2.ToLower().Contains(value)))
						{
							continue;
						}
						if (_wzFilesList.ContainsKey(text))
						{
							_wzFilesList[text].Add(fileName2);
						}
						else
						{
							_wzFilesList.Add(text, new List<string> { fileName2 });
						}
						if (!ContainsCanvasDirectory(path))
						{
							if (!_wzFilesDirectoryList.ContainsKey(fileName2))
							{
								_wzFilesDirectoryList.Add(fileName2, item3);
							}
							continue;
						}
						string key = Path.Combine(text, fileName2.ToLower()).Replace("\\", "/");
						if (!_wzFilesDirectoryList.ContainsKey(key))
						{
							_wzFilesDirectoryList.Add(key, item3);
						}
					}
				}
				return;
			}
		}
		IEnumerable<string> enumerable2 = from f in Directory.EnumerateFileSystemEntries(wzBaseDirectory, "*.wz", SearchOption.AllDirectories)
			where !File.GetAttributes(f).HasFlag(FileAttributes.Directory) && !EXCLUDED_DIRECTORY_FROM_WZ_LIST.Any((string x) => x.ToLower() == new DirectoryInfo(Path.GetDirectoryName(f)).Name)
			select f;
		foreach (string item4 in enumerable2)
		{
			string directoryName = Path.GetDirectoryName(item4);
			string fileName3 = Path.GetFileName(item4);
			string text2 = fileName3.Replace(".wz", "");
			string key2 = new string((from c in text2.ToLower()
				where char.IsLetter(c)
				select c).ToArray());
			if (_wzFilesList.ContainsKey(key2))
			{
				_wzFilesList[key2].Add(text2);
			}
			else
			{
				_wzFilesList.Add(key2, new List<string> { text2 });
			}
			if (!_wzFilesDirectoryList.ContainsKey(text2))
			{
				_wzFilesDirectoryList.Add(text2, directoryName);
			}
		}
	}

	public void LoadPacksFiles()
	{
		if (_loadedPacksFiles)
		{
			return;
		}
		_loadedPacksFiles = true;
		bool bInitAs64Bit = _bInitAs64Bit;
		string wzBaseDirectory = WzBaseDirectory;
		if (!bInitAs64Bit)
		{
			return;
		}
		string text = (from dir in Directory.EnumerateDirectories(wzBaseDirectory, "Packs", SearchOption.AllDirectories)
			where !EXCLUDED_DIRECTORY_FROM_WZ_LIST.Any((string x) => dir.ToLower().Contains(x))
			select dir).FirstOrDefault();
		if (text == null)
		{
			return;
		}
		string[] files = Directory.GetFiles(text, "*.ms", SearchOption.TopDirectoryOnly);
		string[] array = files;
		foreach (string text2 in array)
		{
			string fileName = Path.GetFileName(text2);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
			string text3 = fileNameWithoutExtension.ToLower();
			if (_msFiles.ContainsKey(text3))
			{
				continue;
			}
			try
			{
				FileStream fileStream = File.OpenRead(text2);
				MemoryStream memoryStream = new MemoryStream();
				fileStream.CopyTo(memoryStream);
				memoryStream.Position = 0L;
				WzMsFile wzMsFile = new WzMsFile(memoryStream, fileName, text2, leaveOpen: true);
				wzMsFile.ReadEntries();
				string key = text3.Split('_')[0];
				_msFiles.Add(text3, wzMsFile);
				WzFile wzFile = wzMsFile.LoadAsWzFile();
				LoadWzFile(wzFile.Name, wzFile);
				string item = text3;
				if (!_wzFilesList.ContainsKey(key))
				{
					_wzFilesList[key] = new List<string> { item };
				}
				else if (!_wzFilesList[key].Contains(item))
				{
					_wzFilesList[key].Add(item);
				}
				string text4 = fileNameWithoutExtension.Split('_')[0];
				string text5 = ((fileNameWithoutExtension.Split('_').Length > 1) ? fileNameWithoutExtension.Split('_')[1] : "");
				string key2 = text4 + ((text5.Length >= 3) ? ("_" + text5.Substring(0, 3)) : "");
				if (!_wzFilesDirectoryList.ContainsKey(key2))
				{
					_wzFilesDirectoryList[key2] = text;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to load MS file '" + text2 + "': " + ex.Message);
			}
		}
		string[] files2 = Directory.GetFiles(text, "*.nm", SearchOption.TopDirectoryOnly);
		string[] array2 = files2;
		foreach (string path in array2)
		{
			string text6 = Path.GetFileNameWithoutExtension(path).ToLower();
		}
	}

	public bool LoadListWzFile(WzMapleVersion fileVersion)
	{
		if (!Is64Bit)
		{
			if (_listWzPaths.Count > 0)
			{
				return true;
			}
			try
			{
				string wzFilePath = GetWzFilePath("List");
				List<string> collection = ListFileParser.ParseListFile(wzFilePath, fileVersion);
				_listWzPaths.AddRange(collection);
				return true;
			}
			catch
			{
			}
		}
		return false;
	}

	public bool IsWzFileLoaded(string baseName)
	{
		bool flag = false;
		if (Is64Bit)
		{
			flag = ContainsCanvasDirectory(baseName);
		}
		string key = baseName.ToLower().Replace(".wz", "");
		return _wzFiles.ContainsKey(key);
	}

	public WzFile LoadWzFile(string baseName, WzMapleVersion encVersion)
	{
		string wzFilePath = GetWzFilePath(baseName);
		if (wzFilePath == null)
		{
			return null;
		}
		WzFile wzFile = new WzFile(wzFilePath, encVersion);
		WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
		if (wzFileParseStatus != WzFileParseStatus.Success)
		{
			throw new Exception("Error parsing " + baseName + ".wz (" + wzFileParseStatus.GetErrorDescription() + ")");
		}
		LoadWzFile(baseName, wzFile);
		return wzFile;
	}

	public WzFile LoadWzFile(string baseName, WzFile wzf)
	{
		string text = baseName.ToLower().Replace(".wz", "");
		if (_wzFilesUpdated.ContainsKey(wzf))
		{
			throw new Exception($"Wz {text} at the path {wzf.FilePath} has already been loaded, and cannot be loaded again. Remove it from memory first.");
		}
		_readWriteLock.EnterWriteLock();
		try
		{
			_wzFiles[text] = wzf;
			_wzFilesUpdated[wzf] = false;
			_wzDirs[text] = new WzMainDirectory(wzf);
		}
		finally
		{
			_readWriteLock.ExitWriteLock();
		}
		return wzf;
	}

	public void LoadCanvasSection(string canvasFolder, WzMapleVersion encVersion)
	{
		if (_wzCanvasSectionLoaded.ContainsKey(canvasFolder) && _wzCanvasSectionLoaded[canvasFolder])
		{
			return;
		}
		string directoryPath = Path.Combine(WzBaseDirectory, canvasFolder, CANVAS_DIRECTORY_NAME);
		var (text, num) = GetIniWzIndexInfo(directoryPath);
		if (text == null)
		{
			return;
		}
		string arg = $"{canvasFolder}/{CANVAS_DIRECTORY_NAME.ToLower()}/{CANVAS_DIRECTORY_NAME.ToLower()}_0";
		for (int i = 0; i <= num; i++)
		{
			string baseName = $"{arg}{i:D2}";
			if (!IsWzFileLoaded(baseName))
			{
				WzFile wzFile = LoadWzFile(baseName, encVersion);
			}
		}
		_readWriteLock.EnterWriteLock();
		try
		{
			_wzCanvasSectionLoaded[canvasFolder] = true;
		}
		finally
		{
			_readWriteLock.ExitWriteLock();
		}
	}

	public bool LoadLegacyDataWzFile(string baseName, WzMapleVersion encVersion)
	{
		string wzFilePath = GetWzFilePath(baseName);
		WzFile wzFile = new WzFile(wzFilePath, encVersion);
		WzFileParseStatus wzFileParseStatus = wzFile.ParseWzFile();
		if (wzFileParseStatus != WzFileParseStatus.Success)
		{
			MessageBox.Show("Error parsing " + baseName + ".wz (" + wzFileParseStatus.GetErrorDescription() + ")");
			return false;
		}
		baseName = baseName.ToLower();
		if (_wzFilesUpdated.ContainsKey(wzFile))
		{
			throw new Exception($"Wz file {baseName} at the path {wzFile.FilePath} has already been loaded, and cannot be loaded again.");
		}
		_readWriteLock.EnterWriteLock();
		try
		{
			_wzFiles[baseName] = wzFile;
			_wzFilesUpdated[wzFile] = false;
			_wzDirs[baseName] = new WzMainDirectory(wzFile);
		}
		finally
		{
			_readWriteLock.ExitWriteLock();
		}
		foreach (WzDirectory wzDirectory in wzFile.WzDirectory.WzDirectories)
		{
			_wzDirs[wzDirectory.Name.ToLower()] = new WzMainDirectory(wzFile, wzDirectory);
		}
		return true;
	}

	public WzImage LoadDataWzHotfixFile(string basePath, WzMapleVersion encVersion)
	{
		string wzFilePath = GetWzFilePath(basePath);
		if (!File.Exists(wzFilePath))
		{
			throw new Exception($"File '{basePath}' does not exist");
		}
		FileStream dataStream = File.Open(wzFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		WzImage wzImage = new WzImage(Path.GetFileName(wzFilePath), dataStream, encVersion);
		wzImage.ParseImage(forceReadFromData: true);
		_readWriteLock.EnterWriteLock();
		try
		{
			_wzImages[basePath] = wzImage;
			_wzImagesUpdated[wzImage] = false;
		}
		finally
		{
			_readWriteLock.ExitWriteLock();
		}
		return wzImage;
	}

	public void SetWzFileUpdated(string name, WzImage img)
	{
		img.Changed = true;
		_updatedWzImages.Add(img);
		WzFile file = GetMainDirectoryByName(name).File;
		SetWzFileUpdated(file);
	}

	public void SetWzFileUpdated(WzFile wzFile)
	{
		if (_wzFilesUpdated.ContainsKey(wzFile))
		{
			_readWriteLock.EnterWriteLock();
			try
			{
				_wzFilesUpdated[wzFile] = true;
				return;
			}
			finally
			{
				_readWriteLock.ExitWriteLock();
			}
		}
		throw new Exception("wz file to be flagged do not exist in memory " + wzFile.FilePath);
	}

	public List<WzFile> GetUpdatedWzFiles()
	{
		List<WzFile> list = new List<WzFile>();
		_readWriteLock.EnterReadLock();
		try
		{
			IEnumerable<WzFile> collection = (from keyPair in _wzFilesUpdated
				where keyPair.Value
				select keyPair.Key).AsEnumerable();
			list.AddRange(collection);
		}
		finally
		{
			_readWriteLock.ExitReadLock();
		}
		return list;
	}

	public void UnloadWzFile(WzFile wzFile, string wzFilePath)
	{
		string key = wzFilePath.ToLower().Replace(".wz", "");
		if (_wzFiles.ContainsKey(key))
		{
			_readWriteLock.EnterWriteLock();
			try
			{
				_wzFiles.Remove(key);
				_wzFilesUpdated.Remove(wzFile);
				_wzDirs.Remove(key);
			}
			finally
			{
				_readWriteLock.ExitWriteLock();
			}
			wzFile.Dispose();
		}
	}

	public void UnloadWzImgFile(WzImage wzImage)
	{
		string key = _wzImages.FirstOrDefault((KeyValuePair<string, WzImage> kvp) => kvp.Value == wzImage).Key;
		if (_wzImages.ContainsKey(key))
		{
			_readWriteLock.EnterWriteLock();
			try
			{
				_wzImages.Remove(key);
				_wzImagesUpdated.Remove(wzImage);
			}
			finally
			{
				_readWriteLock.ExitWriteLock();
			}
			wzImage.Dispose();
		}
	}

	public void Dispose()
	{
		_readWriteLock.EnterWriteLock();
		try
		{
			foreach (WzFile value in _wzFiles.Values)
			{
				value.Dispose();
			}
			_wzFiles.Clear();
			_wzFilesUpdated.Clear();
			_updatedWzImages.Clear();
			_wzDirs.Clear();
			_wzCanvasSectionLoaded.Clear();
		}
		finally
		{
			_readWriteLock.ExitWriteLock();
		}
	}

	public static bool ContainsCanvasDirectory(string path)
	{
		path = path.ToLower();
		string canvasDirLower = CANVAS_DIRECTORY_NAME.ToLower();
		string? directoryName = Path.GetDirectoryName(path);
		int result;
		if (directoryName == null)
		{
			result = 0;
		}
		else
		{
			InlineArray2<char> buffer = default;
			buffer[0] = Path.DirectorySeparatorChar;
			buffer[1] = Path.AltDirectorySeparatorChar;
			result = (directoryName.Split(buffer).Any((string dir) => dir == canvasDirLower) ? 1 : 0);
		}
		return (byte)result != 0;
	}

	public static string NormaliseWzCanvasDirectory(string filePathOrBaseFileName)
	{
		return Regex.Match(filePathOrBaseFileName, $"^.*?(?=)/{CANVAS_DIRECTORY_NAME}").Value.ToLower();
	}

	public WzMainDirectory GetMainDirectoryByName(string name)
	{
		name = name.ToLower();
		if (name.EndsWith(".wz"))
		{
			name = name.Replace(".wz", "");
		}
		return _wzDirs[name];
	}

	public List<string> GetWzFileNameListFromBase(string baseName)
	{
		if (_bIsPreBBDataWzFormat)
		{
			if (!_wzFilesList.ContainsKey("data"))
			{
				return new List<string>();
			}
			return _wzFilesList["data"];
		}
		if (!_wzFilesList.ContainsKey(baseName))
		{
			return new List<string>();
		}
		return _wzFilesList[baseName];
	}

	public List<WzDirectory> GetWzDirectoriesFromBase(string baseName, bool isCanvas = false)
	{
		List<string> wzFileNameListFromBase = GetWzFileNameListFromBase(baseName);
		if (_bIsPreBBDataWzFormat)
		{
			return (from name in wzFileNameListFromBase
				select this["data"][baseName] as WzDirectory into dir
				where dir != null
				select dir).ToList();
		}
		if (isCanvas)
		{
			string canvasDir = baseName.Replace("\\", "/") + "/";
			return (from name in wzFileNameListFromBase
				select this[canvasDir + name] into dir
				where dir != null
				select dir).ToList();
		}
		return (from name in wzFileNameListFromBase
			select this[name] into dir
			where dir != null
			select dir).ToList();
	}

	public WzObject FindWzImageByName(string baseWzName, string imageName)
	{
		baseWzName = baseWzName.ToLower();
		List<WzDirectory> wzDirectoriesFromBase = GetWzDirectoriesFromBase(baseWzName);
		if (imageName != string.Empty)
		{
			return (from wzFile in wzDirectoriesFromBase
				where wzFile != null && wzFile[imageName] != null
				select wzFile[imageName]).FirstOrDefault();
		}
		return wzDirectoriesFromBase.Where((WzDirectory wzFile) => wzFile != null).FirstOrDefault();
	}

	public List<WzObject> FindWzImagesByName(string baseWzName, string imageName)
	{
		baseWzName = baseWzName.ToLower();
		List<WzObject> list = new List<WzObject>();
		List<WzDirectory> wzDirectoriesFromBase = GetWzDirectoriesFromBase(baseWzName);
		list.AddRange(from wzFile in wzDirectoriesFromBase
			where wzFile != null && wzFile[imageName] != null
			select wzFile[imageName]);
		return list;
	}

	private string GetWzFilePath(string filePathOrBaseFileName)
	{
		if (!_wzFilesDirectoryList.ContainsKey(filePathOrBaseFileName))
		{
			if (File.Exists(filePathOrBaseFileName))
			{
				return filePathOrBaseFileName;
			}
			return null;
		}
		if (!ContainsCanvasDirectory(filePathOrBaseFileName))
		{
			string path = StringUtility.CapitalizeFirstCharacter(filePathOrBaseFileName) + ".wz";
			string fileName = Path.GetFileName(path);
			string text = Path.Combine(_wzFilesDirectoryList[filePathOrBaseFileName], fileName);
			if (!File.Exists(text))
			{
				throw new Exception("wz file at the path '" + text + "' does not exist.");
			}
			return text;
		}
		string path2 = _wzFilesDirectoryList[filePathOrBaseFileName];
		string fileName2 = Path.GetFileName(filePathOrBaseFileName);
		string text2 = Path.Combine(path2, fileName2 + ".wz");
		if (!File.Exists(text2))
		{
			throw new Exception("canvas wz file at the path '" + text2 + "' does not exist.");
		}
		return text2;
	}
}
