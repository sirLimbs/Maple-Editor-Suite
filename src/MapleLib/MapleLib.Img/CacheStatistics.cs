namespace MapleLib.Img;

public class CacheStatistics
{
	public int ItemCount { get; set; }

	public long SizeBytes { get; set; }

	public long MaxSizeBytes { get; set; }

	public long HitCount { get; set; }

	public long MissCount { get; set; }

	public long EvictionCount { get; set; }

	public double HitRatio { get; set; }

	public override string ToString()
	{
		return $"Items: {ItemCount}, Size: {SizeBytes / 1024 / 1024}MB, Hit Ratio: {HitRatio:P1}, Evictions: {EvictionCount}";
	}
}
