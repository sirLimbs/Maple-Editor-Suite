using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.Serializer;

public abstract class WzSerializer : ProgressingWzSerializer
{
	protected string indent;

	protected string lineBreak;

	public static NumberFormatInfo formattingInfo;

	protected bool bExportBase64Data = false;

	protected static char[] amp;

	protected static char[] lt;

	protected static char[] gt;

	protected static char[] apos;

	protected static char[] quot;

	static WzSerializer()
	{
		amp = "&amp;".ToCharArray();
		lt = "&lt;".ToCharArray();
		gt = "&gt;".ToCharArray();
		apos = "&apos;".ToCharArray();
		quot = "&quot;".ToCharArray();
		formattingInfo = new NumberFormatInfo
		{
			NumberDecimalSeparator = ".",
			NumberGroupSeparator = ","
		};
	}

	public WzSerializer(int indentation, LineBreak lineBreakType)
	{
		switch (lineBreakType)
		{
		case LineBreak.None:
			lineBreak = "";
			break;
		case LineBreak.Windows:
			lineBreak = "\r\n";
			break;
		case LineBreak.Unix:
			lineBreak = "\n";
			break;
		}
		indent = new string(' ', indentation);
	}

	protected void WritePropertyToXML(TextWriter tw, string depth, WzImageProperty prop, string exportFilePath)
	{
		if (prop is WzCanvasProperty)
		{
			WzCanvasProperty wzCanvasProperty = (WzCanvasProperty)prop;
			if (bExportBase64Data)
			{
				MemoryStream memoryStream = new MemoryStream();
				wzCanvasProperty.PngProperty.GetImage(saveInMemory: false).Save(memoryStream, ImageFormat.Png);
				byte[] inArray = memoryStream.ToArray();
				memoryStream.Close();
				tw.Write(string.Concat(depth + "<canvas name=\"" + XmlUtil.SanitizeText(wzCanvasProperty.Name) + "\" width=\"" + wzCanvasProperty.PngProperty.Width + "\" height=\"" + wzCanvasProperty.PngProperty.Height + "\" basedata=\"" + Convert.ToBase64String(inArray) + "\">", lineBreak));
			}
			else
			{
				tw.Write(string.Concat(depth + "<canvas name=\"" + XmlUtil.SanitizeText(wzCanvasProperty.Name) + "\" width=\"" + wzCanvasProperty.PngProperty.Width + "\" height=\"" + wzCanvasProperty.PngProperty.Height + "\">", lineBreak));
			}
			string depth2 = depth + indent;
			foreach (WzImageProperty wzProperty in wzCanvasProperty.WzProperties)
			{
				WritePropertyToXML(tw, depth2, wzProperty, exportFilePath);
			}
			tw.Write(depth + "</canvas>" + lineBreak);
		}
		else if (prop is WzIntProperty)
		{
			WzIntProperty wzIntProperty = (WzIntProperty)prop;
			tw.Write(string.Concat(depth + "<int name=\"" + XmlUtil.SanitizeText(wzIntProperty.Name) + "\" value=\"" + wzIntProperty.Value + "\"/>", lineBreak));
		}
		else if (prop is WzDoubleProperty)
		{
			WzDoubleProperty wzDoubleProperty = (WzDoubleProperty)prop;
			tw.Write(string.Concat(depth + "<double name=\"" + XmlUtil.SanitizeText(wzDoubleProperty.Name) + "\" value=\"" + wzDoubleProperty.Value + "\"/>", lineBreak));
		}
		else if (prop is WzNullProperty)
		{
			WzNullProperty wzNullProperty = (WzNullProperty)prop;
			tw.Write(depth + "<null name=\"" + XmlUtil.SanitizeText(wzNullProperty.Name) + "\"/>" + lineBreak);
		}
		else if (prop is WzBinaryProperty)
		{
			WzBinaryProperty wzBinaryProperty = (WzBinaryProperty)prop;
			if (bExportBase64Data)
			{
				tw.Write(string.Concat(depth + "<sound name=\"" + XmlUtil.SanitizeText(wzBinaryProperty.Name) + "\" length=\"" + wzBinaryProperty.Length.ToString() + "\" basehead=\"" + Convert.ToBase64String(wzBinaryProperty.Header) + "\" basedata=\"" + Convert.ToBase64String(wzBinaryProperty.GetBytes(saveInMemory: false)) + "\"/>", lineBreak));
			}
			else
			{
				tw.Write(depth + "<sound name=\"" + XmlUtil.SanitizeText(wzBinaryProperty.Name) + "\"/>" + lineBreak);
			}
		}
		else if (prop is WzStringProperty)
		{
			WzStringProperty wzStringProperty = (WzStringProperty)prop;
			string text = XmlUtil.SanitizeText(wzStringProperty.Value);
			tw.Write(depth + "<string name=\"" + XmlUtil.SanitizeText(wzStringProperty.Name) + "\" value=\"" + text + "\"/>" + lineBreak);
		}
		else if (prop is WzSubProperty)
		{
			WzSubProperty wzSubProperty = (WzSubProperty)prop;
			tw.Write(depth + "<imgdir name=\"" + XmlUtil.SanitizeText(wzSubProperty.Name) + "\">" + lineBreak);
			string depth3 = depth + indent;
			foreach (WzImageProperty wzProperty2 in wzSubProperty.WzProperties)
			{
				WritePropertyToXML(tw, depth3, wzProperty2, exportFilePath);
			}
			tw.Write(depth + "</imgdir>" + lineBreak);
		}
		else if (prop is WzShortProperty)
		{
			WzShortProperty wzShortProperty = (WzShortProperty)prop;
			tw.Write(string.Concat(depth + "<short name=\"" + XmlUtil.SanitizeText(wzShortProperty.Name) + "\" value=\"" + wzShortProperty.Value + "\"/>", lineBreak));
		}
		else if (prop is WzLongProperty)
		{
			WzLongProperty wzLongProperty = (WzLongProperty)prop;
			tw.Write(string.Concat(depth + "<long name=\"" + XmlUtil.SanitizeText(wzLongProperty.Name) + "\" value=\"" + wzLongProperty.Value + "\"/>", lineBreak));
		}
		else if (prop is WzUOLProperty)
		{
			WzUOLProperty wzUOLProperty = (WzUOLProperty)prop;
			tw.Write(depth + "<uol name=\"" + wzUOLProperty.Name + "\" value=\"" + XmlUtil.SanitizeText(wzUOLProperty.Value) + "\"/>" + lineBreak);
		}
		else if (prop is WzVectorProperty)
		{
			WzVectorProperty wzVectorProperty = (WzVectorProperty)prop;
			tw.Write(string.Concat(depth + "<vector name=\"" + XmlUtil.SanitizeText(wzVectorProperty.Name) + "\" x=\"" + wzVectorProperty.X.Value + "\" y=\"" + wzVectorProperty.Y.Value + "\"/>", lineBreak));
		}
		else if (prop is WzFloatProperty)
		{
			WzFloatProperty wzFloatProperty = (WzFloatProperty)prop;
			string text2 = Convert.ToString(wzFloatProperty.Value, formattingInfo);
			if (!text2.Contains("."))
			{
				text2 += ".0";
			}
			tw.Write(depth + "<float name=\"" + XmlUtil.SanitizeText(wzFloatProperty.Name) + "\" value=\"" + text2 + "\"/>" + lineBreak);
		}
		else if (prop is WzConvexProperty)
		{
			tw.Write(depth + "<extended name=\"" + XmlUtil.SanitizeText(prop.Name) + "\">" + lineBreak);
			WzConvexProperty wzConvexProperty = (WzConvexProperty)prop;
			string depth4 = depth + indent;
			foreach (WzImageProperty wzProperty3 in wzConvexProperty.WzProperties)
			{
				WritePropertyToXML(tw, depth4, wzProperty3, exportFilePath);
			}
			tw.Write(depth + "</extended>" + lineBreak);
		}
		else if (prop is WzLuaProperty wzLuaProperty)
		{
			string name = wzLuaProperty.Parent.Name;
			tw.Write(depth);
			tw.Write(lineBreak);
			if (bExportBase64Data)
			{
			}
			using TextWriter textWriter = new StreamWriter(File.Create(exportFilePath.Replace(name + ".xml", name)));
			textWriter.Write(wzLuaProperty.ToString());
		}
	}

