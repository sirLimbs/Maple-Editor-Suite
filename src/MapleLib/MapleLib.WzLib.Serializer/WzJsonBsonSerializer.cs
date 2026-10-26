using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace MapleLib.WzLib.Serializer;

public class WzJsonBsonSerializer : WzSerializer, IWzImageSerializer, IWzDirectorySerializer, IWzFileSerializer
{
	private readonly bool bExportAsJson;

	public WzJsonBsonSerializer(int indentation, LineBreak lineBreakType, bool bExportBase64Data, bool bExportAsJson)
		: base(indentation, lineBreakType)
	{
		base.bExportBase64Data = bExportBase64Data;
		this.bExportAsJson = bExportAsJson;
	}

	private void ExportInternal(WzImage img, string path)
	{
		ExportInternalAsync(img, path).GetAwaiter().GetResult();
	}

	private async Task ExportInternalAsync(WzImage img, string path)
	{
		bool parsed = img.Parsed || img.Changed;
		if (!parsed)
		{
			img.ParseImage();
		}
		curr++;
		Dictionary<string, object> jsonObject = new Dictionary<string, object>();
		foreach (WzImageProperty property in img.WzProperties)
		{
			WritePropertyToJsonBson(jsonObject, property, path);
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		using (FileStream file = File.Create(path))
		{
			if (bExportAsJson)
			{
				await System.Text.Json.JsonSerializer.SerializeAsync((Stream)file, jsonObject, new JsonSerializerOptions
				{
					WriteIndented = true
				}, default(CancellationToken));
			}
			else
			{
				using MemoryStream ms = new MemoryStream();
				using (BsonDataWriter writer = new BsonDataWriter(ms))
				{
					Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
					serializer.Serialize(writer, jsonObject);
				}
				byte[] bsonData = ms.ToArray();
				await file.WriteAsync(bsonData, 0, bsonData.Length);
			}
		}
		if (!parsed)
		{
			img.UnparseImage();
		}
	}

	private void exportDirInternal(WzDirectory dir, string path)
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
			exportDirInternal(wzDirectory, path + ProgressingWzSerializer.EscapeInvalidFilePathNames(wzDirectory.name) + "\\");
		}
		foreach (WzImage wzImage in dir.WzImages)
		{
			ExportInternal(wzImage, path + ProgressingWzSerializer.EscapeInvalidFilePathNames(wzImage.Name) + (bExportAsJson ? ".json" : ".bin"));
		}
	}

	public void SerializeImage(WzImage img, string path)
	{
		total = 1;
		curr = 0;
		if (Path.GetExtension(path) != (bExportAsJson ? ".json" : ".bin"))
		{
			path += (bExportAsJson ? ".json" : ".bin");
		}
		ExportInternal(img, path);
	}

	public void SerializeDirectory(WzDirectory dir, string path)
	{
		total = dir.CountImages();
		curr = 0;
		exportDirInternal(dir, path);
	}

	public void SerializeFile(WzFile file, string path)
	{
		SerializeDirectory(file.WzDirectory, path);
	}
}
