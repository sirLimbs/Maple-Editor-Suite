using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzConvexProperty : WzExtended, IPropertyContainer
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

	public override WzPropertyType PropertyType => WzPropertyType.Convex;

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
			string text = name.ToLower();
			foreach (WzImageProperty property in properties)
			{
				if (property.Name.ToLower() == text)
				{
					return property;
				}
			}
			return null;
		}
	}

	public override void SetValue(object value)
	{
		throw new NotImplementedException();
	}

	public override WzImageProperty DeepClone()
	{
		WzConvexProperty wzConvexProperty = new WzConvexProperty(name);
		foreach (WzImageProperty property in properties)
		{
			wzConvexProperty.AddProperty(property.DeepClone());
		}
		return wzConvexProperty;
	}

	public WzImageProperty GetProperty(string name)
	{
		string text = name.ToLower();
		foreach (WzImageProperty property in properties)
		{
			if (property.Name.ToLower() == text)
			{
				return property;
			}
		}
		return null;
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
		List<WzExtended> list = new List<WzExtended>(properties.Count);
		foreach (WzImageProperty property in properties)
		{
			if (property is WzExtended item)
			{
				list.Add(item);
			}
		}
		writer.WriteStringValue("Shape2D#Convex2D", 115, 27);
		writer.WriteCompressedInt(list.Count);
		foreach (WzImageProperty property2 in properties)
		{
			property2.WriteValue(writer);
		}
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzConvex", Name, finish: true));
		WzImageProperty.DumpPropertyList(writer, level, WzProperties);
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzConvex"));
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

	public WzConvexProperty()
	{
		properties = new WzPropertyCollection(this);
	}

	public WzConvexProperty(string name)
	{
		this.name = name;
		properties = new WzPropertyCollection(this);
	}

	public void AddProperty(WzImageProperty prop)
	{
		if (!(prop is WzExtended))
		{
			throw new Exception("Property is not IExtended");
		}
		prop.Parent = this;
		properties.Add((WzExtended)prop);
	}

	public void AddProperties(WzPropertyCollection properties)
	{
		foreach (WzImageProperty property in properties)
		{
			AddProperty(property);
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
}
