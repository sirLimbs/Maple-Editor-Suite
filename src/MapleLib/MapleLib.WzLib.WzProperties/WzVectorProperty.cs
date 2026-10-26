using System.Drawing;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties;

public class WzVectorProperty : WzExtended
{
	internal string name;

	internal WzIntProperty x;

	internal WzIntProperty y;

	internal WzObject parent;

	public override object WzValue => new Point(x.Value, y.Value);

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

	public override WzPropertyType PropertyType => WzPropertyType.Vector;

	public WzIntProperty X
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	public WzIntProperty Y
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public Point Pos => new Point(X.Value, Y.Value);

	public override void SetValue(object value)
	{
		if (value is Point)
		{
			x.val = ((Point)value).X;
			y.val = ((Point)value).Y;
		}
		else
		{
			x.val = ((Size)value).Width;
			y.val = ((Size)value).Height;
		}
	}

	public override WzImageProperty DeepClone()
	{
		return new WzVectorProperty(name, x, y);
	}

	public override void WriteValue(WzBinaryWriter writer)
	{
		writer.WriteStringValue("Shape2D#Vector2D", 115, 27);
		writer.WriteCompressedInt(X.Value);
		writer.WriteCompressedInt(Y.Value);
	}

	public override void ExportXml(StreamWriter writer, int level)
	{
		writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzVector", Name, finish: false, empty: false) + XmlUtil.Attrib("X", X.Value.ToString()) + XmlUtil.Attrib("Y", Y.Value.ToString(), closeTag: true, empty: true));
	}

	public override void Dispose()
	{
		name = null;
		x.Dispose();
		x = null;
		y.Dispose();
		y = null;
	}

	public WzVectorProperty()
	{
	}

	public WzVectorProperty(string name)
	{
		this.name = name;
	}

	public WzVectorProperty(string name, WzIntProperty x, WzIntProperty y)
	{
		this.name = name;
		this.x = x;
		this.y = y;
	}

	public WzVectorProperty(string name, int x, int y)
	{
		this.name = name;
		this.x = new WzIntProperty(string.Empty, x);
		this.y = new WzIntProperty(string.Empty, y);
	}

	public WzVectorProperty(string name, float x, float y)
	{
		this.name = name;
		this.x = new WzIntProperty(string.Empty, (int)x);
		this.y = new WzIntProperty(string.Empty, (int)y);
	}

	public override Point GetPoint()
	{
		return new Point(x.val, y.val);
	}

	public override string ToString()
	{
		return "X: " + x.val + ", Y: " + y.val;
	}
}
