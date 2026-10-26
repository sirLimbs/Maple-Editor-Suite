#define DEBUG
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzCanvasProperty : WzExtended, IPropertyContainer
{
	public const string InlinkPropertyName = "_inlink";

	public const string OutlinkPropertyName = "_outlink";

	public const string OriginPropertyName = "origin";

	public const string HeadPropertyName = "head";

	public const string LtPropertyName = "lt";

	public const string AnimationDelayPropertyName = "delay";

	internal WzPropertyCollection properties;

	internal WzPngProperty imageProp;

	internal string name;

	internal WzObject parent;

	public override object WzValue => PngProperty;

	public override WzObject Parent
	{
		get
		{
			return parent;
		}
		internal set
		{
			parent = value;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.Canvas;

	public override WzPropertyCollection WzProperties => properties;

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

	public override WzImageProperty this[string name]
	{
		get
		{
			if (name == "PNG")
			{
				return imageProp;
			}
			return properties.FirstOrDefault((WzImageProperty iwp) => iwp.Name.ToLower() == name.ToLower());
		}
		set
		{
			if (value != null)
			{
				if (name == "PNG")
				{
					imageProp = (WzPngProperty)value;
					return;
				}
				value.Name = name;
				AddProperty(value);
			}
		}
	}

	public WzPngProperty PngProperty
	{
		get
		{
			return imageProp;
		}
		set
		{
			imageProp = value;
		}
	}

	public override void SetValue(object value)
	{
		imageProp.SetValue(value);
	}

	public override WzImageProperty DeepClone()
	{
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(name);
		foreach (WzImageProperty property in properties)
		{
			wzCanvasProperty.AddProperty(property.DeepClone());
		}
		wzCanvasProperty.imageProp = (WzPngProperty)imageProp.DeepClone();
		return wzCanvasProperty;
	}

	public WzImageProperty GetProperty(string name)
	{
		return properties.FirstOrDefault((WzImageProperty iwp) => iwp.Name.ToLower() == name.ToLower());
	}

	public override WzImageProperty GetFromPath(string path)
	{
		string[] array = path.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (array[0] == "..")
		{
			return ((WzImageProperty)Parent)[path.Substring(name.IndexOf('/') + 1)];
		}
		WzImageProperty wzImageProperty = this;
		string[] array2 = array;
		foreach (string segment in array2)
		{
			if (segment == "PNG")
			{
				return imageProp;
			}
			WzImageProperty wzImageProperty2 = wzImageProperty.WzProperties.FirstOrDefault((WzImageProperty p) => p.Name == segment);
			if (wzImageProperty2 == null)
			{
				return null;
			}
			wzImageProperty = wzImageProperty2;
		}
		return wzImageProperty;
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.WriteStringValue("Canvas", 115, 27);
		writer.Write((byte)0);
		if (properties.Count > 0)
		{
			writer.Write((byte)1);
			WzImageProperty.WritePropertyList(writer, properties);
		}
		else
		{
			writer.Write((byte)0);
		}
		writer.WriteCompressedInt(PngProperty.Width);
		writer.WriteCompressedInt(PngProperty.Height);
		int format = (int)PngProperty.Format;
		int value = format & 0xFF;
		int value2 = format >> 8;
		writer.WriteCompressedInt(value);
		writer.WriteCompressedInt(value2);
		writer.Write(0);
		byte[] compressedBytesForExtraction = PngProperty.GetCompressedBytesForExtraction(saveInMemory: false);
		writer.Write(compressedBytesForExtraction.Length + 1);
		writer.Write((byte)0);
		writer.Write(compressedBytesForExtraction);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzCanvas", Name, finish: false, empty: false) + XmlUtil.Attrib("width", PngProperty.Width.ToString()) + XmlUtil.Attrib("height", PngProperty.Height.ToString(), closeTag: true, empty: false));
		WzImageProperty.DumpPropertyList(writer, level, WzProperties);
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzCanvas"));
	}

	public override void Dispose()
	{
		name = null;
		imageProp.Dispose();
		imageProp = null;
		foreach (WzImageProperty property in properties)
		{
			property.Dispose();
		}
		properties.Clear();
		properties = null;
	}

	public PointF GetCanvasOriginPosition()
	{
		WzVectorProperty wzVectorProperty = (WzVectorProperty)this["origin"];
		if (wzVectorProperty != null)
		{
			return new PointF(wzVectorProperty.X.Value, wzVectorProperty.Y.Value);
		}
		return new PointF(0f, 0f);
	}

	public void SetCanvasOriginPosition(PointF pointF)
	{
		PointF canvasOriginPosition = GetCanvasOriginPosition();
		if (canvasOriginPosition.X != 0f && canvasOriginPosition.Y != 0f)
		{
			WzVectorProperty wzVectorProperty = (WzVectorProperty)this["origin"];
			wzVectorProperty.X.SetValue(pointF.X);
			wzVectorProperty.Y.SetValue(pointF.Y);
			return;
		}
		throw new Exception(string.Format("'{0}' property is not available", "origin"));
	}

	public PointF GetCanvasHeadPosition()
	{
		WzVectorProperty wzVectorProperty = (WzVectorProperty)this["head"];
		if (wzVectorProperty != null)
		{
			return new PointF(wzVectorProperty.X.Value, wzVectorProperty.Y.Value);
		}
		return new PointF(0f, 0f);
	}

	public PointF GetCanvasLtPosition()
	{
		WzVectorProperty wzVectorProperty = (WzVectorProperty)this["lt"];
		if (wzVectorProperty != null)
		{
			return new PointF(wzVectorProperty.X.Value, wzVectorProperty.Y.Value);
		}
		return new PointF(0f, 0f);
	}

	public bool ContainsInlinkProperty()
	{
		return this["_inlink"] != null;
	}

	public bool ContainsOutlinkProperty()
	{
		return this["_outlink"] != null;
	}

	public Bitmap GetLinkedWzCanvasBitmap()
	{
		return GetLinkedWzImageProperty().GetBitmap();
	}

	public new WzImageProperty GetLinkedWzImageProperty()
	{
		string text = ((WzStringProperty)this["_inlink"])?.Value;
		string text2 = ((WzStringProperty)this["_outlink"])?.Value;
		if (!string.IsNullOrEmpty(text))
		{
			for (WzObject wzObject = Parent; wzObject != null; wzObject = wzObject.Parent)
			{
				if (wzObject is WzImage wzImage)
				{
					WzImageProperty fromPath = wzImage.GetFromPath(text);
					if (fromPath != null)
					{
						return fromPath;
					}
					break;
				}
			}
			Debug.WriteLine("Could not resolve _inlink path: " + text);
		}
		else if (!string.IsNullOrEmpty(text2))
		{
			WzObject wzObject2 = Parent;
			WzFile wzFile = null;
			while (wzObject2 != null)
			{
				if (wzObject2 is WzDirectory wzDirectory)
				{
					wzFile = wzDirectory.wzFile;
					break;
				}
				wzObject2 = wzObject2.Parent;
			}
			if (wzFile != null)
			{
				Match match = Regex.Match(wzFile.Name, "^([A-Za-z]+)([0-9]*).wz");
				string text3 = match.Groups[1].Value + "/";
				WzObject objectFromPath;
				if (text2.StartsWith(text3))
				{
					string path = text2.Replace(text3, WzFileParent.Name.Replace(".wz", "") + "/");
					objectFromPath = wzFile.GetObjectFromPath(path);
				}
				else
				{
					if (WzFileManager.fileManager != null && WzFileManager.fileManager.Is64Bit)
					{
						if (WzFileManager.ContainsCanvasDirectory(text2))
						{
							string canvasFolder = WzFileManager.NormaliseWzCanvasDirectory(text2);
							WzFileManager.fileManager.LoadCanvasSection(canvasFolder, wzFile.MapleVersion);
						}
						else
						{
							Debug.WriteLine(GetLinkedWzImageProperty().Name + " has an _outlink that does not contain '" + WzFileManager.CANVAS_DIRECTORY_NAME);
						}
					}
					objectFromPath = wzFile.GetObjectFromPath(text2);
				}
				if (objectFromPath != null && objectFromPath is WzImageProperty result)
				{
					return result;
				}
			}
			Debug.WriteLine("Could not resolve _outlink path: " + text2);
		}
		return this;
	}

	public WzCanvasProperty()
	{
		properties = new WzPropertyCollection(this);
	}

	public WzCanvasProperty(string name)
	{
		this.name = name;
		properties = new WzPropertyCollection(this);
	}

	public void AddProperty(WzImageProperty prop)
	{
		prop.Parent = this;
		properties.Add(prop);
	}

	public void AddProperties(WzPropertyCollection props)
	{
		foreach (WzImageProperty prop in props)
		{
			AddProperty(prop);
		}
	}

	public void RemoveProperty(WzImageProperty prop)
	{
		prop.Parent = null;
		properties.Remove(prop);
	}

	public void RemoveProperty(string propertyName)
	{
		WzImageProperty wzImageProperty = this[propertyName];
		if (wzImageProperty != null)
		{
			RemoveProperty(wzImageProperty);
		}
	}

	public void ClearProperties()
	{
		foreach (WzImageProperty property in properties)
		{
			property.Parent = null;
		}
		properties.Clear();
	}

	public override Bitmap GetBitmap()
	{
		return imageProp.GetImage(saveInMemory: false);
	}
}
