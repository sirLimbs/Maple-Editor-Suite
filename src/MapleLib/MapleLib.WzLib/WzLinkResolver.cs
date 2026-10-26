#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib;

public class WzLinkResolver
{
	private List<WzFile> _categoryWzFiles = new List<WzFile>();

	private string _categoryName;

	private List<WzImage> _currentCanvasImages = new List<WzImage>();

	public int LinksResolved { get; private set; }

	public int LinksFailed { get; private set; }

	public List<string> FailedLinks { get; } = new List<string>();

	public void SetCategoryWzFiles(IEnumerable<WzFile> wzFiles, string categoryName)
	{
		_categoryWzFiles = wzFiles?.ToList() ?? new List<WzFile>();
		_categoryName = categoryName;
	}

	public static bool ResolveSingleCanvas(WzCanvasProperty canvas, bool inlinkOnly = false)
	{
		if (canvas == null)
		{
			return false;
		}
		bool flag = canvas.ContainsInlinkProperty();
		bool flag2 = canvas.ContainsOutlinkProperty();
		if (!flag && !flag2)
		{
			return false;
		}
		if (inlinkOnly && !flag)
		{
			return false;
		}
		try
		{
			WzImageProperty linkedWzImageProperty = canvas.GetLinkedWzImageProperty();
			if (linkedWzImageProperty != null && linkedWzImageProperty != canvas && linkedWzImageProperty is WzCanvasProperty srcCanvas)
			{
				return CopyCanvasData(canvas, srcCanvas, flag, flag2);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[WzLinkResolver] Exception resolving canvas link: " + ex.Message);
		}
		return false;
	}

	public int ResolveLinksInImage(WzImage image)
	{
		if (image == null)
		{
			return 0;
		}
		if (!image.Parsed)
		{
			image.ParseImage();
		}
		int num = 0;
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			num += ResolveLinksInProperty(wzProperty, image.FullPath ?? image.Name);
		}
		if (num > 0)
		{
			image.Changed = true;
		}
		return num;
	}

	public void Reset()
	{
		LinksResolved = 0;
		LinksFailed = 0;
		FailedLinks.Clear();
	}

	private static bool CopyCanvasData(WzCanvasProperty destCanvas, WzCanvasProperty srcCanvas, bool hasInlink, bool hasOutlink)
	{
		WzPngProperty pngProperty = srcCanvas.PngProperty;
		WzPngProperty pngProperty2 = destCanvas.PngProperty;
		if (pngProperty != null && pngProperty2 != null)
		{
			byte[] compressedBytesForExtraction = pngProperty.GetCompressedBytesForExtraction(saveInMemory: false);
			pngProperty2.SetCompressedBytes(compressedBytesForExtraction, pngProperty.Width, pngProperty.Height, pngProperty.Format);
		}
		if (hasInlink)
		{
			destCanvas.RemoveProperty("_inlink");
		}
		if (hasOutlink)
		{
			destCanvas.RemoveProperty("_outlink");
		}
		return true;
	}

	private int ResolveLinksInProperty(WzImageProperty property, string parentPath)
	{
		if (property == null)
		{
			return 0;
		}
		switch (property.PropertyType)
		{
		case WzPropertyType.Null:
		case WzPropertyType.Short:
		case WzPropertyType.Int:
		case WzPropertyType.Long:
		case WzPropertyType.Float:
		case WzPropertyType.Double:
		case WzPropertyType.String:
		case WzPropertyType.Sound:
		case WzPropertyType.UOL:
		case WzPropertyType.PNG:
			return 0;
		default:
		{
			int num = 0;
			string text = parentPath + "/" + property.Name;
			if (property is WzCanvasProperty canvas && TryResolveCanvasLink(canvas, text))
			{
				num++;
			}
			WzPropertyCollection wzProperties = property.WzProperties;
			if (wzProperties != null && wzProperties.Count > 0)
			{
				foreach (WzImageProperty item in wzProperties)
				{
					num += ResolveLinksInProperty(item, text);
				}
			}
			return num;
		}
		}
	}

