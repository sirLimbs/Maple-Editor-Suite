using Newtonsoft.Json;

namespace MapleLib.Img;

public class ExtractionConfig
{
	[JsonProperty("defaultOutputPath")]
	public string DefaultOutputPath { get; set; }

	[JsonProperty("generateIndex")]
	public bool GenerateIndex { get; set; } = true;

	[JsonProperty("validateAfterExtract")]
	public bool ValidateAfterExtract { get; set; } = true;

	[JsonProperty("parallelThreads")]
	public int ParallelThreads { get; set; } = 4;
}
