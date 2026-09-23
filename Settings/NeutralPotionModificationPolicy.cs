#nullable enable
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ.Character;
using STS2RitsuLib.Data;

namespace Squ.Settings;

public enum NeutralContentPermissionMode
{
	Allow,
	WhenModCharacterPresent,
	Deny,
}

/// <summary>
/// 中立药水覆写的统一入口。所有中立药水 Patch 都应先调用 <see cref="ShouldApply"/>。
/// </summary>
public static class NeutralPotionModificationPolicy
{
	public static bool ShouldApply(PotionModel potion)
	{
		ArgumentNullException.ThrowIfNull(potion);

		NeutralContentPermissionMode mode = ModDataStore.For(SquMod.ModId)
			.Get<SquSettings>(SquSettings.DataKey)
			.NeutralPotionModification;

		return mode switch
		{
			NeutralContentPermissionMode.Allow => true,
			NeutralContentPermissionMode.WhenModCharacterPresent => HasModCharacter(potion),
			_ => false,
		};
	}

	private static bool HasModCharacter(PotionModel potion)
	{
		IRunState? runState = potion.IsMutable ? potion.Owner?.RunState : null;
		if (runState is null or NullRunState)
		{
			runState = RunManager.Instance.DebugOnlyGetState();
		}

		return runState?.Players.Any(player => player.Character is SunqianCharacter) == true;
	}
}