	private bool TryResolveCanvasLink(WzCanvasProperty canvas, string path)
	{
		bool flag = canvas.ContainsInlinkProperty();
		bool flag2 = canvas.ContainsOutlinkProperty();
		if (!flag && !flag2)
		{
			return false;
		}
		string value = (flag ? "_inlink" : "_outlink");
		string text = ((!flag) ? (((WzStringProperty)canvas["_outlink"])?.Value ?? "unknown") : (((WzStringProperty)canvas["_inlink"])?.Value ?? "unknown"));
		if (flag)
		{
			if (ResolveSingleCanvas(canvas, inlinkOnly: true))
			{
				LinksResolved++;
				return true;
			}
			LinksFailed++;
			FailedLinks.Add($"{path} ({value}: {text})");
			Debug.WriteLine($"[WzLinkResolver] Failed to resolve {value} at {path} -> {text}");
			return false;
		}
		if (flag2)
		{
			if (TryResolveOutlink(canvas, text, path))
			{
				LinksResolved++;
				return true;
			}
			LinksFailed++;
			FailedLinks.Add($"{path} ({value}: {text})");
			Debug.WriteLine($"[WzLinkResolver] Failed to resolve {value} at {path} -> {text}");
			return false;
		}
		return false;
	}

	private bool TryResolveOutlink(WzCanvasProperty canvas, string outlinkPath, string logPath)
	{
		if (string.IsNullOrEmpty(outlinkPath) || _categoryWzFiles == null || _categoryWzFiles.Count == 0)
		{
			return false;
		}
		try
		{
			string[] array = outlinkPath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length < 2)
			{
				return false;
			}
			string text = array[0];
			if (!string.Equals(text, _categoryName, StringComparison.OrdinalIgnoreCase))
			{
				Debug.WriteLine($"[WzLinkResolver] Outlink to different category '{text}' cannot be resolved (current: {_categoryName})");
				return false;
			}
			bool flag = outlinkPath.Contains("/_Canvas/", StringComparison.OrdinalIgnoreCase) || outlinkPath.Contains("_Canvas/", StringComparison.OrdinalIgnoreCase);
			int num = -1;
			for (int i = 1; i < array.Length; i++)
			{
				if (array[i].EndsWith(".img", StringComparison.OrdinalIgnoreCase))
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				return false;
			}
			string text2 = ((num > 1) ? string.Join("/", array.Skip(1).Take(num - 1)) : null);
			string text3 = array[num];
			string text4 = ((num + 1 < array.Length) ? string.Join("/", array.Skip(num + 1)) : null);
			WzImage wzImage = null;
			if (flag)
			{
				string canvasFolderPath = NormalizePath(text2);
				string text5 = "/_Canvas/";
				int num2 = outlinkPath.IndexOf(text5, StringComparison.OrdinalIgnoreCase);
				string text6 = ((num2 >= 0) ? outlinkPath.Substring(num2 + text5.Length) : null);
				if (!string.IsNullOrEmpty(text6))
				{
					string[] array2 = text6.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length >= 1)
					{
						string text7 = array2[0];
						if (text7.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
						{
							text3 = text7;
							text4 = ((array2.Length > 1) ? string.Join("/", array2.Skip(1)) : null);
						}
					}
				}
				List<WzImage> list = new List<WzImage>();
				foreach (WzFile categoryWzFile in _categoryWzFiles)
				{
					string filePath = categoryWzFile.FilePath;
					if (filePath == null || !filePath.Contains("_Canvas", StringComparison.OrdinalIgnoreCase))
					{
						string name = categoryWzFile.Name;
						if (name == null || !name.Contains("_Canvas", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
					}
					if (categoryWzFile.WzDirectory == null)
					{
						continue;
					}
					string wzFilePath = NormalizePath(categoryWzFile.FilePath ?? categoryWzFile.Name);
					if (!MatchesCanvasFolder(wzFilePath, _categoryName, canvasFolderPath))
					{
						continue;
					}
					WzImage wzImage2 = FindImageInDirectory(categoryWzFile.WzDirectory, text3);
					if (wzImage2 != null)
					{
						list.Add(wzImage2);
					}
					if (categoryWzFile.WzDirectory.WzDirectories == null)
					{
						continue;
					}
					foreach (WzDirectory wzDirectory in categoryWzFile.WzDirectory.WzDirectories)
					{
						if (wzDirectory != null)
						{
							wzImage2 = FindImageInDirectory(wzDirectory, text3);
							if (wzImage2 != null)
							{
								list.Add(wzImage2);
							}
						}
					}
				}
				wzImage = list.FirstOrDefault();
				_currentCanvasImages = list;
			}
			else
			{
				foreach (WzFile categoryWzFile2 in _categoryWzFiles)
				{
					wzImage = FindImageInDirectory(categoryWzFile2.WzDirectory, text3, text2);
					if (wzImage != null)
					{
						break;
					}
				}
			}
			if (wzImage == null)
			{
				string value = (string.IsNullOrEmpty(text2) ? text3 : (text2 + "/" + text3));
				Debug.WriteLine($"[WzLinkResolver] Could not find image '{value}' in any loaded WZ file (isCanvas: {flag})");
				return false;
			}
			if (!wzImage.Parsed)
			{
				wzImage.ParseImage();
			}
			WzImageProperty wzImageProperty = null;
			List<WzImage> list2;
			if (flag && _currentCanvasImages.Count > 0)
			{
				list2 = _currentCanvasImages;
			}
			else
			{
				list2 = ((wzImage != null) ? new List<WzImage> { wzImage } : new List<WzImage>());
			}
			foreach (WzImage item in list2)
			{
				if (item == null)
				{
					continue;
				}
				if (!item.Parsed)
				{
					item.ParseImage();
				}
				if (!string.IsNullOrEmpty(text4))
				{
					wzImageProperty = item.GetFromPath(text4);
					if ((wzImageProperty == null) & flag)
					{
						string[] array3 = text4.Split('/');
						string text8 = array3[^1];
						if (wzImageProperty == null && array3.Length > 2)
						{
							string text9 = string.Join("/", array3.Skip(2));
							string text10 = array3[1];
							List<string> list3 = new List<string> { text9 };
							if (text9.Contains("Segment"))
							{
								string text11 = Regex.Replace(text9, "Segment\\d+", "Segment0");
								if (text11 != text9)
								{
									list3.Add(text11);
								}
								string text12 = Regex.Replace(text9, "Segment[^/]+", "Segment:All");
								if (text12 != text9)
								{
									list3.Add(text12);
								}
							}
							WzImageProperty wzImageProperty2 = item["Anims"];
							if (wzImageProperty2 != null && wzImageProperty2.WzProperties != null)
							{
								foreach (WzImageProperty wzProperty in wzImageProperty2.WzProperties)
								{
									if (wzProperty.WzProperties == null)
									{
										continue;
									}
									foreach (WzImageProperty wzProperty2 in wzProperty.WzProperties)
									{
										foreach (string item2 in list3)
										{
											wzImageProperty = wzProperty2.GetFromPath(item2);
											if (wzImageProperty is WzCanvasProperty)
											{
												break;
											}
											wzImageProperty = null;
										}
										if (wzImageProperty != null)
										{
											break;
										}
									}
									if (wzImageProperty == null)
									{
										continue;
									}
									break;
								}
							}
						}
						if (wzImageProperty == null && int.TryParse(text8, out var _))
						{
							WzImageProperty wzImageProperty3 = item["Anims"];
							if (wzImageProperty3 != null && wzImageProperty3.WzProperties != null)
							{
								foreach (WzImageProperty wzProperty3 in wzImageProperty3.WzProperties)
								{
									if (wzProperty3.WzProperties == null)
									{
										continue;
									}
									foreach (WzImageProperty wzProperty4 in wzProperty3.WzProperties)
									{
										WzImageProperty wzImageProperty4 = wzProperty4["LayerSlots"]?["Slot0"];
										if (wzImageProperty4?.WzProperties != null)
										{
											foreach (WzImageProperty wzProperty5 in wzImageProperty4.WzProperties)
											{
												WzImageProperty wzImageProperty5 = wzProperty5["AnimReference"];
												if (wzImageProperty5?.WzProperties != null)
												{
													WzImageProperty wzImageProperty6 = wzImageProperty5[text8];
													if (wzImageProperty6 is WzCanvasProperty)
													{
														wzImageProperty = wzImageProperty6;
														break;
													}
												}
											}
										}
										if (wzImageProperty != null)
										{
											break;
										}
									}
									if (wzImageProperty == null)
									{
										continue;
									}
									break;
								}
							}
						}
						if (wzImageProperty == null)
						{
							wzImageProperty = item[text8];
						}
						if (wzImageProperty == null)
						{
							int num3 = array3.Length - 2;
							while (num3 >= 0 && wzImageProperty == null)
							{
								string path = string.Join("/", array3.Skip(num3));
								wzImageProperty = item.GetFromPath(path);
								num3--;
							}
						}
						if (wzImageProperty == null)
						{
							wzImageProperty = FindCanvasInImage(item, text8);
						}
						if (wzImageProperty == null && int.TryParse(text8, out var result2))
						{
							WzCanvasProperty wzCanvasProperty = FindCanvasByIndex(item, result2);
							if (wzCanvasProperty != null)
							{
								wzImageProperty = wzCanvasProperty;
							}
						}
						if (wzImageProperty == null && array3.Length >= 2)
						{
							string parentName = array3[^2];
							wzImageProperty = FindCanvasWithParent(item, parentName, text8);
						}
						if (wzImageProperty == null && int.TryParse(text8, out var result3))
						{
							List<WzCanvasProperty> allCanvasesInImage = GetAllCanvasesInImage(item);
							if (result3 < allCanvasesInImage.Count)
							{
								wzImageProperty = allCanvasesInImage[result3];
							}
						}
					}
				}
				if (!(wzImageProperty is WzCanvasProperty))
				{
					continue;
				}
				break;
			}
			if (string.IsNullOrEmpty(text4))
			{
				Debug.WriteLine("[WzLinkResolver] Outlink has no property path, only image: " + text3);
				return false;
			}
			if (wzImageProperty == null)
			{
				Debug.WriteLine($"[WzLinkResolver] Could not find property path '{text4}' in image '{text3}' (searched {list2.Count} _Canvas images)");
				return false;
			}
			if (!(wzImageProperty is WzCanvasProperty srcCanvas))
			{
				Debug.WriteLine($"[WzLinkResolver] Target property is not a canvas: {wzImageProperty.PropertyType}");
				return false;
			}
			return CopyCanvasData(canvas, srcCanvas, hasInlink: false, hasOutlink: true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[WzLinkResolver] Exception resolving outlink: " + ex.Message);
			return false;
		}
	}

	private WzImage FindImageInDirectory(WzDirectory directory, string imageName, string subdirPath = null)
	{
		if (directory == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(subdirPath))
		{
			string[] array = subdirPath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			WzDirectory wzDirectory = directory;
			string[] array2 = array;
			foreach (string b in array2)
			{
				if (wzDirectory.WzDirectories == null)
				{
					return null;
				}
				WzDirectory wzDirectory2 = null;
				foreach (WzDirectory wzDirectory3 in wzDirectory.WzDirectories)
				{
					if (wzDirectory3 != null && string.Equals(wzDirectory3.Name, b, StringComparison.OrdinalIgnoreCase))
					{
						wzDirectory2 = wzDirectory3;
						break;
					}
				}
				if (wzDirectory2 == null)
				{
					return null;
				}
				wzDirectory = wzDirectory2;
			}
			if (wzDirectory.WzImages != null)
			{
				foreach (WzImage wzImage2 in wzDirectory.WzImages)
				{
					if (wzImage2 != null && string.Equals(wzImage2.Name, imageName, StringComparison.OrdinalIgnoreCase))
					{
						return wzImage2;
					}
				}
			}
			return null;
		}
		if (directory.WzImages != null)
		{
			foreach (WzImage wzImage3 in directory.WzImages)
			{
				if (wzImage3 != null && string.Equals(wzImage3.Name, imageName, StringComparison.OrdinalIgnoreCase))
				{
					return wzImage3;
				}
			}
		}
		if (directory.WzDirectories != null)
		{
			foreach (WzDirectory wzDirectory4 in directory.WzDirectories)
			{
				if (wzDirectory4 != null)
				{
					WzImage wzImage = FindImageInDirectory(wzDirectory4, imageName);
					if (wzImage != null)
					{
						return wzImage;
					}
				}
			}
		}
		return null;
	}

	private static string NormalizePath(string path)
	{
		return string.IsNullOrEmpty(path) ? string.Empty : path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('/');
	}

	private static bool MatchesCanvasFolder(string wzFilePath, string categoryName, string canvasFolderPath)
	{
		if (string.IsNullOrEmpty(wzFilePath) || string.IsNullOrEmpty(categoryName))
		{
			return false;
		}
		if (string.IsNullOrEmpty(canvasFolderPath))
		{
			return wzFilePath.Contains("/_Canvas/", StringComparison.OrdinalIgnoreCase);
		}
		string value = NormalizePath(categoryName);
		string value2 = NormalizePath(canvasFolderPath);
		string value3 = $"/{value}/{value2}/";
		return wzFilePath.Contains(value3, StringComparison.OrdinalIgnoreCase);
	}

	private WzCanvasProperty FindCanvasInImage(WzImage image, string canvasName)
	{
		if (image == null || image.WzProperties == null)
		{
			return null;
		}
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			WzCanvasProperty wzCanvasProperty = FindCanvasInProperty(wzProperty, canvasName);
			if (wzCanvasProperty != null)
			{
				return wzCanvasProperty;
			}
		}
		return null;
	}

	private WzCanvasProperty FindCanvasInProperty(WzImageProperty property, string canvasName)
	{
		if (property == null)
		{
			return null;
		}
		if (property is WzCanvasProperty result && string.Equals(property.Name, canvasName, StringComparison.OrdinalIgnoreCase))
		{
			return result;
		}
		WzPropertyCollection wzProperties = property.WzProperties;
		if (wzProperties != null)
		{
			foreach (WzImageProperty item in wzProperties)
			{
				WzCanvasProperty wzCanvasProperty = FindCanvasInProperty(item, canvasName);
				if (wzCanvasProperty != null)
				{
					return wzCanvasProperty;
				}
			}
		}
		return null;
	}

	private WzCanvasProperty FindCanvasByIndex(WzImage image, int index)
	{
		if (image == null || image.WzProperties == null)
		{
			return null;
		}
		string text = index.ToString();
		WzImageProperty wzImageProperty = image[text];
		if (wzImageProperty is WzCanvasProperty result)
		{
			return result;
		}
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			if (wzProperty is WzCanvasProperty result2 && wzProperty.Name == text)
			{
				return result2;
			}
			WzPropertyCollection wzProperties = wzProperty.WzProperties;
			if (wzProperties == null)
			{
				continue;
			}
			foreach (WzImageProperty item in wzProperties)
			{
				if (item is WzCanvasProperty result3 && item.Name == text)
				{
					return result3;
				}
			}
		}
		return null;
	}

