using System.Drawing;
using System.IO;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.Serializer;

public class WzPngMp3Serializer : ProgressingWzSerializer, IWzImageSerializer, IWzDirectorySerializer, IWzFileSerializer, IWzObjectSerializer
{
	private string outPath;

	public void SerializeObject(WzObject obj, string outPath)
	{
		total = 0;
		curr = 0;
		this.outPath = outPath;
		if (!Directory.Exists(outPath))
		{
			ProgressingWzSerializer.CreateDirSafe(ref outPath);
		}
		if (outPath.Substring(outPath.Length - 1, 1) != "\\")
		{
			outPath += "\\";
		}
		total = CalculateTotal(obj);
		ExportRecursion(obj, outPath);
	}

	public void SerializeFile(WzFile file, string path)
	{
		SerializeObject(file, path);
	}

	public void SerializeDirectory(WzDirectory file, string path)
	{
		SerializeObject(file, path);
	}

	public void SerializeImage(WzImage file, string path)
	{
		SerializeObject(file, path);
	}

	private int CalculateTotal(WzObject currObj)
	{
		int num = 0;
		if (currObj is WzFile wzFile)
		{
			num += wzFile.WzDirectory.CountImages();
		}
		else if (currObj is WzDirectory wzDirectory)
		{
			num += wzDirectory.CountImages();
		}
		return num;
	}

	private void ExportRecursion(WzObject currObj, string outPath)
	{
		if (currObj is WzFile wzFile)
		{
			ExportRecursion(wzFile.WzDirectory, outPath);
			return;
		}
		if (currObj is WzDirectory wzDirectory)
		{
			outPath = outPath + ProgressingWzSerializer.EscapeInvalidFilePathNames(currObj.Name) + "\\";
			if (!Directory.Exists(outPath))
			{
				Directory.CreateDirectory(outPath);
			}
			foreach (WzDirectory wzDirectory2 in wzDirectory.WzDirectories)
			{
				ExportRecursion(wzDirectory2, outPath + wzDirectory2.Name + "\\");
			}
			{
				foreach (WzImage wzImage2 in wzDirectory.WzImages)
				{
					ExportRecursion(wzImage2, outPath + wzImage2.Name + "\\");
				}
				return;
			}
		}
		if (currObj is WzCanvasProperty wzCanvasProperty)
		{
			Bitmap linkedWzCanvasBitmap = wzCanvasProperty.GetLinkedWzCanvasBitmap();
			string filename = outPath + ProgressingWzSerializer.EscapeInvalidFilePathNames(currObj.Name) + ".png";
			linkedWzCanvasBitmap.Save(filename);
			return;
		}
		if (currObj is WzBinaryProperty wzBinaryProperty)
		{
			string file = outPath + ProgressingWzSerializer.EscapeInvalidFilePathNames(currObj.Name) + ".mp3";
			wzBinaryProperty.SaveToFile(file);
			return;
		}
		if (currObj is WzImage wzImage)
		{
			outPath = outPath + ProgressingWzSerializer.EscapeInvalidFilePathNames(currObj.Name) + "\\";
			if (!Directory.Exists(outPath))
			{
				Directory.CreateDirectory(outPath);
			}
			bool flag = wzImage.Parsed || wzImage.Changed;
			if (!flag)
			{
				wzImage.ParseImage();
			}
			foreach (WzImageProperty wzProperty in wzImage.WzProperties)
			{
				ExportRecursion(wzProperty, outPath);
			}
			if (!flag)
			{
				wzImage.UnparseImage();
			}
			curr++;
			return;
		}
		if (currObj is IPropertyContainer propertyContainer)
		{
			outPath = outPath + ProgressingWzSerializer.EscapeInvalidFilePathNames(currObj.Name) + ".";
			{
				foreach (WzImageProperty wzProperty2 in propertyContainer.WzProperties)
				{
					ExportRecursion(wzProperty2, outPath);
				}
				return;
			}
		}
		if (currObj is WzUOLProperty { LinkValue: var linkValue } && linkValue is WzCanvasProperty currObj2)
		{
			ExportRecursion(currObj2, outPath);
		}
	}
}
