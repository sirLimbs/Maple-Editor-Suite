using System;
using System.Collections.Generic;
using System.Drawing;
using MapleLib.Helpers;
using MapleLib.WzLib.WzProperties;
using MapleLib.WzLib.WzStructure.Data;
using MapleLib.WzLib.WzStructure.Data.MapStructure;

namespace MapleLib.WzLib.WzStructure;

public class MapInfo
{
	public static MapInfo Default = new MapInfo();

	private WzImage image = null;

	public MapType mapType = MapType.RegularMap;

	public int version = 10;

	public readonly List<WzImageProperty> unsupportedInfoProperties = new List<WzImageProperty>();

	public string bgm = "Bgm00/GoPicnic";

	public string mapMark = "None";

	public long fieldLimit = 0L;

	public int returnMap = 999999999;

	public int forcedReturn = 999999999;

	public bool cloud = false;

	public bool swim = false;

	public bool hideMinimap = false;

	public bool town = false;

	public float mobRate = 1.5f;

	public int? VRTop = null;

	public int? VRBottom = null;

	public int? VRLeft = null;

	public int? VRRight = null;

	public int? LBSide = null;

	public int? LBTop = null;

	public int? LBBottom = null;

	public int? timeLimit = null;

	public int? lvLimit = null;

	public FieldType? fieldType = null;

	public string onFirstUserEnter = null;

	public string onUserEnter = null;

	public MapleBool fly = null;

	public MapleBool noMapCmd = null;

	public MapleBool partyOnly = null;

	public MapleBool reactorShuffle = null;

	public string reactorShuffleName = null;

	public MapleBool personalShop = null;

	public MapleBool entrustedShop = null;

	public string effect = null;

	public int? lvForceMove = null;

	public TimeMob? timeMob = null;

	public string help = null;

	public MapleBool snow = null;

	public MapleBool rain = null;

	public int? dropExpire = null;

	public int? decHP = null;

	public int? decInterval = null;

	public AutoLieDetector autoLieDetector = null;

	public MapleBool expeditionOnly = null;

	public float? fs = null;

	public List<int> protectItem = null;

	public int? createMobInterval = null;

	public int? fixedMobCapacity = null;

	public MapleBool mirror_Bottom = null;

	public int? moveLimit = null;

	public string mapDesc = null;

	public string mapName = null;

	public string streetName = null;

	public MapleBool miniMapOnOff = null;

	public MapleBool noRegenMap = null;

	public List<int> allowedItem = null;

	public float? recovery = null;

	public MapleBool blockPBossChange = null;

	public MapleBool everlast = null;

	public MapleBool damageCheckFree = null;

	public float? dropRate = null;

	public MapleBool scrollDisable = null;

	public MapleBool needSkillForFly = null;

	public MapleBool zakum2Hack = null;

	public MapleBool allMoveCheck = null;

	public MapleBool VRLimit = null;

	public int? consumeItemCoolTime = null;

	public MapleBool nofollowCharacter = null;

	public MapleBool vanishDragon = null;

	public MapleBool zeroSideOnly = null;

	public List<WzImageProperty> additionalProps = new List<WzImageProperty>();

	public List<WzImageProperty> additionalNonInfoProps = new List<WzImageProperty>();

	public string strMapName = "<Untitled>";

	public string strStreetName = "<Untitled>";

	public string strCategoryName = "HaCreator";

	public int id = 0;

	public WzImage Image
	{
		get
		{
			return image;
		}
		set
		{
			image = value;
		}
	}

	public MapInfo()
	{
	}

