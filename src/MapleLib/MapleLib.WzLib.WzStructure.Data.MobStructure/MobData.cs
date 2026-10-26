using System.Collections.Generic;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.WzStructure.Data.MobStructure;

public class MobData
{
	public int MobId { get; set; }

	public byte Level { get; set; }

	public byte Category { get; set; }

	public byte RareItemDropLevel { get; set; }

	public byte IgnoreFieldOut { get; set; }

	public byte OnlyNormalAttack { get; set; }

	public bool IsBoss { get; set; }

	public bool FirstAttack { get; set; }

	public byte ExplosiveReward { get; set; }

	public byte PublicReward { get; set; }

	public byte Undead { get; set; }

	public bool Friendly { get; set; }

	public bool DamagedByMob { get; set; }

	public byte Escort { get; set; }

	public byte GetCP { get; set; }

	public bool PartyBonusMob { get; set; }

	public bool DualGauge { get; set; }

	public byte NoDoom { get; set; }

	public byte RemoveOnMiss { get; set; }

	public byte SummonType { get; set; }

	public byte BodyAttack { get; set; }

	public bool NoFlip { get; set; }

	public short Eva { get; set; }

	public short Acc { get; set; }

	public int MaxHP { get; set; }

	public int MaxMP { get; set; }

	public int Exp { get; set; }

	public int Pushed { get; set; }

	public int PADamage { get; set; }

	public int MADamage { get; set; }

	public int RemoveAfter { get; set; }

	public int ChargeCount { get; set; }

	public bool HasAngerGauge { get; set; }

	public int FixedDamage { get; set; } = -1;

	public int Buff { get; set; } = -1;

	public int PDDamage { get; set; }

	public int MDDamage { get; set; }

	public int CharismaEXP { get; set; }

	public int WillEXP { get; set; }

	public int PDRate { get; set; }

	public int MDRate { get; set; }

	public int HpRecovery { get; set; }

	public int MpRecovery { get; set; }

	public string ElemAttr { get; set; } = "";

	public bool CanFly { get; set; }

	public bool IsMobile { get; set; }

	public bool CanJump { get; set; }

	public short Speed { get; set; }

	public short FlySpeed { get; set; }

	public short HpTagColor { get; set; }

	public short HpTagBgColor { get; set; }

	public MobHpDisplayType HpDisplayType { get; set; }

	public List<int> ReviveData { get; set; } = new List<int>();

	public List<MobSkillData> SkillData { get; set; } = new List<MobSkillData>();

	public List<MobAttackData> AttackData { get; set; } = new List<MobAttackData>();

	public MobSelfDestructionData SelfDestruction { get; set; }

	public MobBanishData Banish { get; set; }