	protected void WritePropertyToJsonBson(Dictionary<string, object> json, WzImageProperty prop, string exportFilePath)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>
		{
			{ "_dirName", prop.Name },
			{
				"_dirType",
				prop.PropertyType.ToString()
			}
		};
		if (!(prop is WzCanvasProperty wzCanvasProperty))
		{
			if (!(prop is WzIntProperty wzIntProperty))
			{
				if (!(prop is WzDoubleProperty wzDoubleProperty))
				{
					if (!(prop is WzNullProperty))
					{
						if (!(prop is WzBinaryProperty wzBinaryProperty))
						{
							if (!(prop is WzStringProperty wzStringProperty))
							{
								if (!(prop is WzSubProperty wzSubProperty))
								{
									if (!(prop is WzShortProperty wzShortProperty))
									{
										if (!(prop is WzLongProperty wzLongProperty))
										{
											if (!(prop is WzUOLProperty wzUOLProperty))
											{
												if (!(prop is WzVectorProperty wzVectorProperty))
												{
													if (!(prop is WzFloatProperty wzFloatProperty))
													{
														if (!(prop is WzConvexProperty wzConvexProperty))
														{
															if (prop is WzLuaProperty wzLuaProperty)
															{
																dictionary["_dirType"] = "lua";
																dictionary["_fileName"] = wzLuaProperty.Parent.Name;
																if (bExportBase64Data && !dictionary.ContainsKey("_image"))
																{
																	dictionary["_image"] = wzLuaProperty.ToString();
																}
															}
															else
															{
																dictionary["_value"] = prop.ToString();
															}
														}
														else
														{
															dictionary["_dirType"] = "convex";
															foreach (WzImageProperty wzProperty in wzConvexProperty.WzProperties)
															{
																WritePropertyToJsonBson(dictionary, wzProperty, exportFilePath);
															}
														}
													}
													else
													{
														dictionary["_value"] = wzFloatProperty.Value;
													}
												}
												else
												{
													dictionary["_dirType"] = "vector";
													dictionary["_x"] = wzVectorProperty.X.Value;
													dictionary["_y"] = wzVectorProperty.Y.Value;
												}
											}
											else
											{
												dictionary["_dirType"] = "uol";
												dictionary["_value"] = wzUOLProperty.Value;
											}
										}
										else
										{
											dictionary["_value"] = wzLongProperty.Value;
										}
									}
									else
									{
										dictionary["_value"] = wzShortProperty.Value;
									}
								}
								else
								{
									dictionary["_dirType"] = "sub";
									foreach (WzImageProperty wzProperty2 in wzSubProperty.WzProperties)
									{
										WritePropertyToJsonBson(dictionary, wzProperty2, exportFilePath);
									}
								}
							}
							else
							{
								dictionary["_value"] = wzStringProperty.Value;
							}
						}
						else
						{
							dictionary["_dirType"] = "binary";
							dictionary["_length"] = wzBinaryProperty.Length.ToString();
							if (bExportBase64Data && !dictionary.ContainsKey("basehead") && !dictionary.ContainsKey("basedata"))
							{
								dictionary["basehead"] = Convert.ToBase64String(wzBinaryProperty.Header);
								dictionary["basedata"] = Convert.ToBase64String(wzBinaryProperty.GetBytes(saveInMemory: false));
							}
						}
					}
				}
				else
				{
					dictionary["_value"] = wzDoubleProperty.Value;
				}
			}
			else
			{
				dictionary["_value"] = wzIntProperty.Value;
			}
		}
		else
		{
			dictionary["_dirType"] = "canvas";
			dictionary["_width"] = wzCanvasProperty.PngProperty.Width;
			dictionary["_height"] = wzCanvasProperty.PngProperty.Height;
			if (bExportBase64Data && !dictionary.ContainsKey("_image"))
			{
				using MemoryStream memoryStream = new MemoryStream();
				wzCanvasProperty.PngProperty.GetImage(saveInMemory: false)?.Save(memoryStream, ImageFormat.Png);
				dictionary["_image"] = Convert.ToBase64String(memoryStream.ToArray());
			}
			foreach (WzImageProperty wzProperty3 in wzCanvasProperty.WzProperties)
			{
				WritePropertyToJsonBson(dictionary, wzProperty3, exportFilePath);
			}
		}
		string name = prop.Name;
		if (!json.ContainsKey(name))
		{
			json[name] = dictionary;
		}
	}
}
