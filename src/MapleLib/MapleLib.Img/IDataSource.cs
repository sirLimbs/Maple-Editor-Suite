using System;
using System.Collections.Generic;
using MapleLib.WzLib;

namespace MapleLib.Img;

public interface IDataSource : IDisposable
{
	string Name { get; }

	bool IsInitialized { get; }

	VersionInfo VersionInfo { get; }

	WzImage GetImage(string category, string imageName);

	WzImage GetImageByPath(string relativePath);

	IEnumerable<WzImage> GetImagesInCategory(string category);

	IEnumerable<WzImage> GetImagesInDirectory(string category, string subDirectory);

	IEnumerable<string> GetImageNamesInDirectory(string category, string subDirectory);

	bool ImageExists(string category, string imageName);

	bool CategoryExists(string category);

	IEnumerable<string> GetCategories();

	IEnumerable<string> GetSubdirectories(string category);

	WzDirectory GetDirectory(string category);

	IEnumerable<WzDirectory> GetDirectories(string baseCategory);

	void PreloadCategory(string category);

	void ClearCache();

	DataSourceStats GetStats();

	bool SaveImage(string category, WzImage image, string relativePath = null);

	void MarkImageUpdated(string category, WzImage image);
}
