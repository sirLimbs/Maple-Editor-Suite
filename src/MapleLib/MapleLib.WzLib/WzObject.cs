using System;
using System.Drawing;

namespace MapleLib.WzLib;

public abstract class WzObject : IDisposable
{
	private object hcTag = null;

	private object hcTag_spine = null;

	private object msTag = null;

	private object msTag_spine = null;

	private object tag3 = null;

	public abstract string Name { get; set; }

	public abstract WzObjectType ObjectType { get; }

	public abstract WzObject Parent { get; internal set; }

	public abstract WzFile WzFileParent { get; }

	public WzObject this[string name]
	{
		get
		{
			if (this is WzFile)
			{
				return ((WzFile)this)[name];
			}
			if (this is WzDirectory)
			{
				return ((WzDirectory)this)[name];
			}
			if (this is WzImage)
			{
				return ((WzImage)this)[name];
			}
			if (this is WzImageProperty)
			{
				return ((WzImageProperty)this)[name];
			}
			throw new NotImplementedException();
		}
	}

	public string FullPath
	{
		get
		{
			if (this is WzFile wzFile)
			{
				return wzFile.WzDirectory.Name;
			}
			string text = Name;
			WzObject wzObject = this;
			while (wzObject.Parent != null)
			{
				wzObject = wzObject.Parent;
				text = wzObject.Name + "\\" + text;
			}
			return text;
		}
	}

	public virtual object HCTag
	{
		get
		{
			return hcTag;
		}
		set
		{
			hcTag = value;
		}
	}

	public virtual object HCTagSpine
	{
		get
		{
			return hcTag_spine;
		}
		set
		{
			hcTag_spine = value;
		}
	}

	public virtual object MSTag
	{
		get
		{
			return msTag;
		}
		set
		{
			msTag = value;
		}
	}

	public virtual object MSTagSpine
	{
		get
		{
			return msTag_spine;
		}
		set
		{
			msTag_spine = value;
		}
	}

	public virtual object HRTag
	{
		get
		{
			return tag3;
		}
		set
		{
			tag3 = value;
		}
	}

	public virtual object WzValue => null;

	public abstract void Dispose();

	public WzObject GetTopMostWzDirectory()
	{
		WzObject parent = Parent;
		if (parent == null)
		{
			return this;
		}
		while (parent.Parent != null)
		{
			parent = parent.Parent;
		}
		return parent;
	}

	public WzObject GetTopMostWzImage()
	{
		WzObject parent = Parent;
		if (parent == null)
		{
			return this;
		}
		while (parent.Parent != null)
		{
			parent = parent.Parent;
			if (parent.GetType() == typeof(WzImage))
			{
				return parent;
			}
		}
		return parent;
	}

	public abstract void Remove();

	public virtual int GetInt()
	{
		throw new NotImplementedException();
	}

	public virtual short GetShort()
	{
		throw new NotImplementedException();
	}

	public virtual long GetLong()
	{
		throw new NotImplementedException();
	}

	public virtual float GetFloat()
	{
		throw new NotImplementedException();
	}

	public virtual double GetDouble()
	{
		throw new NotImplementedException();
	}

	public virtual string GetString()
	{
		throw new NotImplementedException();
	}

	public virtual Point GetPoint()
	{
		throw new NotImplementedException();
	}

	public virtual Bitmap GetBitmap()
	{
		throw new NotImplementedException();
	}

	public virtual byte[] GetBytes()
	{
		throw new NotImplementedException();
	}
}
