using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

namespace MapleImgEditor;

internal sealed class ImgXmlDocumentService
{
	private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

	public WzImage LoadFromXml(string path, out bool hasCanvasPlaceholders)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(path);
		XmlElement documentElement = xmlDocument.DocumentElement;
		if (documentElement == null || documentElement.Name != "imgdir")
		{
			throw new InvalidOperationException("Expected an imgdir root element.");
		}
		WzImage wzImage = new WzImage(documentElement.GetAttribute("name"));
		hasCanvasPlaceholders = false;
		foreach (XmlNode childNode in documentElement.ChildNodes)
		{
			if (childNode is XmlElement element)
			{
				wzImage.AddProperty(ParseProperty(element, ref hasCanvasPlaceholders));
			}
		}
		wzImage.Changed = true;
		wzImage.MarkWzImageAsParsed();
		return wzImage;
	}

	public void SaveToXml(WzImage image, string path)
	{
		XmlWriterSettings settings = new XmlWriterSettings
		{
			Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
			Indent = true,
			OmitXmlDeclaration = false
		};
		using XmlWriter xmlWriter = XmlWriter.Create(path, settings);
		xmlWriter.WriteStartDocument(standalone: true);
		xmlWriter.WriteStartElement("imgdir");
		xmlWriter.WriteAttributeString("name", image.Name);
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			WriteProperty(xmlWriter, wzProperty);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
	}

	private static WzImageProperty ParseProperty(XmlElement element, ref bool hasCanvasPlaceholders)
	{
		string name = element.Name;
		if (1 == 0)
		{
		}
		WzImageProperty result = name switch
		{
			"subprop" => (WzImageProperty)ParseSubProperty(element, ref hasCanvasPlaceholders), 
			"imgdir" => ParseSubProperty(element, ref hasCanvasPlaceholders), 
			"canvas" => ParseCanvasProperty(element, ref hasCanvasPlaceholders), 
			"vector" => ParseVectorProperty(element), 
			"string" => new WzStringProperty(element.GetAttribute("name"), element.GetAttribute("value")), 
			"uol" => new WzUOLProperty(element.GetAttribute("name"), element.GetAttribute("value")), 
			"int" => new WzIntProperty(element.GetAttribute("name"), ParseInt(element, "value")), 
			"short" => new WzShortProperty(element.GetAttribute("name"), short.Parse(element.GetAttribute("value"), Culture)), 
			"long" => new WzLongProperty(element.GetAttribute("name"), long.Parse(element.GetAttribute("value"), Culture)), 
			"float" => new WzFloatProperty(element.GetAttribute("name"), float.Parse(element.GetAttribute("value"), Culture)), 
			"double" => new WzDoubleProperty(element.GetAttribute("name"), double.Parse(element.GetAttribute("value"), Culture)), 
			"null" => new WzNullProperty(element.GetAttribute("name")), 
			"extended" => ParseExtendedProperty(element, ref hasCanvasPlaceholders), 
			_ => throw new InvalidOperationException("Unsupported XML node '" + element.Name + "'."), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static WzSubProperty ParseSubProperty(XmlElement element, ref bool hasCanvasPlaceholders)
	{
		WzSubProperty wzSubProperty = new WzSubProperty(element.GetAttribute("name"));
		foreach (XmlNode childNode in element.ChildNodes)
		{
			if (childNode is XmlElement element2)
			{
				wzSubProperty.AddProperty(ParseProperty(element2, ref hasCanvasPlaceholders));
			}
		}
		return wzSubProperty;
	}

	private static WzConvexProperty ParseExtendedProperty(XmlElement element, ref bool hasCanvasPlaceholders)
	{
		WzConvexProperty wzConvexProperty = new WzConvexProperty(element.GetAttribute("name"));
		foreach (XmlNode childNode in element.ChildNodes)
		{
			if (childNode is XmlElement element2)
			{
				wzConvexProperty.AddProperty(ParseProperty(element2, ref hasCanvasPlaceholders));
			}
		}
		return wzConvexProperty;
	}

	private static WzCanvasProperty ParseCanvasProperty(XmlElement element, ref bool hasCanvasPlaceholders)
	{
		string attribute = element.GetAttribute("name");
		int num = ParseInt(element, "width");
		int num2 = ParseInt(element, "height");
		WzCanvasProperty wzCanvasProperty = new WzCanvasProperty(attribute)
		{
			PngProperty = new WzPngProperty()
		};
		string attribute2 = element.GetAttribute("basedata");
		string attribute3 = element.GetAttribute("bytedata");
		bool flag = !string.IsNullOrWhiteSpace(attribute2);
		bool flag2 = !string.IsNullOrWhiteSpace(attribute3);
		if (flag)
		{
			using MemoryStream stream = new MemoryStream(Convert.FromBase64String(attribute2));
			using Bitmap bitmap = new Bitmap(stream);
			wzCanvasProperty.PngProperty.PNG = (Bitmap)bitmap.Clone();
		}
		else if (flag2)
		{
			WzPngFormat format = (WzPngFormat)ParseInt(element, "pixFormat");
			byte[] bytes = Convert.FromBase64String(attribute3);
			wzCanvasProperty.PngProperty.SetCompressedBytes(bytes, num, num2, format);
		}
		else
		{
			hasCanvasPlaceholders = true;
			using Bitmap bitmap2 = new Bitmap(Math.Max(num, 1), Math.Max(num2, 1));
			using Graphics graphics = Graphics.FromImage(bitmap2);
			graphics.Clear(Color.Transparent);
			wzCanvasProperty.PngProperty.PNG = (Bitmap)bitmap2.Clone();
		}
		wzCanvasProperty.HRTag = new CanvasMetadata(flag, flag2);
		foreach (XmlNode childNode in element.ChildNodes)
		{
			if (childNode is XmlElement element2)
			{
				wzCanvasProperty.AddProperty(ParseProperty(element2, ref hasCanvasPlaceholders));
			}
		}
		return wzCanvasProperty;
	}

	private static WzVectorProperty ParseVectorProperty(XmlElement element)
	{
		string attribute = element.GetAttribute("name");
		int value = ParseInt(element, "x");
		int value2 = ParseInt(element, "y");
		return new WzVectorProperty(attribute, new WzIntProperty("x", value), new WzIntProperty("y", value2));
	}

	private static int ParseInt(XmlElement element, string attributeName)
	{
		string attribute = element.GetAttribute(attributeName);
		return int.Parse(attribute, Culture);
	}

	private static void WriteProperty(XmlWriter writer, WzImageProperty property)
	{
		if (!(property is WzSubProperty wzSubProperty))
		{
			if (!(property is WzCanvasProperty wzCanvasProperty))
			{
				if (!(property is WzVectorProperty wzVectorProperty))
				{
					if (!(property is WzStringProperty wzStringProperty))
					{
						if (!(property is WzUOLProperty wzUOLProperty))
						{
							if (!(property is WzIntProperty wzIntProperty))
							{
								if (!(property is WzShortProperty wzShortProperty))
								{
									if (!(property is WzLongProperty wzLongProperty))
									{
										if (!(property is WzFloatProperty wzFloatProperty))
										{
											if (!(property is WzDoubleProperty wzDoubleProperty))
											{
												if (!(property is WzNullProperty wzNullProperty))
												{
													if (!(property is WzConvexProperty wzConvexProperty))
													{
														throw new InvalidOperationException("Unsupported property type '" + property.GetType().Name + "'.");
													}
													writer.WriteStartElement("extended");
													writer.WriteAttributeString("name", wzConvexProperty.Name);
													foreach (WzImageProperty wzProperty in wzConvexProperty.WzProperties)
													{
														WriteProperty(writer, wzProperty);
													}
													writer.WriteEndElement();
												}
												else
												{
													writer.WriteStartElement("null");
													writer.WriteAttributeString("name", wzNullProperty.Name);
													writer.WriteEndElement();
												}
											}
											else
											{
												WriteValueProperty(writer, "double", wzDoubleProperty.Name, wzDoubleProperty.Value.ToString(Culture));
											}
										}
										else
										{
											WriteValueProperty(writer, "float", wzFloatProperty.Name, wzFloatProperty.Value.ToString(Culture));
										}
									}
									else
									{
										WriteValueProperty(writer, "long", wzLongProperty.Name, wzLongProperty.Value.ToString(Culture));
									}
								}
								else
								{
									WriteValueProperty(writer, "short", wzShortProperty.Name, wzShortProperty.Value.ToString(Culture));
								}
							}
							else
							{
								WriteValueProperty(writer, "int", wzIntProperty.Name, wzIntProperty.Value.ToString(Culture));
							}
						}
						else
						{
							WriteValueProperty(writer, "uol", wzUOLProperty.Name, wzUOLProperty.Value);
						}
					}
					else
					{
						WriteValueProperty(writer, "string", wzStringProperty.Name, wzStringProperty.Value);
					}
				}
				else
				{
					writer.WriteStartElement("vector");
					writer.WriteAttributeString("name", wzVectorProperty.Name);
					writer.WriteAttributeString("x", wzVectorProperty.X.Value.ToString(Culture));
					writer.WriteAttributeString("y", wzVectorProperty.Y.Value.ToString(Culture));
					writer.WriteEndElement();
				}
				return;
			}
			writer.WriteStartElement("canvas");
			writer.WriteAttributeString("name", wzCanvasProperty.Name);
			writer.WriteAttributeString("width", wzCanvasProperty.PngProperty.Width.ToString(Culture));
			writer.WriteAttributeString("height", wzCanvasProperty.PngProperty.Height.ToString(Culture));
			CanvasMetadata canvasMetadata = (wzCanvasProperty.HRTag as CanvasMetadata) ?? new CanvasMetadata(HasEmbeddedBitmapData: true, HasCompressedBitmapData: false);
			if (canvasMetadata.HasCompressedBitmapData)
			{
				writer.WriteAttributeString("pixFormat", ((int)wzCanvasProperty.PngProperty.Format).ToString(Culture));
				writer.WriteAttributeString("magLevel", "0");
				writer.WriteAttributeString("bytedata", Convert.ToBase64String(wzCanvasProperty.PngProperty.GetCompressedBytesForExtraction(saveInMemory: true)));
			}
			else if (canvasMetadata.HasEmbeddedBitmapData)
			{
				using Bitmap bitmap = wzCanvasProperty.PngProperty.GetImage(saveInMemory: false);
				using MemoryStream memoryStream = new MemoryStream();
				bitmap.Save(memoryStream, ImageFormat.Png);
				writer.WriteAttributeString("basedata", Convert.ToBase64String(memoryStream.ToArray()));
			}
			foreach (WzImageProperty wzProperty2 in wzCanvasProperty.WzProperties)
			{
				WriteProperty(writer, wzProperty2);
			}
			writer.WriteEndElement();
			return;
		}
		writer.WriteStartElement("subprop");
		writer.WriteAttributeString("name", wzSubProperty.Name);
		foreach (WzImageProperty wzProperty3 in wzSubProperty.WzProperties)
		{
			WriteProperty(writer, wzProperty3);
		}
		writer.WriteEndElement();
	}

	private static void WriteValueProperty(XmlWriter writer, string elementName, string name, string value)
	{
		writer.WriteStartElement(elementName);
		writer.WriteAttributeString("name", name);
		writer.WriteAttributeString("value", value);
		writer.WriteEndElement();
	}
}
