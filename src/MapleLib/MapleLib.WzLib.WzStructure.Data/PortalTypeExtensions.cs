using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleLib.WzLib.WzStructure.Data;

public static class PortalTypeExtensions
{
	private static readonly IReadOnlyDictionary<PortalType, string> _portalTypeToCodes = new Dictionary<PortalType, string>
	{
		{
			PortalType.StartPoint,
			"sp"
		},
		{
			PortalType.Invisible,
			"pi"
		},
		{
			PortalType.Visible,
			"pv"
		},
		{
			PortalType.Default,
			"default"
		},
		{
			PortalType.Collision,
			"pc"
		},
		{
			PortalType.Changeable,
			"pg"
		},
		{
			PortalType.ChangeableInvisible,
			"pgi"
		},
		{
			PortalType.TownPortalPoint,
			"tp"
		},
		{
			PortalType.Script,
			"ps"
		},
		{
			PortalType.ScriptInvisible,
			"psi"
		},
		{
			PortalType.CollisionScript,
			"pcs"
		},
		{
			PortalType.Hidden,
			"ph"
		},
		{
			PortalType.ScriptHidden,
			"psh"
		},
		{
			PortalType.CollisionVerticalJump,
			"pcj"
		},
		{
			PortalType.CollisionCustomImpact,
			"pci"
		},
		{
			PortalType.CollisionCustomImpact2,
			"pci2"
		},
		{
			PortalType.CollisionUnknownPcig,
			"pcig"
		},
		{
			PortalType.ScriptHiddenUng,
			"pshg"
		},
		{
			PortalType.UNKNOWN_PCC,
			"pcc"
		},
		{
			PortalType.UNKNOWN_PCIR,
			"pcir"
		}
	};

	private static readonly IReadOnlyDictionary<PortalType, string> _portalTypeToNames = new Dictionary<PortalType, string>
	{
		{
			PortalType.StartPoint,
			"Start Point"
		},
		{
			PortalType.Invisible,
			"Invisible"
		},
		{
			PortalType.Visible,
			"Visible"
		},
		{
			PortalType.Default,
			"Visible (Default)"
		},
		{
			PortalType.Collision,
			"Collision"
		},
		{
			PortalType.Changeable,
			"Changeable"
		},
		{
			PortalType.ChangeableInvisible,
			"Changeable Invisible"
		},
		{
			PortalType.TownPortalPoint,
			"Town Portal"
		},
		{
			PortalType.Script,
			"Script"
		},
		{
			PortalType.ScriptInvisible,
			"Script Invisible"
		},
		{
			PortalType.CollisionScript,
			"Script Collision"
		},
		{
			PortalType.Hidden,
			"Hidden"
		},
		{
			PortalType.ScriptHidden,
			"Script Hidden"
		},
		{
			PortalType.CollisionVerticalJump,
			"Collision Vertical Jump"
		},
		{
			PortalType.CollisionCustomImpact,
			"Collision Custom Impact Spring"
		},
		{
			PortalType.CollisionCustomImpact2,
			"Collision Custom Impact Spring 2"
		},
		{
			PortalType.CollisionUnknownPcig,
			"Unknown (PCIG)"
		},
		{
			PortalType.ScriptHiddenUng,
			"Script Hidden Ung"
		},
		{
			PortalType.UNKNOWN_PCC,
			"Unknown pcc"
		},
		{
			PortalType.UNKNOWN_PCIR,
			"Unknown pcir"
		}
	};

	private static readonly IReadOnlyDictionary<string, PortalType> _codeToPortalTypes = _portalTypeToCodes.ToDictionary((KeyValuePair<PortalType, string> x) => x.Value, (KeyValuePair<PortalType, string> x) => x.Key, StringComparer.OrdinalIgnoreCase);

	public static string ToCode(this PortalType portalType)
	{
		if (!_portalTypeToCodes.TryGetValue(portalType, out var value))
		{
			throw new ArgumentOutOfRangeException("portalType");
		}
		return value;
	}

	public static PortalType FromCode(string code)
	{
		if (code == null)
		{
			throw new ArgumentNullException("code");
		}
		if (!_codeToPortalTypes.TryGetValue(code, out var value))
		{
			throw new ArgumentException("Invalid portal type code: " + code, "code");
		}
		return value;
	}

	public static string GetFriendlyName(this PortalType portalType)
	{
		if (!_portalTypeToNames.TryGetValue(portalType, out var value))
		{
			throw new ArgumentOutOfRangeException("portalType");
		}
		return value;
	}
}
