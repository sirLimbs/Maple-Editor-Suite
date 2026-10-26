namespace MapleLib.Img;

public class DataSourceStats
{
	public int CategoryCount { get; set; }

	public int ImageCount { get; set; }

	public int CachedImageCount { get; set; }

	public long MemoryUsageBytes { get; set; }

	public int DiskReadCount { get; set; }

	public int CacheHitCount { get; set; }

	public int CacheMissCount { get; set; }

	public double CacheHitRatio => (CacheHitCount + CacheMissCount > 0) ? ((double)CacheHitCount / (double)(CacheHitCount + CacheMissCount)) : 0.0;
}
