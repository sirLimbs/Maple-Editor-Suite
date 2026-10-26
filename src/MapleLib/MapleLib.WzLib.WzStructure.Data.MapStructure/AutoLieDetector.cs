namespace MapleLib.WzLib.WzStructure.Data.MapStructure;

public class AutoLieDetector
{
	public int startHour;

	public int endHour;

	public int interval;

	public int prop;

	public AutoLieDetector(int startHour, int endHour, int interval, int prop)
	{
		this.startHour = startHour;
		this.endHour = endHour;
		this.interval = interval;
		this.prop = prop;
	}
}
