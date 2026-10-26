using System;

namespace MapleLib.Img;

public static class DataSourceFactory
{
	public static IDataSource Create(DataSourceMode mode, string path, HaCreatorConfig config = null)
	{
		if (config == null)
		{
			config = new HaCreatorConfig();
		}
		if (1 == 0)
		{
		}
		IDataSource result = mode switch
		{
			DataSourceMode.ImgFileSystem => (IDataSource)new ImgFileSystemDataSource(path, config), 
			DataSourceMode.WzFiles => new WzFileDataSource(path, config), 
			DataSourceMode.Hybrid => new HybridDataSource(path, config), 
			_ => throw new ArgumentException($"Unknown data source mode: {mode}"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
