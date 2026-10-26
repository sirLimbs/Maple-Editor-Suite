using System;

namespace MapleLib.WzLib.WzStructure.Data;

[Flags]
public enum ItemTypes
{
	None = 0,
	Tiles = 1,
	Objects = 2,
	Mobs = 4,
	NPCs = 8,
	Ropes = 0x10,
	Footholds = 0x20,
	Portals = 0x40,
	Chairs = 0x80,
	Reactors = 0x100,
	ToolTips = 0x200,
	Backgrounds = 0x400,
	Misc = 0x800,
	MirrorFieldData = 0x1000,
	All = Tiles | Objects | Mobs | NPCs | Ropes | Footholds | Portals | Chairs | Reactors | ToolTips | Backgrounds | Misc
}
