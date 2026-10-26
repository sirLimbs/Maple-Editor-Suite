namespace MapleLib.WzLib;

public interface IPropertyContainer
{
	WzPropertyCollection WzProperties { get; }

	WzImageProperty this[string name] { get; set; }

	void AddProperty(WzImageProperty prop);

	void AddProperties(WzPropertyCollection props);

	void RemoveProperty(string propertyName);

	void RemoveProperty(WzImageProperty prop);

	void ClearProperties();
}
