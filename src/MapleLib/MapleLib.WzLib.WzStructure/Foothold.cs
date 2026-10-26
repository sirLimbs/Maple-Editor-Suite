namespace MapleLib.WzLib.WzStructure;

public struct Foothold(int x1, int x2, int y1, int y2, int num, int layer)
{
	public int x1 = x1;

	public int x2 = x2;

	public int y1 = y1;

	public int y2 = y2;

	public int prev = 0;

	public int next = 0;

	public int num = num;

	public int layer = layer;

	public bool IsWall()
	{
		return x1 == x2;
	}
}
