using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.Serializer;

public class WzClassicXmlSerializer : WzSerializer, IWzImageSerializer, IWzDirectorySerializer, IWzFileSerializer
{
	public WzClassicXmlSerializer(int indentation, LineBreak lineBreakType, bool exportbase64)
		: base(indentation, lineBreakType)
	{
		bExportBase64Data = exportbase64;
	}

	private void exportXmlInternal(WzImage img, string path)
	{
		bool flag = img.Parsed || img.Changed;
		try
		{
			if (!flag)
			{
				img.ParseImage();
			}
			curr++;
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			List<WzImageProperty> list = img.WzProperties.ToList();
			using TextWriter textWriter = new StreamWriter(File.Create(path));
			textWriter.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
			textWriter.Write(lineBreak);
			textWriter.Write("<imgdir name=\"");
			textWriter.Write(XmlUtil.SanitizeText(img.Name));
			textWriter.Write("\">");
			textWriter.Write(lineBreak);
			foreach (WzImageProperty item in list)
			{
				WritePropertyToXML(textWriter, indent, item, path);
			}
			textWriter.Write("</imgdir>");
			textWriter.Write(lineBreak);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to export XML for image " + img.Name, innerException);
		}
		finally
		{
			if (!flag)
			{
				img.UnparseImage();
			}
		}
	}

	private void exportDirXmlInternal(WzDirectory dir, string path)
	{
		if (!Directory.Exists(path))
		{
			ProgressingWzSerializer.CreateDirSafe(ref path);
		}
		if (path.Substring(path.Length - 1) != "\\")
		{
			path += "\\";
		}
		foreach (WzDirectory wzDirectory in dir.WzDirectories)
		{
			exportDirXmlInternal(wzDirectory, path + ProgressingWzSerializer.EscapeInvalidFilePathNames(wzDirectory.name) + "\\");
		}
		foreach (WzImage wzImage in dir.WzImages)
		{
			exportXmlInternal(wzImage, path + ProgressingWzSerializer.EscapeInvalidFilePathNames(wzImage.Name) + ".xml");
		}
	}

	public void SerializeImage(WzImage img, string path)
	{
		total = 1;
		curr = 0;
		if (Path.GetExtension(path) != ".xml")
		{
			path += ".xml";
		}
		exportXmlInternal(img, path);
	}

	public void SerializeDirectory(WzDirectory dir, string path)
	{
		total = dir.CountImages();
		curr = 0;
		exportDirXmlInternal(dir, path);
	}

	public void SerializeFile(WzFile file, string path)
	{
		SerializeDirectory(file.WzDirectory, path);
	}
}
