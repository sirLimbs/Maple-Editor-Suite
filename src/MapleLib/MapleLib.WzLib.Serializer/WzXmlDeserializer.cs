using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.Serializer;

public class WzXmlDeserializer : ProgressingWzSerializer
{
	public static NumberFormatInfo formattingInfo;

	private readonly bool useMemorySaving;

	private readonly byte[] iv;

	private readonly WzImgDeserializer imgDeserializer = new WzImgDeserializer(freeResources: false);

	public WzXmlDeserializer(bool useMemorySaving, byte[] iv)
	{
		this.useMemorySaving = useMemorySaving;
		this.iv = iv;
	}

	public List<WzObject> ParseXML(string path)
	{
		List<WzObject> list = new List<WzObject>();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(path);
		XmlElement xmlElement = (XmlElement)xmlDocument.ChildNodes[1];
		curr = 0;
		if (xmlElement.Name == "xmldump")
		{
			total = CountImgs(xmlElement);
			foreach (XmlElement item in xmlElement)
			{
				if (item.Name == "wzdir")
				{
					list.Add(ParseXMLWzDir(item));
					continue;
				}
				if (item.Name == "wzimg")
				{
					list.Add(ParseXMLWzImg(item));
					continue;
				}
				throw new InvalidDataException("unknown XML prop " + item.Name);
			}
		}
		else
		{
			if (!(xmlElement.Name == "imgdir"))
			{
				throw new InvalidDataException("unknown main XML prop " + xmlElement.Name);
			}
			total = 1;
			list.Add(ParseXMLWzImg(xmlElement));
			curr++;
		}
		return list;
	}

	internal int CountImgs(XmlElement element)
	{
		int num = element.Cast<XmlElement>().Count((XmlElement e) => e.Name == "wzimg");
		int num2 = (from XmlElement e in element
			where e.Name == "wzdir"
			select e).Sum((XmlElement e) => CountImgs(e));
		return num + num2;
	}

	internal WzDirectory ParseXMLWzDir(XmlElement dirElement)
	{
		WzDirectory wzDirectory = new WzDirectory(dirElement.GetAttribute("name"));
		foreach (XmlElement item in dirElement)
		{
			if (item.Name == "wzdir")
			{
				wzDirectory.AddDirectory(ParseXMLWzDir(item));
				continue;
			}
			if (item.Name == "wzimg")
			{
				wzDirectory.AddImage(ParseXMLWzImg(item));
				continue;
			}
			throw new InvalidDataException("unknown XML prop " + item.Name);
		}
		return wzDirectory;
	}

	internal WzImage ParseXMLWzImg(XmlElement imgElement)
	{
		string attribute = imgElement.GetAttribute("name");
		WzImage wzImage = new WzImage(attribute);
		foreach (XmlElement item in imgElement)
		{
			wzImage.WzProperties.Add(ParsePropertyFromXMLElement(item));
		}
		wzImage.Changed = true;
		if (useMemorySaving)
		{
			string tempFileName = Path.GetTempFileName();
			try
			{
				using (WzBinaryWriter writer = new WzBinaryWriter(File.Create(tempFileName), iv))
				{
					wzImage.SaveImage(writer);
					wzImage.Dispose();
				}
				wzImage = imgDeserializer.WzImageFromIMGFile(tempFileName, iv, attribute, out var _);
			}
			finally
			{
				if (wzImage.ParseEverything)
				{
					File.Delete(tempFileName);
				}
			}
		}
		return wzImage;
	}

	internal WzImageProperty ParsePropertyFromXMLElement(XmlElement element)
	{
		switch (element.Name)
		{
		case "imgdir":
		{
			WzSubProperty wzSubProperty = new WzSubProperty(element.GetAttribute("name"));
			foreach (XmlElement item in element)
			{
				wzSubProperty.AddProperty(ParsePropertyFromXMLElement(item));
			}
			return wzSubProperty;
		}
		case "canvas":
		{
			WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(element.GetAttribute("name"));
			if (!element.HasAttribute("basedata"))
			{
				throw new NoBase64DataException("no base64 data in canvas element with name " + wzCanvasProperty.Name);
			}
			wzCanvasProperty.PngProperty = new WzPngProperty();
			MemoryStream stream = new MemoryStream(Convert.FromBase64String(element.GetAttribute("basedata")));
			wzCanvasProperty.PngProperty.PNG = (Bitmap)Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
			foreach (XmlElement item2 in element)
			{
				wzCanvasProperty.AddProperty(ParsePropertyFromXMLElement(item2));
			}
			return wzCanvasProperty;
		}
		case "int":
			return new WzIntProperty(element.GetAttribute("name"), int.Parse(element.GetAttribute("value"), formattingInfo));
		case "double":
			return new WzDoubleProperty(element.GetAttribute("name"), double.Parse(element.GetAttribute("value"), formattingInfo));
		case "null":
			return new WzNullProperty(element.GetAttribute("name"));
		case "sound":
			if (!element.HasAttribute("basedata") || !element.HasAttribute("basehead") || !element.HasAttribute("length"))
			{
				throw new NoBase64DataException("no base64 data in sound element with name " + element.GetAttribute("name"));
			}
			return new WzBinaryProperty(element.GetAttribute("name"), int.Parse(element.GetAttribute("length")), Convert.FromBase64String(element.GetAttribute("basehead")), Convert.FromBase64String(element.GetAttribute("basedata")));
		case "string":
			return new WzStringProperty(element.GetAttribute("name"), element.GetAttribute("value"));
		case "short":
			return new WzShortProperty(element.GetAttribute("name"), short.Parse(element.GetAttribute("value"), formattingInfo));
		case "long":
			return new WzLongProperty(element.GetAttribute("name"), long.Parse(element.GetAttribute("value"), formattingInfo));
		case "uol":
			return new WzUOLProperty(element.GetAttribute("name"), element.GetAttribute("value"));
		case "vector":
			return new WzVectorProperty(element.GetAttribute("name"), new WzIntProperty("x", Convert.ToInt32(element.GetAttribute("x"))), new WzIntProperty("y", Convert.ToInt32(element.GetAttribute("y"))));
		case "float":
			return new WzFloatProperty(element.GetAttribute("name"), float.Parse(element.GetAttribute("value"), formattingInfo));
		case "extended":
		{
			WzConvexProperty wzConvexProperty = new WzConvexProperty(element.GetAttribute("name"));
			foreach (XmlElement item3 in element)
			{
				wzConvexProperty.AddProperty(ParsePropertyFromXMLElement(item3));
			}
			return wzConvexProperty;
		}
		default:
			throw new InvalidDataException("unknown XML prop " + element.Name);
		}
	}
}
