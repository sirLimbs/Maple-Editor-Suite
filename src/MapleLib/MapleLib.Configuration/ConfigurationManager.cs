using System;
using System.Collections.Generic;
using System.IO;
using MapleLib.MapleCryptoLib;
using MapleLib.PacketLib;
using Newtonsoft.Json;

namespace MapleLib.Configuration;

public class ConfigurationManager
{
	private const string SETTINGS_FILE_USER = "Settings.txt";

	private const string SETTINGS_FILE_APPLICATION = "ApplicationSettings.txt";

	private const string SETTINGS_FILE_CUSTOM_KEYS = "CustomKeys.txt";

	public const string configPipeName = "HaRepacker";

	private bool loaded = false;

	private string folderPath;

	private UserSettings _userSettings = new UserSettings();

	private ApplicationSettings _appSettings = new ApplicationSettings();

	private List<EncryptionKey> _customKeys = new List<EncryptionKey>();

	public UserSettings UserSettings
	{
		get
		{
			return _userSettings;
		}
		private set
		{
		}
	}

	public ApplicationSettings ApplicationSettings
	{
		get
		{
			return _appSettings;
		}
		private set
		{
		}
	}

	public List<EncryptionKey> CustomKeys
	{
		get
		{
			return _customKeys;
		}
		private set
		{
		}
	}

	public ConfigurationManager()
	{
		folderPath = GetLocalFolderPath();
	}

	public static string GetLocalFolderPath()
	{
		string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string text = Path.Combine(path, "HaRepacker");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public bool Load()
	{
		string path = Path.Combine(folderPath, "Settings.txt");
		string path2 = Path.Combine(folderPath, "ApplicationSettings.txt");
		string path3 = Path.Combine(folderPath, "CustomKeys.txt");
		if (File.Exists(path) && File.Exists(path2) && File.Exists(path3))
		{
			string value = File.ReadAllText(path);
			string value2 = File.ReadAllText(path2);
			string value3 = File.ReadAllText(path3);
			try
			{
				_userSettings = JsonConvert.DeserializeObject<UserSettings>(value);
				_appSettings = JsonConvert.DeserializeObject<ApplicationSettings>(value2);
				_customKeys = JsonConvert.DeserializeObject<List<EncryptionKey>>(value3);
				return true;
			}
			catch (Exception)
			{
				try
				{
					File.Delete(path);
					File.Delete(path2);
					File.Delete(path3);
				}
				catch
				{
				}
			}
		}
		_userSettings = new UserSettings();
		_appSettings = new ApplicationSettings();
		_customKeys = new List<EncryptionKey>();
		return false;
	}

	public bool Save()
	{
		string value = JsonConvert.SerializeObject(_userSettings, Formatting.Indented);
		string value2 = JsonConvert.SerializeObject(_appSettings, Formatting.Indented);
		string value3 = JsonConvert.SerializeObject(_customKeys, Formatting.Indented);
		string path = Path.Combine(folderPath, "Settings.txt");
		string path2 = Path.Combine(folderPath, "ApplicationSettings.txt");
		string path3 = Path.Combine(folderPath, "CustomKeys.txt");
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(path))
			{
				streamWriter.Write(value);
			}
			using (StreamWriter streamWriter2 = new StreamWriter(path2))
			{
				streamWriter2.Write(value2);
			}
			using (StreamWriter streamWriter3 = new StreamWriter(path3))
			{
				streamWriter3.Write(value3);
			}
			return true;
		}
		catch
		{
		}
		return false;
	}

	public byte[] GetCusomWzIVEncryption()
	{
		if (!loaded)
		{
			loaded = Load();
		}
		if (loaded)
		{
			string mapleVersion_CustomEncryptionBytes = ApplicationSettings.MapleVersion_CustomEncryptionBytes;
			byte[] bytes = HexEncoding.GetBytes(mapleVersion_CustomEncryptionBytes);
			if (bytes.Length == 4)
			{
				return bytes;
			}
		}
		return new byte[4];
	}

	public void SetCustomWzUserKeyFromConfig()
	{
		MapleCryptoConstants.UserKey_WzLib = new byte[128];
		byte[] bytes = HexEncoding.GetBytes(ApplicationSettings.MapleVersion_CustomAESUserKey);
		if (bytes.Length != 0)
		{
			MapleCryptoConstants.UserKey_WzLib = new byte[MapleCryptoConstants.MAPLESTORY_USERKEY_DEFAULT.Length];
			for (int i = 0; i < MapleCryptoConstants.UserKey_WzLib.Length; i += 4)
			{
				MapleCryptoConstants.UserKey_WzLib[i] = bytes[i / 4];
				MapleCryptoConstants.UserKey_WzLib[i + 1] = 0;
				MapleCryptoConstants.UserKey_WzLib[i + 2] = 0;
				MapleCryptoConstants.UserKey_WzLib[i + 3] = 0;
			}
		}
	}
}
