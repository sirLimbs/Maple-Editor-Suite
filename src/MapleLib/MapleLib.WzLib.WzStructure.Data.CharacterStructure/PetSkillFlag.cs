using System;

namespace MapleLib.WzLib.WzStructure.Data.CharacterStructure;

[Flags]
public enum PetSkillFlag
{
	PickupMeso = 0,
	PickupItem = 1,
	LongRange = 2,
	DropSweep = PickupItem | LongRange,
	Ignore = 4,
	PickupAll = PickupItem | Ignore,
	ConsumeHP = LongRange | Ignore,
	ConsumeMP = DropSweep | Ignore,
	AutoBuff = 8,
	Smart = PickupItem | AutoBuff,
	Giant = LongRange | AutoBuff,
	Shop = DropSweep | AutoBuff
}