	private WzCanvasProperty FindCanvasWithParent(WzImage image, string parentName, string childName)
	{
		if (image == null || image.WzProperties == null)
		{
			return null;
		}
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			WzCanvasProperty wzCanvasProperty = FindCanvasWithParentInProperty(wzProperty, parentName, childName);
			if (wzCanvasProperty != null)
			{
				return wzCanvasProperty;
			}
		}
		return null;
	}

	private WzCanvasProperty FindCanvasWithParentInProperty(WzImageProperty property, string parentName, string childName)
	{
		if (property == null)
		{
			return null;
		}
		if (string.Equals(property.Name, parentName, StringComparison.OrdinalIgnoreCase))
		{
			WzPropertyCollection wzProperties = property.WzProperties;
			if (wzProperties != null)
			{
				foreach (WzImageProperty item in wzProperties)
				{
					if (item is WzCanvasProperty result && string.Equals(item.Name, childName, StringComparison.OrdinalIgnoreCase))
					{
						return result;
					}
				}
			}
		}
		WzPropertyCollection wzProperties2 = property.WzProperties;
		if (wzProperties2 != null)
		{
			foreach (WzImageProperty item2 in wzProperties2)
			{
				WzCanvasProperty wzCanvasProperty = FindCanvasWithParentInProperty(item2, parentName, childName);
				if (wzCanvasProperty != null)
				{
					return wzCanvasProperty;
				}
			}
		}
		return null;
	}

	private List<WzCanvasProperty> GetAllCanvasesInImage(WzImage image)
	{
		List<WzCanvasProperty> list = new List<WzCanvasProperty>();
		if (image == null || image.WzProperties == null)
		{
			return list;
		}
		foreach (WzImageProperty wzProperty in image.WzProperties)
		{
			CollectCanvasesFromProperty(wzProperty, list);
		}
		return list;
	}

	private void CollectCanvasesFromProperty(WzImageProperty property, List<WzCanvasProperty> canvases)
	{
		if (property == null)
		{
			return;
		}
		if (property is WzCanvasProperty item)
		{
			canvases.Add(item);
		}
		WzPropertyCollection wzProperties = property.WzProperties;
		if (wzProperties == null)
		{
			return;
		}
		foreach (WzImageProperty item2 in wzProperties)
		{
			CollectCanvasesFromProperty(item2, canvases);
		}
	}

	private void ShowPropertyTree(WzImageProperty property, string indent, int maxDepth)
	{
		if (property == null || maxDepth <= 0)
		{
			return;
		}
		WzPropertyCollection wzProperties = property.WzProperties;
		if (wzProperties == null || wzProperties.Count == 0)
		{
			Debug.WriteLine($"{indent}{property.Name} ({property.PropertyType})");
			return;
		}
		Debug.WriteLine(indent + property.Name + "/");
		foreach (WzImageProperty item in wzProperties.Take(3))
		{
			ShowPropertyTree(item, indent + "  ", maxDepth - 1);
		}
		if (wzProperties.Count > 3)
		{
			Debug.WriteLine($"{indent}  ... and {wzProperties.Count - 3} more");
		}
	}
}
