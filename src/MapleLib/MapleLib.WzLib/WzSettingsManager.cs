using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleLib.WzLib;

public class WzSettingsManager
{
	private readonly string settingFilePath;

	private readonly Type userSettingsType;

	private readonly Type appSettingsType;

	private const string USER_SETTING_JSON = "UserSettings";

	private const string APP_SETTING_JSON = "ApplicationSettings";

	public WzSettingsManager(string wzPath, Type userSettingsType, Type appSettingsType)
	{
		settingFilePath = wzPath;
		this.userSettingsType = userSettingsType;
		this.appSettingsType = appSettingsType;
	}

	public void LoadSettings()
	{
		if (File.Exists(settingFilePath))
		{
			string value = File.ReadAllText(settingFilePath);
			try
			{
				JObject jObject = (JObject)JsonConvert.DeserializeObject(value);
				LoadSettingsJson((JObject)jObject["UserSettings"], userSettingsType);
				LoadSettingsJson((JObject)jObject["ApplicationSettings"], appSettingsType);
			}
			catch
			{
			}
		}
	}

	private void LoadSettingsJson(JObject json, Type settingsHolderType)
	{
		FieldInfo[] fields = settingsHolderType.GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			string name = fieldInfo.Name;
			JObject jsonHoldingObject = (JObject)json[name];
			LoadField(fieldInfo, jsonHoldingObject);
		}
	}

	private void LoadField(FieldInfo fieldInfo, JObject jsonHoldingObject)
	{
		if (jsonHoldingObject == null)
		{
			return;
		}
		if (fieldInfo.FieldType.BaseType != null && fieldInfo.FieldType.BaseType.FullName == "System.Enum")
		{
			fieldInfo.SetValue(null, (int)jsonHoldingObject["value"]);
			return;
		}
		switch ((string?)jsonHoldingObject["type"])
		{
		case "Microsoft.Xna.Framework.Color":
		{
			uint packedValue = (uint)jsonHoldingObject["value"];
			Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(packedValue);
			fieldInfo.SetValue(null, color);
			break;
		}
		case "System.Drawing.Color":
		{
			int argb = (int)jsonHoldingObject["value"];
			System.Drawing.Color color2 = System.Drawing.Color.FromArgb(argb);
			fieldInfo.SetValue(null, color2);
			break;
		}
		case "System.Int32":
		{
			int num3 = (int)jsonHoldingObject["value"];
			fieldInfo.SetValue(null, num3);
			break;
		}
		case "System.Double":
		{
			double num2 = (double)jsonHoldingObject["value"];
			fieldInfo.SetValue(null, num2);
			break;
		}
		case "System.Single":
		{
			float num = (float)jsonHoldingObject["value"];
			fieldInfo.SetValue(null, num);
			break;
		}
		case "System.Drawing.Size":
		{
			int width = (int)jsonHoldingObject["valueHeight"];
			int height = (int)jsonHoldingObject["valueWidth"];
			Size size = new Size(width, height);
			fieldInfo.SetValue(null, size);
			break;
		}
		case "System.String":
		{
			string value2 = (string?)jsonHoldingObject["value"];
			fieldInfo.SetValue(null, value2);
			break;
		}
		case "System.Drawing.Bitmap":
		{
			string s = (string?)jsonHoldingObject["value"];
			byte[] buffer = Convert.FromBase64String(s);
			Bitmap value;
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				value = new Bitmap(stream);
			}
			fieldInfo.SetValue(null, value);
			break;
		}
		case "System.Boolean":
		{
			bool flag = (bool)jsonHoldingObject["value"];
			fieldInfo.SetValue(null, flag);
			break;
		}
		default:
			throw new Exception("Unsupported data type for WzSettings.");
		}
	}

	public void SaveSettings()
	{
		JObject value = SaveSettingsJson(userSettingsType);
		JObject value2 = SaveSettingsJson(appSettingsType);
		JObject jObject = new JObject();
		jObject.Add("UserSettings", value);
		jObject.Add("ApplicationSettings", value2);
		if (File.Exists(settingFilePath))
		{
			File.Delete(settingFilePath);
		}
		using StreamWriter textWriter = File.CreateText(settingFilePath);
		JsonSerializer jsonSerializer = new JsonSerializer();
		jsonSerializer.Serialize(textWriter, jObject);
	}

	private JObject SaveSettingsJson(Type settingsHolderType)
	{
		JObject jObject = new JObject();
		FieldInfo[] fields = settingsHolderType.GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			SaveField(fieldInfo, jObject);
		}
		return jObject;
	}

	private void SaveField(FieldInfo fieldInfo, JObject jObjHolder)
	{
		string name = fieldInfo.Name;
		JObject jObject = new JObject();
		jObjHolder.Add(name, jObject);
		jObject.Add("type", fieldInfo.FieldType.FullName);
		if (fieldInfo.FieldType.BaseType != null && fieldInfo.FieldType.BaseType.FullName == "System.Enum")
		{
			jObject.Add("value", (int)fieldInfo.GetValue(null));
			return;
		}
		switch (fieldInfo.FieldType.FullName)
		{
		case "Microsoft.Xna.Framework.Color":
		{
			uint packedValue = ((Microsoft.Xna.Framework.Color)fieldInfo.GetValue(null)).PackedValue;
			jObject.Add("value", packedValue);
			break;
		}
		case "System.Drawing.Color":
		{
			int num4 = ((System.Drawing.Color)fieldInfo.GetValue(null)).ToArgb();
			jObject.Add("value", num4);
			break;
		}
		case "System.Int32":
		{
			int num3 = (int)fieldInfo.GetValue(null);
			jObject.Add("value", num3);
			break;
		}
		case "System.Double":
		{
			double num2 = (double)fieldInfo.GetValue(null);
			jObject.Add("value", num2);
			break;
		}
		case "System.Single":
		{
			float num = (float)fieldInfo.GetValue(null);
			jObject.Add("value", num);
			break;
		}
		case "System.Drawing.Size":
		{
			Size size = (Size)fieldInfo.GetValue(null);
			jObject.Add("valueHeight", size.Height);
			jObject.Add("valueWidth", size.Width);
			break;
		}
		case "System.String":
		{
			string text2 = (string)fieldInfo.GetValue(null);
			jObject.Add("value", text2);
			break;
		}
		case "System.Drawing.Bitmap":
		{
			Bitmap value = (Bitmap)fieldInfo.GetValue(null);
			ImageConverter imageConverter = new ImageConverter();
			byte[] array = (byte[])imageConverter.ConvertTo(value, typeof(byte[]));
			string text = Convert.ToBase64String(array, 0, array.Length, Base64FormattingOptions.None);
			jObject.Add("value", text);
			break;
		}
		case "System.Boolean":
		{
			bool flag = (bool)fieldInfo.GetValue(null);
			jObject.Add("value", flag);
			break;
		}
		default:
			throw new Exception("Unsupported data type for WzSettings.");
		}
	}
}
