using System.Collections.Generic;

namespace MapleLib.Img;

internal class ListWzJsonFormat
{
	public string Type { get; set; }

	public string Description { get; set; }

	public List<string> Entries { get; set; }

	public List<string> RemainingEntries { get; set; }

	public List<string> DecryptedEntries { get; set; }

	public int OriginalCount { get; set; }

	public int DecryptedCount { get; set; }

	public int RemainingCount { get; set; }
}