	public MapInfo(WzImage image, string strMapName, string strStreetName, string strCategoryName)
	{
		this.image = image;
		this.strMapName = strMapName;
		this.strStreetName = strStreetName;
		this.strCategoryName = strCategoryName;
		WzFile wzFileParent = image.WzFileParent;
		string text = ", map " + image.Name + ((wzFileParent != null) ? (" of version " + Enum.GetName(typeof(WzMapleVersion), wzFileParent.MapleVersion) + ", v" + wzFileParent.Version) : "");
		WzImageProperty wzImageProperty = image["info"];
		foreach (WzImageProperty wzProperty in wzImageProperty.WzProperties)
		{
			switch (wzProperty.Name)
			{
			case "bgm":
				bgm = InfoTool.GetString(wzProperty);
				break;
			case "cloud":
				cloud = wzProperty.GetBool();
				break;
			case "swim":
				swim = wzProperty.GetBool();
				break;
			case "forcedReturn":
				forcedReturn = InfoTool.GetInt(wzProperty);
				break;
			case "hideMinimap":
				hideMinimap = wzProperty.GetBool();
				break;
			case "mapDesc":
				mapDesc = InfoTool.GetString(wzProperty);
				break;
			case "mapName":
				mapName = InfoTool.GetString(wzProperty);
				break;
			case "mapMark":
				mapMark = InfoTool.GetString(wzProperty);
				break;
			case "mobRate":
				mobRate = InfoTool.GetFloat(wzProperty);
				break;
			case "moveLimit":
				moveLimit = InfoTool.GetInt(wzProperty);
				break;
			case "returnMap":
				returnMap = InfoTool.GetInt(wzProperty);
				break;
			case "town":
				town = wzProperty.GetBool();
				break;
			case "version":
				version = InfoTool.GetInt(wzProperty);
				break;
			case "fieldLimit":
			{
				long num = InfoTool.GetLong(wzProperty);
				fieldLimit = num;
				break;
			}
			case "VRTop":
				VRTop = wzProperty.GetOptionalInt(0);
				break;
			case "VRBottom":
				VRBottom = wzProperty.GetOptionalInt(0);
				break;
			case "VRLeft":
				VRLeft = wzProperty.GetOptionalInt(0);
				break;
			case "VRRight":
				VRRight = wzProperty.GetOptionalInt(0);
				break;
			case "timeLimit":
				timeLimit = InfoTool.GetInt(wzProperty);
				break;
			case "lvLimit":
				lvLimit = InfoTool.GetInt(wzProperty);
				break;
			case "onFirstUserEnter":
				onFirstUserEnter = InfoTool.GetString(wzProperty);
				break;
			case "onUserEnter":
				onUserEnter = InfoTool.GetString(wzProperty);
				break;
			case "fly":
				fly = wzProperty.GetBool();
				break;
			case "noMapCmd":
				noMapCmd = wzProperty.GetBool();
				break;
			case "partyOnly":
				partyOnly = wzProperty.GetBool();
				break;
			case "fieldType":
			{
				int num2 = InfoTool.GetInt(wzProperty);
				if (!Enum.IsDefined(typeof(FieldType), num2))
				{
					ErrorLogger.Log(ErrorLevel.IncorrectStructure, "Invalid fieldType " + num2 + text);
					num2 = 0;
				}
				fieldType = (FieldType)num2;
				break;
			}
			case "miniMapOnOff":
				miniMapOnOff = wzProperty.GetBool();
				break;
			case "reactorShuffle":
				reactorShuffle = wzProperty.GetBool();
				break;
			case "reactorShuffleName":
				reactorShuffleName = InfoTool.GetString(wzProperty);
				break;
			case "personalShop":
				personalShop = wzProperty.GetBool();
				break;
			case "entrustedShop":
				entrustedShop = wzProperty.GetBool();
				break;
			case "effect":
				effect = InfoTool.GetString(wzProperty);
				break;
			case "lvForceMove":
				lvForceMove = InfoTool.GetInt(wzProperty);
				break;
			case "timeMob":
			{
				int? optionalInt = wzProperty["startHour"].GetOptionalInt();
				int? optionalInt2 = wzProperty["endHour"].GetOptionalInt();
				int? optionalInt5 = wzProperty["id"].GetOptionalInt();
				string optionalString = wzProperty["message"].GetOptionalString();
				if (!optionalInt5.HasValue || optionalString == null || (!optionalInt.HasValue ^ !optionalInt2.HasValue))
				{
					ErrorLogger.Log(ErrorLevel.IncorrectStructure, "timeMob" + text);
				}
				else
				{
					timeMob = new TimeMob(optionalInt, optionalInt2, optionalInt5.Value, optionalString);
				}
				break;
			}
			case "help":
				help = InfoTool.GetString(wzProperty);
				break;
			case "snow":
				snow = wzProperty.GetBool();
				break;
			case "rain":
				rain = wzProperty.GetBool();
				break;
			case "dropExpire":
				dropExpire = InfoTool.GetInt(wzProperty);
				break;
			case "decHP":
				decHP = InfoTool.GetInt(wzProperty);
				break;
			case "decInterval":
				decInterval = InfoTool.GetInt(wzProperty);
				break;
			case "autoLieDetector":
			{
				int? optionalInt = wzProperty["startHour"].GetOptionalInt();
				int? optionalInt2 = wzProperty["endHour"].GetOptionalInt();
				int? optionalInt3 = wzProperty["interval"].GetOptionalInt();
				int? optionalInt4 = wzProperty["prop"].GetOptionalInt();
				if (!optionalInt.HasValue || !optionalInt2.HasValue || !optionalInt3.HasValue || !optionalInt4.HasValue)
				{
					ErrorLogger.Log(ErrorLevel.IncorrectStructure, "autoLieDetector" + text);
				}
				else
				{
					autoLieDetector = new AutoLieDetector(optionalInt.Value, optionalInt2.Value, optionalInt3.Value, optionalInt4.Value);
				}
				break;
			}
			case "expeditionOnly":
				expeditionOnly = wzProperty.GetBool();
				break;
			case "fs":
				fs = InfoTool.GetFloat(wzProperty);
				break;
			case "protectItem":
				if (wzProperty is WzSubProperty)
				{
					WzSubProperty wzSubProperty = wzProperty as WzSubProperty;
					protectItem = new List<int>();
					if (wzSubProperty.WzProperties == null || wzSubProperty.WzProperties.Count <= 0)
					{
						break;
					}
					foreach (WzImageProperty wzProperty2 in wzSubProperty.WzProperties)
					{
						protectItem.Add(wzProperty2.GetInt());
					}
				}
				else
				{
					protectItem = new List<int> { InfoTool.GetInt(wzProperty) };
				}
				break;
			case "createMobInterval":
				createMobInterval = InfoTool.GetInt(wzProperty);
				break;
			case "fixedMobCapacity":
				fixedMobCapacity = InfoTool.GetInt(wzProperty);
				break;
			case "streetName":
				streetName = InfoTool.GetString(wzProperty);
				break;
			case "noRegenMap":
				noRegenMap = wzProperty.GetBool();
				break;
			case "allowedItem":
				allowedItem = new List<int>();
				if (wzProperty.WzProperties == null || wzProperty.WzProperties.Count <= 0)
				{
					break;
				}
				foreach (WzImageProperty wzProperty3 in wzProperty.WzProperties)
				{
					allowedItem.Add(wzProperty3.GetInt());
				}
				break;
			case "recovery":
				recovery = InfoTool.GetFloat(wzProperty);
				break;
			case "blockPBossChange":
				blockPBossChange = wzProperty.GetBool();
				break;
			case "everlast":
				everlast = wzProperty.GetBool();
				break;
			case "damageCheckFree":
				damageCheckFree = wzProperty.GetBool();
				break;
			case "dropRate":
				dropRate = InfoTool.GetFloat(wzProperty);
				break;
			case "scrollDisable":
				scrollDisable = wzProperty.GetBool();
				break;
			case "needSkillForFly":
				needSkillForFly = wzProperty.GetBool();
				break;
			case "zakum2Hack":
				zakum2Hack = wzProperty.GetBool();
				break;
			case "allMoveCheck":
				allMoveCheck = wzProperty.GetBool();
				break;
			case "nofollowCharacter":
				nofollowCharacter = wzProperty.GetBool();
				break;
			case "vanishDragon":
				vanishDragon = wzProperty.GetBool();
				break;
			case "VRLimit":
				VRLimit = wzProperty.GetBool();
				break;
			case "consumeItemCoolTime":
				consumeItemCoolTime = InfoTool.GetInt(wzProperty);
				break;
			case "zeroSideOnly":
				zeroSideOnly = wzProperty.GetBool();
				break;
			case "mirror_Bottom":
				mirror_Bottom = wzProperty.GetBool();
				break;
			case "LBSide":
				LBSide = InfoTool.GetInt(wzProperty);
				break;
			case "LBTop":
				LBTop = InfoTool.GetInt(wzProperty);
				break;
			case "LBBottom":
				LBBottom = InfoTool.GetInt(wzProperty);
				break;
			case "AmbientBGM":
			case "AmbientBGMv":
			case "areaCtrl":
			case "onlyUseSkill":
			case "limitUseShop":
			case "limitUseTrunk":
			case "freeFallingVX":
			case "midAirAccelVX":
			case "midAirDecelVX":
			case "jumpSpeedR":
			case "jumpAccUpKey":
			case "jumpAccDownKey":
			case "jumpApplyVX":
			case "dashSkill":
			case "speedMaxOver":
			case "speedMaxOver ":
			case "isSpecialMoveCheck":
			case "forceSpeed":
			case "forceJump":
			case "forceUseIndie":
			case "vanishPet":
			case "vanishAndroid":
			case "limitUI":
			case "largeSplit":
			case "qrLimit":
			case "noChair":
			case "fieldScript":
			case "standAlone":
			case "partyStandAlone":
			case "HobbangKing":
			case "quarterView":
			case "MRLeft":
			case "MRTop":
			case "MRRight":
			case "MRBottom":
			case "limitSpeedAndJump":
			case "directionInfo":
			case "bgmSub":
			case "fieldLimit2":
			case "fieldLimit_tw":
			case "particle":
			case "qrLimitJob":
			case "individualMobPool":
			case "barrier":
			case "remoteEffect":
			case "isFixDec":
			case "decHPr":
			case "isDecView":
			case "forceFadeInTime":
			case "forceFadeOutTime":
			case "qrLimitState":
			case "qrLimitState2":
			case "difficulty":
			case "alarm":
			case "bossMobID":
			case "reviveCurField":
			case "ReviveCurFieldOfNoTransfer":
			case "ReviveCurFieldOfNoTransferPoint":
			case "limitUserEffectField":
			case "functionObjInfo":
			case "quest":
			case "ridingMove":
			case "ridingField":
			case "noLanding":
			case "noCancelSkill":
			case "defaultAvatarDir":
			case "footStepSound":
			case "taggedObjRegenInfo":
			case "offSoulAbsorption":
			case "canPartyStatChangeIgnoreParty":
			case "canPartyStatChangeIgnoreParty ":
			case "forceReturnOnDead":
			case "zoomOutField":
			case "EscortMinTime":
			case "deathCount":
			case "onEnterResetFifthSkill":
			case "potionLimit":
			case "lifeCount":
			case "respawnCooltimeField":
			case "scale":
			case "skills":
			case "noResurection":
			case "incInterval":
			case "incMPr":
			case "bonusExpPerUserHPRate":
			case "barrierArc":
			case "noBackOverlapped":
			case "partyBonusR":
			case "specialSound":
			case "inFieldsetForcedReturn":
			case "chaser":
			case "chaserEndTime":
			case "chaserEffect":
			case "cagePotal":
			case "cageLT":
			case "cageRB":
			case "chaserHoldTime":
			case "phase":
			case "boss":
			case "actionBarIdx":
			case "shadowzone":
			case "towerChairEnable":
			case "DiceMasterUpIndex":
			case "DiceMasterDownIndex":
			case "ChangeName":
			case "entrustedFishing":
			case "forcedScreenMode":
			case "isHideUI":
			case "resetRidingField":
			case "PL_Gunman":
			case "noHekatonEffect":
			case "mode":
			case "skill":
			case "MR":
			case "ratemob":
			case "bonusStageNoChangeBack":
			case "questAmbience":
			case "blockScriptItem":
			case "remoteTranslucenceView":
			case "rewardContentName":
			case "specialDeadAction":
			case "FriendsStoryBossDelay":
			case "soulFieldType":
			case "cameraMoveY":
			case "subType":
			case "playTime":
			case "noEvent":
			case "forParty":
			case "whiteOut":
			case "UserInvisible":
			case "OnFirstUserEnter":
			case "teamPortal":
			case "typingGame":
			case "lt":
			case "rb":
			case "fieldAttackObj":
			case "timeLeft":
			case "timeLeft_d":
			case "eventSummon":
			case "rankCheckMob":
			case "questCheckMob":
			case "bindSkillLimit":
			case "NoMobCapacityLimit":
			case "userTimer":
			case "eventChairIndex":
			case "onlyEventChair":
			case "waitReviveTime":
			case "RidingTop":
			case "temporarySkill":
			case "largeSplt":
			case "blockTakeOffItem":
			case "PartyOnly":
			case "climb":
			case "bulletConsume":
			case "gaugeDelay":
			case "individualPet":
			case "level":
			case "hungryMuto":
			case "property":
			case "spiritSavior":
			case "standAlonePermitUpgrade":
			case "limitHeadAlarmField":
			case "noPsychicGrab":
			case "offSoulEffect":
			case "offActiveEffectItem":
			case "forceDamageSkinID":
			case "minimapFullSizeViewBlock":
			case "limitUIContextMenu":
			case "vanishHaku":
			case "pulbicTaggedObjectVisible":
			case "housingGrid":
			case "noUseStardustField":
			case "shiftChannelForbidden":
			case "barrierAut":
			{
				WzImageProperty item = wzProperty.DeepClone();
				unsupportedInfoProperties.Add(item);
				break;
			}
			default:
				ErrorLogger.Log(ErrorLevel.MissingFeature, $"[MapInfo] Unknown field info/ property: '{wzProperty.Name}'. {text}. Please fix it at MapInfo.cs");
				additionalProps.Add(wzProperty.DeepClone());
				break;
			case "link":
				break;
			}
		}
	}

