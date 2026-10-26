#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MapleLib.ClientLib;
using MapleLib.Helpers;
using MapleLib.MapleCryptoLib;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public class WzFile : WzObject
{
	internal string path;

	internal WzDirectory wzDir;

	internal WzHeader header;

	internal string name = "";

	internal ushort wzVersionHeader = 0;

	internal const ushort wzVersionHeader64bit_start = 770;

	internal uint versionHash = 0u;

	internal short mapleStoryPatchVersion = 0;

	internal WzMapleVersion maplepLocalVersion;

	internal MapleStoryLocalisation mapleLocaleVersion = MapleStoryLocalisation.Not_Known;

	internal bool wz_withEncryptVersionHeader = true;

	internal byte[] WzIv;

	private readonly Dictionary<string, WzObject> _pathCache = new Dictionary<string, WzObject>(StringComparer.OrdinalIgnoreCase);

	private bool _isUnloaded = false;

	public WzDirectory WzDirectory => wzDir;

	public override string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public override WzObjectType ObjectType => WzObjectType.File;

	public new WzObject this[string name] => WzDirectory[name];

	public WzHeader Header
	{
		get
		{
			return header;
		}
		set
		{
			header = value;
		}
	}

	public short Version
	{
		get
		{
			return mapleStoryPatchVersion;
		}
		set
		{
			mapleStoryPatchVersion = value;
		}
	}

	public string FilePath => path;

	public WzMapleVersion MapleVersion
	{
		get
		{
			return maplepLocalVersion;
		}
		set
		{
			maplepLocalVersion = value;
		}
	}

	public MapleStoryLocalisation MapleLocaleVersion
	{
		get
		{
			return mapleLocaleVersion;
		}
		private set
		{
		}
	}

	public bool Is64BitWzFile
	{
		get
		{
			return !wz_withEncryptVersionHeader;
		}
		private set
		{
		}
	}

	public override WzObject Parent
	{
		get
		{
			return null;
		}
		internal set
		{
		}
	}

	public override WzFile WzFileParent => this;

	public bool IsUnloaded
	{
		get
		{
			return _isUnloaded;
		}
		private set
		{
		}
	}

	public override void Dispose()
	{
		_isUnloaded = true;
		if (wzDir != null && wzDir.reader != null)
		{
			wzDir.reader.Close();
			wzDir.reader = null;
			Header = null;
			path = null;
			name = null;
			_pathCache.Clear();
			WzDirectory.Dispose();
		}
	}

	public WzFile(short gameVersion, WzMapleVersion version)
	{
		wzDir = new WzDirectory();
		Header = WzHeader.GetDefault();
		mapleStoryPatchVersion = gameVersion;
		maplepLocalVersion = version;
		WzIv = WzTool.GetIvByMapleVersion(version);
		wzDir.WzIv = WzIv;
	}

	public WzFile(string filePath, WzMapleVersion version)
		: this(filePath, -1, version)
	{
	}

	public WzFile(string filePath, short gameVersion, WzMapleVersion version)
	{
		name = Path.GetFileName(filePath);
		path = filePath;
		mapleStoryPatchVersion = gameVersion;
		maplepLocalVersion = version;
		if (version == WzMapleVersion.GETFROMZLZ)
		{
			using (FileStream zlzStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(filePath), "ZLZ.dll")))
			{
				WzIv = WzKeyGenerator.GetIvFromZlz(zlzStream);
				return;
			}
		}
		WzIv = WzTool.GetIvByMapleVersion(version);
	}

	public WzFile(string filePath, byte[] wzIv)
	{
		name = Path.GetFileName(filePath);
		path = filePath;
		mapleStoryPatchVersion = -1;
		maplepLocalVersion = WzMapleVersion.CUSTOM;
		WzIv = wzIv;
	}

	public WzFileParseStatus ParseWzFile(byte[] WzIv = null)
	{
		if (WzIv != null)
		{
			this.WzIv = WzIv;
		}
		return ParseMainWzDirectory();
	}

	internal WzFileParseStatus ParseMainWzDirectory(bool lazyParse = false)
	{
		if (path == null)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] Path is null");
			return WzFileParseStatus.Path_Is_Null;
		}
		WzBinaryReader wzBinaryReader = new WzBinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), WzIv, 0L);
		Header = new WzHeader();
		Header.Ident = wzBinaryReader.ReadString(4);
		Header.FSize = wzBinaryReader.ReadUInt64();
		Header.FStart = wzBinaryReader.ReadUInt32();
		Header.Copyright = wzBinaryReader.ReadString((int)(Header.FStart - 17));
		byte b = wzBinaryReader.ReadByte();
		byte[] array = wzBinaryReader.ReadBytes((int)(Header.FStart - wzBinaryReader.BaseStream.Position));
		wzBinaryReader.Header = Header;
		Check64BitClient(wzBinaryReader);
		wzVersionHeader = (ushort)(wz_withEncryptVersionHeader ? wzBinaryReader.ReadUInt16() : 770);
		Debug.WriteLine("----------------------------------------");
		Debug.WriteLine($"Read Wz File {Name}");
		Debug.WriteLine($"wz_withEncryptVersionHeader: {wz_withEncryptVersionHeader}");
		Debug.WriteLine($"wzVersionHeader: {wzVersionHeader}");
		Debug.WriteLine("----------------------------------------");
		if (mapleStoryPatchVersion == -1)
		{
			if (!wz_withEncryptVersionHeader)
			{
				for (ushort num = 770; num < 780; num++)
				{
					if (TryDecodeWithWZVersionNumber(wzBinaryReader, wzVersionHeader, num, lazyParse))
					{
						return WzFileParseStatus.Success;
					}
				}
			}
			short mapleStoryVerFromExe = GetMapleStoryVerFromExe(path, out mapleLocaleVersion);
			for (int i = mapleStoryVerFromExe; i < 2000; i++)
			{
				if (TryDecodeWithWZVersionNumber(wzBinaryReader, wzVersionHeader, i, lazyParse))
				{
					return WzFileParseStatus.Success;
				}
			}
			return WzFileParseStatus.Error_Game_Ver_Hash;
		}
		versionHash = CheckAndGetVersionHash(wzVersionHeader, mapleStoryPatchVersion);
		wzBinaryReader.Hash = versionHash;
		WzDirectory wzDirectory = new WzDirectory(wzBinaryReader, name, versionHash, WzIv, this);
		wzDirectory.ParseDirectory();
		wzDir = wzDirectory;
		return WzFileParseStatus.Success;
	}

	private void Check64BitClient(WzBinaryReader reader)
	{
		if (Header.FSize >= 2)
		{
			reader.BaseStream.Position = header.FStart;
			int num = reader.ReadUInt16();
			if (num > 255)
			{
				wz_withEncryptVersionHeader = false;
			}
			else if (num == 128 && Header.FSize >= 5)
			{
				reader.BaseStream.Position = header.FStart;
				int num2 = reader.ReadInt32();
				if (num2 > 0 && (num2 & 0xFF) == 0 && num2 <= 65535)
				{
					wz_withEncryptVersionHeader = false;
				}
			}
		}
		else
		{
			wz_withEncryptVersionHeader = false;
		}
		reader.BaseStream.Position = Header.FStart;
	}

	private bool TryDecodeWithWZVersionNumber(WzBinaryReader reader, int useWzVersionHeader, int useMapleStoryPatchVersion, bool lazyParse)
	{
		mapleStoryPatchVersion = (short)useMapleStoryPatchVersion;
		versionHash = CheckAndGetVersionHash(useWzVersionHeader, mapleStoryPatchVersion);
		if (versionHash == 0)
		{
			return false;
		}
		reader.Hash = versionHash;
		long position = reader.BaseStream.Position;
		WzDirectory wzDirectory;
		try
		{
			wzDirectory = new WzDirectory(reader, name, versionHash, WzIv, this);
			wzDirectory.ParseDirectory(lazyParse);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			reader.BaseStream.Position = position;
			return false;
		}
		try
		{
			WzImage wzImage = wzDirectory.WzImages.FirstOrDefault();
			if (wzImage != null)
			{
				try
				{
					reader.BaseStream.Position = wzImage.Offset;
					byte b = reader.ReadByte();
					reader.BaseStream.Position = position;
					switch (b)
					{
					case 27:
					case 115:
					{
						WzDirectory wzDirectory2 = new WzDirectory(reader, name, versionHash, WzIv, this);
						wzDirectory2.ParseDirectory(lazyParse);
						wzDir = wzDirectory;
						return true;
					}
					default:
					{
						string message = $"[WzFile.cs] New Wz image header found. checkByte = {b}. File Name = {Name}";
						ErrorLogger.Log(ErrorLevel.MissingFeature, message);
						Debug.WriteLine(message);
						reader.BaseStream.Position = position;
						break;
					}
					}
				}
				catch
				{
					reader.BaseStream.Position = position;
					return false;
				}
				return true;
			}
			if (Is64BitWzFile && mapleStoryPatchVersion == 113)
			{
				reader.BaseStream.Position = position;
				return false;
			}
			wzDir = wzDirectory;
			return true;
		}
		finally
		{
		}
	}

	private static short GetMapleStoryVerFromExe(string wzFilePath, out MapleStoryLocalisation mapleLocaleVersion)
	{
		FileInfo fileInfo = new FileInfo(wzFilePath);
		if (!fileInfo.Exists)
		{
			mapleLocaleVersion = MapleStoryLocalisation.Not_Known;
			return 0;
		}
		DirectoryInfo directoryInfo = fileInfo.Directory;
		for (int i = 0; i < 4; i++)
		{
			FileInfo[] files = directoryInfo.GetFiles("MapleStory.exe", SearchOption.TopDirectoryOnly);
			FileInfo[] files2 = directoryInfo.GetFiles("MapleStoryT.exe", SearchOption.TopDirectoryOnly);
			FileInfo[] files3 = directoryInfo.GetFiles("MapleStoryA.exe", SearchOption.TopDirectoryOnly);
			List<FileInfo> list = new List<FileInfo>();
			if (files2.Length != 0 && files2[0].Exists)
			{
				list.Add(files2[0]);
			}
			if (files3.Length != 0 && files3[0].Exists)
			{
				list.Add(files3[0]);
			}
			if (files.Length != 0 && files[0].Exists)
			{
				list.Add(files[0]);
			}
			foreach (FileInfo item in list)
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(directoryInfo.FullName, item.FullName));
				if ((versionInfo.FileMajorPart == 1 && versionInfo.FileMinorPart == 0 && versionInfo.FileBuildPart == 0) || (versionInfo.FileMajorPart == 0 && versionInfo.FileMinorPart == 0 && versionInfo.FileBuildPart == 0))
				{
					continue;
				}
				int fileMajorPart = versionInfo.FileMajorPart;
				MapleStoryLocalisation mapleStoryLocalisation = MapleStoryLocalisation.Not_Known;
				if (Enum.IsDefined(typeof(MapleStoryLocalisation), fileMajorPart))
				{
					mapleStoryLocalisation = (MapleStoryLocalisation)fileMajorPart;
				}
				int fileMinorPart = versionInfo.FileMinorPart;
				int fileBuildPart = versionInfo.FileBuildPart;
				mapleLocaleVersion = mapleStoryLocalisation;
				return (short)fileMinorPart;
			}
			directoryInfo = directoryInfo.Parent;
			if (directoryInfo == null)
			{
				break;
			}
		}
		mapleLocaleVersion = MapleStoryLocalisation.Not_Known;
		return 0;
	}

	private static uint CheckAndGetVersionHash(int wzVersionHeader, int maplestoryPatchVersion)
	{
		uint num = 0u;
		string text = maplestoryPatchVersion.ToString();
		foreach (char c in text)
		{
			num = num * 32 + (byte)c + 1;
		}
		if (wzVersionHeader == 770)
		{
			return num;
		}
		int num2 = (byte)(~(((num >> 24) & 0xFF) ^ ((num >> 16) & 0xFF) ^ ((num >> 8) & 0xFF) ^ (num & 0xFF)));
		if (wzVersionHeader == num2)
		{
			return num;
		}
		return 0u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CreateWZVersionHash()
	{
		versionHash = 0u;
		string text = mapleStoryPatchVersion.ToString();
		foreach (char c in text)
		{
			versionHash = versionHash * 32 + (byte)c + 1;
		}
		wzVersionHeader = (byte)(~(((versionHash >> 24) & 0xFF) ^ ((versionHash >> 16) & 0xFF) ^ ((versionHash >> 8) & 0xFF) ^ (versionHash & 0xFF)));
	}

	public void SaveToDisk(string path, bool? override_saveAs64BitWZ = null, WzMapleVersion savingToPreferredWzVer = WzMapleVersion.UNKNOWN)
	{
		if (savingToPreferredWzVer == WzMapleVersion.UNKNOWN)
		{
			WzIv = WzTool.GetIvByMapleVersion(maplepLocalVersion);
		}
		else
		{
			WzIv = WzTool.GetIvByMapleVersion(savingToPreferredWzVer);
		}
		bool flag = WzIv.SequenceEqual(wzDir.WzIv);
		wzDir.WzIv = WzIv;
		bool bIsWzUserKeyDefault = MapleCryptoConstants.IsDefaultMapleStoryUserKey();
		bool flag2 = !wz_withEncryptVersionHeader;
		if (override_saveAs64BitWZ.HasValue)
		{
			flag2 = override_saveAs64BitWZ.Value;
		}
		CreateWZVersionHash();
		wzDir.SetVersionHash(versionHash);
		Debug.WriteLine("----------------------------------------");
		Debug.WriteLine($"Saving Wz File {Name}");
		Debug.WriteLine($"wzVersionHeader: {wzVersionHeader}");
		Debug.WriteLine($"bSaveAs64BitWz: {flag2}");
		Debug.WriteLine("----------------------------------------");
		try
		{
			string text = Path.Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + ".TEMP");
			File.Create(text).Close();
			using (FileStream prevOpenedStream = new FileStream(text, FileMode.Append, FileAccess.Write))
			{
				wzDir.GenerateDataFile(flag ? null : WzIv, bIsWzUserKeyDefault, prevOpenedStream);
			}
			WzTool.StringCache.Clear();
			using WzBinaryWriter wzBinaryWriter = new WzBinaryWriter(File.Create(path), WzIv);
			wzBinaryWriter.Hash = versionHash;
			uint imgOffsets = wzDir.GetImgOffsets(wzDir.GetOffsets(Header.FStart + (uint)((!flag2) ? 2 : 0)));
			Header.FSize = imgOffsets - Header.FStart;
			for (int i = 0; i < 4; i++)
			{
				wzBinaryWriter.Write((byte)Header.Ident[i]);
			}
			wzBinaryWriter.Write((long)Header.FSize);
			wzBinaryWriter.Write(Header.FStart);
			wzBinaryWriter.WriteNullTerminatedString(Header.Copyright);
			long num = Header.FStart - wzBinaryWriter.BaseStream.Position;
			if (num > 0)
			{
				wzBinaryWriter.Write(new byte[(int)num]);
			}
			if (!flag2)
			{
				wzBinaryWriter.Write(wzVersionHeader);
			}
			wzBinaryWriter.Header = Header;
			wzDir.SaveDirectory(wzBinaryWriter);
			wzBinaryWriter.StringCache.Clear();
			using (FileStream fs = File.OpenRead(text))
			{
				wzDir.SaveImages(wzBinaryWriter, fs);
			}
			File.Delete(text);
			wzBinaryWriter.StringCache.Clear();
		}
		finally
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}

	public void ExportXml(string path, bool oneFile)
	{
		if (oneFile)
		{
			FileStream stream = File.Create(path + "/" + name + ".xml");
			StreamWriter streamWriter = new StreamWriter(stream);
			int level = 0;
			streamWriter.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzFile", name, finish: true));
			wzDir.ExportXml(streamWriter, oneFile, level, isDirectory: false);
			streamWriter.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzFile"));
			streamWriter.Close();
			return;
		}
		throw new Exception("Under Construction");
	}

	public List<WzObject> GetObjectsFromWildcardPath(string path)
	{
		if (path.ToLower() == name.ToLower())
		{
			return new List<WzObject> { WzDirectory };
		}
		if (path == "*")
		{
			List<WzObject> list = new List<WzObject> { WzDirectory };
			list.AddRange(GetObjectsFromDirectory(WzDirectory));
			return list;
		}
		if (!path.Contains("*"))
		{
			return new List<WzObject> { GetObjectFromPath(path) };
		}
		string[] array = path.Split("/".ToCharArray());
		if (array.Length == 2 && array[1] == "*")
		{
			return GetObjectsFromDirectory(WzDirectory);
		}
		List<WzObject> result = (from spath in WzDirectory.WzImages.SelectMany((WzImage img) => GetPathsFromImage(img, name + "/" + img.Name)).Concat(wzDir.WzDirectories.SelectMany((WzDirectory dir) => GetPathsFromDirectory(dir, name + "/" + dir.Name)))
			where StringMatch(path, spath)
			select GetObjectFromPath(spath)).ToList();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		return result;
	}

	public List<WzObject> GetObjectsFromRegexPath(string path)
	{
		if (path.ToLower() == name.ToLower())
		{
			return new List<WzObject> { WzDirectory };
		}
		List<WzObject> result = (from spath in WzDirectory.WzImages.SelectMany((WzImage img) => GetPathsFromImage(img, name + "/" + img.Name)).Concat(wzDir.WzDirectories.SelectMany((WzDirectory dir) => GetPathsFromDirectory(dir, name + "/" + dir.Name)))
			where Regex.Match(spath, path).Success
			select GetObjectFromPath(spath)).ToList();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		return result;
	}

	public List<WzObject> GetObjectsFromDirectory(WzDirectory dir)
	{
		List<WzObject> list = new List<WzObject>();
		list.AddRange(dir.WzImages.SelectMany((WzImage img) => GetObjectsFromImage(img)));
		list.AddRange(dir.WzDirectories.SelectMany((WzDirectory subdir) => GetObjectsFromDirectory(subdir)));
		return list;
	}

	public List<WzObject> GetObjectsFromImage(WzImage img)
	{
		return img.WzProperties.SelectMany((WzImageProperty prop) =>
		{
			List<WzObject> list = new List<WzObject> { prop };
			list.AddRange(GetObjectsFromProperty(prop));
			return list;
		}).ToList();
	}

	public List<WzObject> GetObjectsFromProperty(WzImageProperty prop)
	{
		List<WzObject> list = new List<WzObject>();
		List<WzImageProperty> source = new List<WzImageProperty>();
		bool flag = true;
		switch (prop.PropertyType)
		{
		case WzPropertyType.Canvas:
			source = ((WzCanvasProperty)prop).WzProperties;
			list.Add(((WzCanvasProperty)prop).PngProperty);
			break;
		case WzPropertyType.Convex:
			source = ((WzConvexProperty)prop).WzProperties;
			break;
		case WzPropertyType.SubProperty:
			source = ((WzSubProperty)prop).WzProperties;
			break;
		case WzPropertyType.Vector:
			list.Add(((WzVectorProperty)prop).X);
			list.Add(((WzVectorProperty)prop).Y);
			flag = false;
			break;
		}
		if (flag)
		{
			list.AddRange(source.SelectMany((WzImageProperty p) => GetObjectsFromProperty(p)));
		}
		return list;
	}

	internal List<string> GetPathsFromDirectory(WzDirectory dir, string curPath)
	{
		return dir.WzImages.SelectMany((WzImage img) =>
		{
			List<string> list = new List<string> { curPath + "/" + img.Name };
			list.AddRange(GetPathsFromImage(img, curPath + "/" + img.Name));
			return list;
		}).Concat(dir.WzDirectories.SelectMany((WzDirectory subdir) =>
		{
			List<string> list = new List<string> { curPath + "/" + subdir.Name };
			list.AddRange(GetPathsFromDirectory(subdir, curPath + "/" + subdir.Name));
			return list;
		})).ToList();
	}

	internal List<string> GetPathsFromImage(WzImage img, string curPath)
	{
		return img.WzProperties.SelectMany((WzImageProperty prop) =>
		{
			List<string> list = new List<string> { curPath + "/" + prop.Name };
			list.AddRange(GetPathsFromProperty(prop, curPath + "/" + prop.Name));
			return list;
		}).ToList();
	}

	internal List<string> GetPathsFromProperty(WzImageProperty prop, string curPath)
	{
		List<string> list = new List<string>();
		List<WzImageProperty> source = new List<WzImageProperty>();
		bool flag = true;
		switch (prop.PropertyType)
		{
		case WzPropertyType.Canvas:
			source = ((WzCanvasProperty)prop).WzProperties;
			list.Add(curPath + "/PNG");
			break;
		case WzPropertyType.Convex:
			source = ((WzConvexProperty)prop).WzProperties;
			break;
		case WzPropertyType.SubProperty:
			source = ((WzSubProperty)prop).WzProperties;
			break;
		case WzPropertyType.Vector:
			list.Add(curPath + "/X");
			list.Add(curPath + "/Y");
			flag = false;
			break;
		}
		if (flag)
		{
			list.AddRange(source.SelectMany((WzImageProperty p) => GetPathsFromProperty(p, curPath + "/" + p.Name)));
		}
		return list;
	}

	public WzObject GetObjectFromPath(string path, bool checkFirstDirectoryName = true)
	{
		if (_pathCache.TryGetValue(path, out var value))
		{
			return value;
		}
		string[] array = path.Split('/');
		if (array.Length == 1)
		{
			_pathCache[path] = WzDirectory;
			return WzDirectory;
		}
		WzObject wzObject = null;
		int num = 0;
		if (checkFirstDirectoryName)
		{
			if (WzFileManager.fileManager == null)
			{
				return null;
			}
			if (WzFileManager.ContainsCanvasDirectory(path))
			{
				string baseName = WzFileManager.NormaliseWzCanvasDirectory(path).Replace("/", "\\");
				List<WzDirectory> wzDirectoriesFromBase = WzFileManager.fileManager.GetWzDirectoriesFromBase(baseName, isCanvas: true);
				string text = "/" + WzFileManager.CANVAS_DIRECTORY_NAME + "/";
				string text2 = (path.Contains(text) ? path.Substring(path.IndexOf(text) + text.Length) : path);
				string[] array2 = text2.Split('/');
				bool flag = false;
				foreach (WzDirectory item in wzDirectoriesFromBase)
				{
					WzObject wzObject2 = item[array2[0]];
					if (wzObject2 != null)
					{
						wzObject = wzObject2;
						num = array.Length - array2.Length + 1;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return null;
				}
			}
			else
			{
				List<WzDirectory> wzDirectoriesFromBase2 = WzFileManager.fileManager.GetWzDirectoriesFromBase(array[0], isCanvas: true);
				WzDirectory wzDirectory = null;
				foreach (WzDirectory item2 in wzDirectoriesFromBase2)
				{
					ReadOnlySpan<char> readOnlySpan = item2.name;
					ReadOnlySpan<char> other = array[0];
					if (string.Equals(item2.name, array[0], StringComparison.OrdinalIgnoreCase) || (item2.name.Length > 3 && readOnlySpan.Slice(0, item2.name.Length - 3).SequenceEqual(other) && string.Equals(item2.name.Substring(0, item2.name.Length - 3), array[0], StringComparison.OrdinalIgnoreCase)))
					{
						wzDirectory = item2;
						break;
					}
				}
				if (wzDirectory != null)
				{
					wzObject = wzDirectory;
					num = 1;
				}
				else
				{
					if (array.Length < 2)
					{
						return null;
					}
					wzObject = WzFileManager.fileManager.FindWzImageByName(array[0], array[1]);
					if (wzObject != null)
					{
						num = 2;
					}
					else
					{
						if (array.Length < 3)
						{
							return null;
						}
						wzObject = WzFileManager.fileManager.FindWzImageByName(array[0] + Path.DirectorySeparatorChar + array[1], array[2]);
						if (wzObject == null)
						{
							return null;
						}
						num = 3;
					}
				}
			}
		}
		else
		{
			wzObject = WzDirectory;
			num = 0;
		}
		if (wzObject == null)
		{
			return null;
		}
		for (int i = num; i < array.Length; i++)
		{
			string text3 = array[i];
			if (wzObject == null)
			{
				return null;
			}
			switch (wzObject.ObjectType)
			{
			case WzObjectType.Directory:
				wzObject = ((WzDirectory)wzObject)[text3];
				break;
			case WzObjectType.Image:
				wzObject = ((WzImage)wzObject)[text3];
				break;
			case WzObjectType.Property:
				switch (((WzImageProperty)wzObject).PropertyType)
				{
				case WzPropertyType.Canvas:
					wzObject = ((WzCanvasProperty)wzObject)[text3];
					break;
				case WzPropertyType.Convex:
					wzObject = ((WzConvexProperty)wzObject)[text3];
					break;
				case WzPropertyType.SubProperty:
					wzObject = ((WzSubProperty)wzObject)[text3];
					break;
				case WzPropertyType.Vector:
					if (text3 == "X")
					{
						return ((WzVectorProperty)wzObject).X;
					}
					if (text3 == "Y")
					{
						return ((WzVectorProperty)wzObject).Y;
					}
					return null;
				default:
					return null;
				}
				break;
			}
		}
		_pathCache[path] = wzObject;
		return wzObject;
	}

	public static WzObject GetObjectFromMultipleWzFilePath(string path, IReadOnlyCollection<WzFile> wzFiles)
	{
		return wzFiles.Select((WzFile file) => file.GetObjectFromPath(path, checkFirstDirectoryName: false)).FirstOrDefault((WzObject obj) => obj != null);
	}

	internal bool StringMatch(string strWildCard, string strCompare)
	{
		int length = strWildCard.Length;
		int length2 = strCompare.Length;
		int i = 0;
		int j = 0;
		while (i < length && j < length2)
		{
			if (strWildCard[i] == '*')
			{
				for (; i < length && strWildCard[i] == '*'; i++)
				{
				}
				if (i == length)
				{
					return true;
				}
				for (; j < length2; j++)
				{
					if (StringMatch(strWildCard.Substring(i), strCompare.Substring(j)))
					{
						return true;
					}
				}
				return false;
			}
			if (strWildCard[i] == strCompare[j])
			{
				i++;
				j++;
				continue;
			}
			return false;
		}
		return i == length && j == length2;
	}

	public override void Remove()
	{
		Dispose();
	}
}
