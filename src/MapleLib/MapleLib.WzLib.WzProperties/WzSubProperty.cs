using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzSubProperty : WzExtended, IPropertyContainer
{
	internal WzPropertyCollection properties;

	internal string name;

	internal WzObject parent;

	public override WzObject Parent
	{
		get
		{
			return parent;
		}
		internal set
		{
			parent = value;
		}
	}

	public override WzPropertyType PropertyType => WzPropertyType.SubProperty;

	public override WzPropertyCollection WzProperties => properties;

	public override string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public override WzImageProperty this[string name]
	{
		get
		{
			return properties.FirstOrDefault((WzImageProperty iwp) => iwp.Name.ToLower() == name.ToLower());
		}
		set
		{
			if (value != null)
			{
				value.Name = name;
				AddProperty(value);
			}
		}
	}

	public override void SetValue(object value)
	{
		throw new NotImplementedException();
	}

	public override WzImageProperty DeepClone()
	{
		WzSubProperty wzSubProperty = new WzSubProperty(name);
		foreach (WzImageProperty property in properties)
		{
			wzSubProperty.AddProperty(property.DeepClone());
		}
		return wzSubProperty;
	}

	public override WzImageProperty GetFromPath(string path)
	{
		string[] array = path.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (array[0] == "..")
		{
			return ((WzImageProperty)Parent)[path.Substring(name.IndexOf('/') + 1)];
		}
		WzImageProperty wzImageProperty = this;
		string[] array2 = array;
		foreach (string segment in array2)
		{
			wzImageProperty = wzImageProperty.WzProperties.FirstOrDefault((WzImageProperty iwp) => iwp.Name == segment);
			if (wzImageProperty == null)
			{
				break;
			}
		}
		return wzImageProperty;
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		if (properties.Count != 1 || !(properties[0] is WzLuaProperty))
		{
			writer.WriteStringValue("Property", 115, 27);
		}
		WzImageProperty.WritePropertyList(writer, properties);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzSub", Name, finish: true));
		WzImageProperty.DumpPropertyList(writer, level, WzProperties);
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzSub"));
	}

	public override void Dispose()
	{
		name = null;
		foreach (WzImageProperty property in properties)
		{
			property.Dispose();
		}
		properties.Clear();
		properties = null;
	}

	public WzSubProperty()
	{
		properties = new WzPropertyCollection(this);
	}

	public WzSubProperty(string name)
	{
		this.name = name;
		properties = new WzPropertyCollection(this);
	}

	public void AddProperty(WzImageProperty prop)
	{
		prop.Parent = this;
		properties.Add(prop);
	}

	public void AddProperties(WzPropertyCollection props)
	{
		foreach (WzImageProperty prop in props)
		{
			AddProperty(prop);
		}
	}

	public void AddPropertiesForWzImageDumping(List<WzImageProperty> props)
	{
		foreach (WzImageProperty prop in props)
		{
			properties.Add(prop);
		}
	}

	public void RemoveProperty(WzImageProperty prop)
	{
		prop.Parent = null;
		properties.Remove(prop);
	}

	public void RemoveProperty(string propertyName)
	{
		WzImageProperty wzImageProperty = this[propertyName];
		if (wzImageProperty != null)
		{
			RemoveProperty(wzImageProperty);
		}
	}

	public void ClearProperties()
	{
		foreach (WzImageProperty property in properties)
		{
			property.Parent = null;
		}
		properties.Clear();
	}

	public void SortProperties()
	{
		properties.Sort((WzImageProperty img1, WzImageProperty img2) =>
		{
			if (img1 == null)
			{
				return 0;
			}
			if (img1.GetType() == typeof(WzCanvasProperty) || img1.GetType() == typeof(WzSubProperty))
			{
				if (int.TryParse(img1.Name, out var result) && int.TryParse(img2.Name, out var result2))
				{
					if (result == result2)
					{
						return 0;
					}
					if (result > result2)
					{
						return 1;
					}
					return -1;
				}
				return img1.Name.CompareTo(img2.Name);
			}
			return img1.Name.CompareTo(img2.Name);
		});
	}
}
