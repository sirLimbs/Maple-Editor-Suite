namespace MapleLib.Img;

public class HotSwapConfig
{
	public bool Enabled { get; set; } = true;

	public int DebounceMs { get; set; } = 500;

	public bool WatchVersions { get; set; } = true;

	public bool WatchCategories { get; set; } = true;

	public bool AutoInvalidateCache { get; set; } = true;

	public bool AutoRefreshDisplayedAssets { get; set; } = true;

	public bool ConfirmRefreshPlacedItems { get; set; } = true;

	public bool PauseSimulatorOnAssetChange { get; set; } = false;

	public DeletedAssetBehavior DeletedAssetBehavior { get; set; } = DeletedAssetBehavior.ShowPlaceholder;
}