	public static Rectangle? GetVR(WzImage image)
	{
		Rectangle? result = null;
		if (image["info"]["VRLeft"] != null)
		{
			WzImageProperty wzImageProperty = image["info"];
			int num = InfoTool.GetInt(wzImageProperty["VRLeft"]);
			int num2 = InfoTool.GetInt(wzImageProperty["VRRight"]);
			int num3 = InfoTool.GetInt(wzImageProperty["VRTop"]);
			int num4 = InfoTool.GetInt(wzImageProperty["VRBottom"]);
			result = new Rectangle(num, num3, num2 - num, num4 - num3);
		}
		return result;
	}

	public void Save(WzImage dest, Rectangle? VR)
	{
		WzSubProperty wzSubProperty = new WzSubProperty();
		wzSubProperty["bgm"] = InfoTool.SetString(bgm);
		wzSubProperty["cloud"] = InfoTool.SetBool(cloud);
		wzSubProperty["swim"] = InfoTool.SetBool(swim);
		wzSubProperty["forcedReturn"] = InfoTool.SetInt(forcedReturn);
		wzSubProperty["hideMinimap"] = InfoTool.SetBool(hideMinimap);
		wzSubProperty["mapDesc"] = InfoTool.SetOptionalString(mapDesc);
		wzSubProperty["mapName"] = InfoTool.SetOptionalString(mapDesc);
		wzSubProperty["mapMark"] = InfoTool.SetString(mapMark);
		wzSubProperty["mobRate"] = InfoTool.SetFloat(mobRate);
		wzSubProperty["moveLimit"] = InfoTool.SetOptionalInt(moveLimit);
		wzSubProperty["returnMap"] = InfoTool.SetInt(returnMap);
		wzSubProperty["town"] = InfoTool.SetBool(town);
		wzSubProperty["version"] = InfoTool.SetInt(version);
		wzSubProperty["fieldLimit"] = InfoTool.SetInt((int)fieldLimit);
		wzSubProperty["timeLimit"] = InfoTool.SetOptionalInt(timeLimit);
		wzSubProperty["lvLimit"] = InfoTool.SetOptionalInt(lvLimit);
		wzSubProperty["onFirstUserEnter"] = InfoTool.SetOptionalString(onFirstUserEnter);
		wzSubProperty["onUserEnter"] = InfoTool.SetOptionalString(onUserEnter);
		wzSubProperty["fly"] = fly.SetOptionalBool();
		wzSubProperty["noMapCmd"] = noMapCmd.SetOptionalBool();
		wzSubProperty["partyOnly"] = partyOnly.SetOptionalBool();
		wzSubProperty["fieldType"] = InfoTool.SetOptionalInt((int?)fieldType);
		wzSubProperty["miniMapOnOff"] = miniMapOnOff.SetOptionalBool();
		wzSubProperty["reactorShuffle"] = reactorShuffle.SetOptionalBool();
		wzSubProperty["reactorShuffleName"] = InfoTool.SetOptionalString(reactorShuffleName);
		wzSubProperty["personalShop"] = personalShop.SetOptionalBool();
		wzSubProperty["entrustedShop"] = entrustedShop.SetOptionalBool();
		wzSubProperty["effect"] = InfoTool.SetOptionalString(effect);
		wzSubProperty["lvForceMove"] = InfoTool.SetOptionalInt(lvForceMove);
		wzSubProperty["mirror_Bottom"] = mirror_Bottom.SetOptionalBool();
		if (timeMob.HasValue)
		{
			WzSubProperty wzSubProperty2 = new WzSubProperty();
			wzSubProperty2["startHour"] = InfoTool.SetOptionalInt(timeMob.Value.startHour);
			wzSubProperty2["endHour"] = InfoTool.SetOptionalInt(timeMob.Value.endHour);
			wzSubProperty2["id"] = InfoTool.SetOptionalInt(timeMob.Value.id);
			wzSubProperty2["message"] = InfoTool.SetOptionalString(timeMob.Value.message);
			wzSubProperty["timeMob"] = wzSubProperty2;
		}
		wzSubProperty["help"] = InfoTool.SetOptionalString(help);
		wzSubProperty["snow"] = snow.SetOptionalBool();
		wzSubProperty["rain"] = rain.SetOptionalBool();
		wzSubProperty["dropExpire"] = InfoTool.SetOptionalInt(dropExpire);
		wzSubProperty["decHP"] = InfoTool.SetOptionalInt(decHP);
		wzSubProperty["decInterval"] = InfoTool.SetOptionalInt(decInterval);
		if (autoLieDetector != null)
		{
			WzSubProperty wzSubProperty3 = new WzSubProperty();
			wzSubProperty3["startHour"] = InfoTool.SetOptionalInt(autoLieDetector.startHour);
			wzSubProperty3["endHour"] = InfoTool.SetOptionalInt(autoLieDetector.endHour);
			wzSubProperty3["interval"] = InfoTool.SetOptionalInt(autoLieDetector.interval);
			wzSubProperty3["prop"] = InfoTool.SetOptionalInt(autoLieDetector.prop);
			wzSubProperty["autoLieDetector"] = wzSubProperty3;
		}
		wzSubProperty["expeditionOnly"] = expeditionOnly.SetOptionalBool();
		wzSubProperty["fs"] = InfoTool.SetOptionalFloat(fs);
		if (protectItem != null && protectItem.Count > 0)
		{
			if (protectItem.Count == 1)
			{
				wzSubProperty["protectItem"] = InfoTool.SetOptionalInt(protectItem[0]);
			}
			else
			{
				WzSubProperty wzSubProperty4 = new WzSubProperty();
				int num = 0;
				foreach (int item in protectItem)
				{
					WzIntProperty prop = new WzIntProperty(num.ToString(), item);
					wzSubProperty4.AddProperty(prop);
					num++;
				}
				wzSubProperty["protectItem"] = wzSubProperty4;
			}
		}
		wzSubProperty["createMobInterval"] = InfoTool.SetOptionalInt(createMobInterval);
		wzSubProperty["fixedMobCapacity"] = InfoTool.SetOptionalInt(fixedMobCapacity);
		wzSubProperty["streetName"] = InfoTool.SetOptionalString(streetName);
		wzSubProperty["noRegenMap"] = noRegenMap.SetOptionalBool();
		if (allowedItem != null)
		{
			WzSubProperty wzSubProperty5 = new WzSubProperty();
			for (int i = 0; i < allowedItem.Count; i++)
			{
				wzSubProperty5[i.ToString()] = InfoTool.SetInt(allowedItem[i]);
			}
			wzSubProperty["allowedItem"] = wzSubProperty5;
		}
		wzSubProperty["recovery"] = InfoTool.SetOptionalFloat(recovery);
		wzSubProperty["blockPBossChange"] = blockPBossChange.SetOptionalBool();
		wzSubProperty["everlast"] = everlast.SetOptionalBool();
		wzSubProperty["damageCheckFree"] = damageCheckFree.SetOptionalBool();
		wzSubProperty["dropRate"] = InfoTool.SetOptionalFloat(dropRate);
		wzSubProperty["scrollDisable"] = scrollDisable.SetOptionalBool();
		wzSubProperty["needSkillForFly"] = needSkillForFly.SetOptionalBool();
		wzSubProperty["zakum2Hack"] = zakum2Hack.SetOptionalBool();
		wzSubProperty["allMoveCheck"] = allMoveCheck.SetOptionalBool();
		wzSubProperty["VRLimit"] = VRLimit.SetOptionalBool();
		wzSubProperty["consumeItemCoolTime"] = InfoTool.SetOptionalInt(consumeItemCoolTime);
		wzSubProperty["nofollowCharacter"] = nofollowCharacter.SetOptionalBool();
		wzSubProperty["vanishDragon"] = vanishDragon.SetOptionalBool();
		wzSubProperty["zeroSideOnly"] = zeroSideOnly.SetOptionalBool();
		foreach (WzImageProperty additionalProp in additionalProps)
		{
			wzSubProperty.AddProperty(additionalProp);
		}
		if (VR.HasValue)
		{
			wzSubProperty["VRLeft"] = InfoTool.SetInt(VR.Value.Left);
			wzSubProperty["VRRight"] = InfoTool.SetInt(VR.Value.Right);
			wzSubProperty["VRTop"] = InfoTool.SetInt(VR.Value.Top);
			wzSubProperty["VRBottom"] = InfoTool.SetInt(VR.Value.Bottom);
		}
		if (LBSide.HasValue)
		{
			wzSubProperty["LBSide"] = InfoTool.SetInt(LBSide.Value);
		}
		if (LBTop.HasValue)
		{
			wzSubProperty["LBTop"] = InfoTool.SetInt(LBTop.Value);
		}
		if (LBBottom.HasValue)
		{
			wzSubProperty["LBBottom"] = InfoTool.SetInt(LBBottom.Value);
		}
		foreach (WzImageProperty unsupportedInfoProperty in unsupportedInfoProperties)
		{
			wzSubProperty.AddProperty(unsupportedInfoProperty);
		}
		dest["info"] = wzSubProperty;
	}

	public bool ShouldSerializeImage()
	{
		return false;
	}
}
