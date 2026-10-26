using MapleLib.WzLib;

namespace MapleMobEditor;

internal sealed class SharedImageLifetime
{
	private readonly WzImage _image;

	private int _remainingReferences;

	public SharedImageLifetime(WzImage image)
	{
		_image = image;
		_remainingReferences = 0;
	}

	public void Release()
	{
		_remainingReferences--;
		if (_remainingReferences == 0)
		{
			_image.Dispose();
		}
	}

	public void AddReference()
	{
		_remainingReferences++;
	}
}