	public static MobData Parse(WzImage mobImage, int mobId)
	{
		if (mobImage == null)
		{
			return null;
		}
		MobData mobData = new MobData
		{
			MobId = mobId
		};
		WzSubProperty wzSubProperty = (WzSubProperty)mobImage["info"];
		if (wzSubProperty == null)
		{
			return mobData;
		}
		WzImage wzImage = mobImage;
		int num = InfoTool.GetInt(wzSubProperty["link"]);
		if (num != 0 && mobImage.Parent is WzDirectory wzDirectory)
		{
			string name = string.Format("{0}{1}.img", (num < 1000000) ? "0" : "", num);
			WzImage imageByName = wzDirectory.GetImageByName(name);
			if (imageByName != null)
			{
				if (!imageByName.Parsed)
				{
					imageByName.ParseImage();
				}
				wzImage = imageByName;
				WzSubProperty wzSubProperty2 = (WzSubProperty)imageByName["info"];
				if (wzSubProperty2 != null)
				{
					wzSubProperty = wzSubProperty2;
				}
			}
		}
		mobData.Level = (byte)InfoTool.GetInt(wzSubProperty["level"]);
		mobData.Category = (byte)InfoTool.GetInt(wzSubProperty["category"]);
		mobData.RareItemDropLevel = (byte)InfoTool.GetInt(wzSubProperty["rareItemDropLevel"]);
		mobData.IgnoreFieldOut = (byte)InfoTool.GetInt(wzSubProperty["ignoreFieldOut"]);
		mobData.OnlyNormalAttack = (byte)InfoTool.GetInt(wzSubProperty["onlyNormalAttack"]);
		bool flag = (mobData.IsBoss = InfoTool.GetInt(wzSubProperty["boss"]) > 0 || mobId == 8810018 || mobId == 8810118 || mobId == 9410066);
		mobData.FirstAttack = InfoTool.GetInt(wzSubProperty["firstAttack"]) > 0 || mobId == 9300275 || mobId == 9300282;
		mobData.ExplosiveReward = (byte)InfoTool.GetInt(wzSubProperty["explosiveReward"]);
		mobData.PublicReward = (byte)InfoTool.GetInt(wzSubProperty["publicReward"]);
		mobData.Undead = (byte)InfoTool.GetInt(wzSubProperty["undead"]);
		mobData.DamagedByMob = InfoTool.GetInt(wzSubProperty["damagedByMob"]) > 0;
		mobData.Friendly = mobData.DamagedByMob;
		mobData.Escort = (byte)InfoTool.GetInt(wzSubProperty["escort"]);
		mobData.GetCP = (byte)InfoTool.GetInt(wzSubProperty["getCP"]);
		mobData.PartyBonusMob = InfoTool.GetInt(wzSubProperty["partyBonusMob"]) > 0;
		mobData.DualGauge = InfoTool.GetInt(wzSubProperty["dualGauge"]) > 0;
		mobData.NoDoom = (byte)InfoTool.GetInt(wzSubProperty["noDoom"]);
		mobData.RemoveOnMiss = (byte)InfoTool.GetInt(wzSubProperty["removeOnMiss"]);
		mobData.SummonType = (byte)InfoTool.GetInt(wzSubProperty["summonType"]);
		mobData.BodyAttack = (byte)InfoTool.GetInt(wzSubProperty["bodyAttack"]);
		mobData.NoFlip = InfoTool.GetInt(wzSubProperty["noFlip"]) == 1;
		mobData.Eva = (short)InfoTool.GetInt(wzSubProperty["eva"]);
		mobData.Acc = (short)InfoTool.GetInt(wzSubProperty["acc"]);
		mobData.MaxHP = InfoTool.GetInt(wzSubProperty["maxHP"]);
		mobData.MaxMP = InfoTool.GetInt(wzSubProperty["maxMP"]);
		mobData.Exp = InfoTool.GetInt(wzSubProperty["exp"]);
		mobData.Pushed = InfoTool.GetInt(wzSubProperty["pushed"]);
		mobData.PADamage = InfoTool.GetInt(wzSubProperty["PADamage"]);
		mobData.MADamage = InfoTool.GetInt(wzSubProperty["MADamage"]);
		mobData.RemoveAfter = InfoTool.GetInt(wzSubProperty["removeAfter"]);
		mobData.ChargeCount = InfoTool.GetInt(wzSubProperty["ChargeCount"]);
		mobData.HasAngerGauge = InfoTool.GetInt(wzSubProperty["AngerGauge"]) > 0;
		mobData.FixedDamage = wzSubProperty["fixedDamage"].GetInt(-1);
		mobData.Buff = wzSubProperty["buff"].GetInt(-1);
		mobData.PDDamage = InfoTool.GetInt(wzSubProperty["PDDamage"]);
		mobData.MDDamage = InfoTool.GetInt(wzSubProperty["MDDamage"]);
		mobData.CharismaEXP = InfoTool.GetInt(wzSubProperty["charismaEXP"]);
		mobData.WillEXP = InfoTool.GetInt(wzSubProperty["willEXP"]);
		mobData.PDRate = InfoTool.GetInt(wzSubProperty["PDRate"]);
		mobData.MDRate = InfoTool.GetInt(wzSubProperty["MDRate"]);
		mobData.HpRecovery = InfoTool.GetInt(wzSubProperty["hpRecovery"]);
		mobData.MpRecovery = InfoTool.GetInt(wzSubProperty["mpRecovery"]);
		WzImageProperty wzImageProperty = wzSubProperty["elemAttr"];
		mobData.ElemAttr = ((wzImageProperty != null) ? InfoTool.GetString(wzImageProperty) : "");
		WzSubProperty wzSubProperty3 = (WzSubProperty)wzSubProperty["revive"];
		if (wzSubProperty3 != null)
		{
			foreach (WzImageProperty wzProperty in wzSubProperty3.WzProperties)
			{
				mobData.ReviveData.Add(InfoTool.GetInt(wzProperty));
			}
		}
		WzSubProperty wzSubProperty4 = (WzSubProperty)wzSubProperty["skill"];
		if (wzSubProperty4 != null)
		{
			foreach (WzSubProperty wzProperty2 in wzSubProperty4.WzProperties)
			{
				MobSkillData item = new MobSkillData
				{
					SourceIndex = (int.TryParse(wzProperty2.Name, out var result) ? result : mobData.SkillData.Count),
					SkillAfter = InfoTool.GetInt(wzProperty2["skillAfter"]),
					EffectAfter = InfoTool.GetInt(wzProperty2["effectAfter"]),
					Skill = InfoTool.GetInt(wzProperty2["skill"]),
					Action = InfoTool.GetInt(wzProperty2["action"]),
					Level = InfoTool.GetInt(wzProperty2["level"]),
					Priority = InfoTool.GetInt(wzProperty2["priority"]),
					PreSkillIndex = (byte)InfoTool.GetInt(wzProperty2["preSkillIndex"]),
					PreSkillCount = (byte)InfoTool.GetInt(wzProperty2["preSkillCount"]),
					OnlyFsm = (InfoTool.GetInt(wzProperty2["onlyFsm"]) > 0),
					SkillForbid = InfoTool.GetInt(wzProperty2["skillForbid"])
				};
				mobData.SkillData.Add(item);
			}
		}
		WzSubProperty wzSubProperty6 = (WzSubProperty)wzSubProperty["selfDestruction"];
		if (wzSubProperty6 != null)
		{
			mobData.SelfDestruction = new MobSelfDestructionData
			{
				Hp = InfoTool.GetInt(wzSubProperty6["hp"]),
				Action = (short)wzSubProperty6["action"].GetInt(-1),
				RemoveAfter = wzSubProperty6["removeAfter"].GetInt(-1)
			};
		}
		if (flag)
		{
			mobData.HpTagColor = (short)InfoTool.GetInt(wzSubProperty["hpTagColor"]);
			mobData.HpTagBgColor = (short)InfoTool.GetInt(wzSubProperty["hpTagBgcolor"]);
		}
		WzSubProperty wzSubProperty7 = (WzSubProperty)wzSubProperty["ban"];
		if (wzSubProperty7 != null)
		{
			mobData.Banish = new MobBanishData
			{
				BanType = (byte)InfoTool.GetInt(wzSubProperty7["banType"]),
				BanMsg = (wzSubProperty7["banMsg"].GetOptionalString() ?? ""),
				BanMapField = wzSubProperty7.GetFromPath("banMap/0/field").GetInt(-1),
				BanMapPortal = (wzSubProperty7.GetFromPath("banMap/0/portal").GetOptionalString() ?? "sp")
			};
		}
		WzSubProperty wzSubProperty8 = (WzSubProperty)wzSubProperty["attack"];
		if (wzSubProperty8 != null)
		{
			foreach (WzSubProperty wzProperty3 in wzSubProperty8.WzProperties)
			{
				if (byte.TryParse(wzProperty3.Name, out var result2))
				{
					MobAttackData item2 = new MobAttackData
					{
						AttackNum = result2,
						Type = wzProperty3["type"].GetInt(-1),
						Action = (byte)InfoTool.GetInt(wzProperty3["action"]),
						AttackCount = InfoTool.GetInt(wzProperty3["attackCount"]),
						Magic = (byte)InfoTool.GetInt(wzProperty3["magic"]),
						DeadlyAttack = (byte)InfoTool.GetInt(wzProperty3["deadlyAttack"]),
						Knockback = (byte)InfoTool.GetInt(wzProperty3["knockback"]),
						Rush = (InfoTool.GetInt(wzProperty3["rush"]) > 0),
						JumpAttack = (InfoTool.GetInt(wzProperty3["jumpAttack"]) > 0),
						Tremble = (InfoTool.GetInt(wzProperty3["tremble"]) > 0),
						BulletSpeed = InfoTool.GetInt(wzProperty3["bulletSpeed"]),
						MpBurn = InfoTool.GetInt(wzProperty3["mpBurn"]),
						Disease = InfoTool.GetInt(wzProperty3["disease"]),
						Level = InfoTool.GetInt(wzProperty3["level"]),
						ConMP = InfoTool.GetInt(wzProperty3["conMP"]),
						HitEffectPath = (wzProperty3["sHit"].GetOptionalString() ?? wzProperty3["hit"].GetOptionalString()),
						HasHitAttach = (wzProperty3["bHitAttach"] != null || wzProperty3["attach"] != null || wzProperty3["hitAttach"] != null),
						HitAttach = (InfoTool.GetInt(wzProperty3["bHitAttach"] ?? wzProperty3["attach"] ?? wzProperty3["hitAttach"]) > 0)
					};
					mobData.AttackData.Add(item2);
				}
			}
		}
		foreach (WzImageProperty wzProperty4 in wzImage.WzProperties)
		{
			string name2 = wzProperty4.Name;
			switch (name2)
			{
			case "fly":
				mobData.CanFly = true;
				mobData.IsMobile = true;
				continue;
			case "jump":
				mobData.CanJump = true;
				mobData.IsMobile = true;
				continue;
			default:
				if (!(name2 == "walk"))
				{
					continue;
				}
				break;
			case "move":
				break;
			}
			mobData.IsMobile = true;
		}
		mobData.Speed = (short)InfoTool.GetInt(wzSubProperty["speed"]);
		mobData.FlySpeed = (short)InfoTool.GetInt(wzSubProperty["flySpeed"]);
		if (mobData.DualGauge)
		{
			mobData.HpDisplayType = MobHpDisplayType.DualGauge;
		}
		else if (mobData.HpTagColor > 0)
		{
			mobData.HpDisplayType = MobHpDisplayType.Boss;
		}
		else if (mobData.DamagedByMob)
		{
			mobData.HpDisplayType = MobHpDisplayType.Friendly;
		}
		else if (mobId >= 9300184 && mobId <= 9300215)
		{
			mobData.HpDisplayType = MobHpDisplayType.MulungTC;
		}
		else if (!flag || mobData.PartyBonusMob)
		{
			mobData.HpDisplayType = MobHpDisplayType.Normal;
		}
		else
		{
			mobData.HpDisplayType = MobHpDisplayType.None;
		}
		return mobData;
	}
}
