namespace MapleLib.WzLib.WzStructure.Data.MapStructure;

public struct TimeMob(int? startHour, int? endHour, int id, string message)
{
	public int? startHour = startHour;

	public int? endHour = endHour;

	public int id = id;

	public string message = message;
}
