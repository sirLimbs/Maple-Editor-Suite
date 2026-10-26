using System;
using System.Collections.Generic;

namespace MapleLib.Img;

internal class ImageCaseMapData
{
	public string Format { get; set; } = "ImgCaseMapV1";

	public Dictionary<string, string> Entries { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
