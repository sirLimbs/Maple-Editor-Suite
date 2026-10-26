namespace MapleLib.WzLib;

public class WzHeader
{
	private const string DEFAULT_WZ_HEADER_COPYRIGHT = "Package file v1.0 Copyright 2002 Wizet, ZMS";

	private string ident;

	private string copyright;

	private ulong fsize;

	private uint fstart;

	public string Ident
	{
		get
		{
			return ident;
		}
		set
		{
			ident = value;
		}
	}

	public string Copyright
	{
		get
		{
			return copyright;
		}
		set
		{
			copyright = value;
		}
	}

	public ulong FSize
	{
		get
		{
			return fsize;
		}
		set
		{
			fsize = value;
		}
	}

	public uint FStart
	{
		get
		{
			return fstart;
		}
		set
		{
			fstart = value;
		}
	}

	public void RecalculateFileStart()
	{
		fstart = (uint)(ident.Length + 8 + 4 + copyright.Length + 1);
	}

	public static WzHeader GetDefault()
	{
		return new WzHeader
		{
			ident = "PKG1",
			copyright = "Package file v1.0 Copyright 2002 Wizet, ZMS",
			fstart = 60u,
			fsize = 0uL
		};
	}
}
