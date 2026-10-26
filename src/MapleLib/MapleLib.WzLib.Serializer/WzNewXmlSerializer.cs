using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.Serializer;

public class WzNewXmlSerializer : WzSerializer
{
	public WzNewXmlSerializer(int indentation, LineBreak lineBreakType)
		: base(indentation, lineBreakType)
	{
	}

	internal void DumpImageToXML(TextWriter tw, string depth, WzImage img, string exportFilePath)
	{
		bool flag = img.Parsed || img.Changed;
		if (!flag)
		{
			img.ParseImage();
		}
		curr++;
		tw.Write(depth + "<wzimg name=\"" + XmlUtil.SanitizeText(img.Name) + "\">" + lineBreak);
		string depth2 = depth + indent;
		foreach (WzImageProperty wzProperty in img.WzProperties)
		{
			WritePropertyToXML(tw, depth2, wzProperty, exportFilePath);
		}
		tw.Write(depth + "</wzimg>");
		if (!flag)
		{
			img.UnparseImage();
		}
	}

	internal void DumpDirectoryToXML(TextWriter tw, string depth, WzDirectory dir, string exportFilePath)
	{
		tw.Write(depth + "<wzdir name=\"" + XmlUtil.SanitizeText(dir.Name) + "\">" + lineBreak);
		foreach (WzDirectory wzDirectory in dir.WzDirectories)
		{
			DumpDirectoryToXML(tw, depth + indent, wzDirectory, exportFilePath);
		}
		foreach (WzImage wzImage in dir.WzImages)
		{
			DumpImageToXML(tw, depth + indent, wzImage, exportFilePath);
		}
		tw.Write(depth + "</wzdir>" + lineBreak);
	}

	public void ExportCombinedXml(List<WzObject> objects, string exportFilePath)
	{
		total = 1;
		curr = 0;
		if (Path.GetExtension(exportFilePath) != ".xml")
		{
			exportFilePath += ".xml";
		}
		total += objects.OfType<WzImage>().Count();
		total += objects.OfType<WzDirectory>().Sum((WzDirectory d) => d.CountImages());
		bExportBase64Data = true;
		using TextWriter textWriter = new StreamWriter(exportFilePath);
		textWriter.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + lineBreak);
		textWriter.Write("<xmldump>" + lineBreak);
		foreach (WzObject @object in objects)
		{
			if (@object is WzDirectory)
			{
				DumpDirectoryToXML(textWriter, indent, (WzDirectory)@object, exportFilePath);
			}
			else if (@object is WzImage)
			{
				DumpImageToXML(textWriter, indent, (WzImage)@object, exportFilePath);
			}
			else if (@object is WzImageProperty)
			{
				WritePropertyToXML(textWriter, indent, (WzImageProperty)@object, exportFilePath);
			}
		}
		textWriter.Write("</xmldump>" + lineBreak);
	}
}
