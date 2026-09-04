using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 本 Mod 卡牌给敌人上的增益不走原版多人层数缩放。
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetScaledAmountForMultiplayer))]
internal static class ModCardEnemyBuffMultiplayerScalingPatch
{
	private static bool Prefix(
		PowerModel __instance,
		decimal amount,
		Creature target,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!ShouldSkipScaling(__instance, amount, target, cardSource))
		{
			return true;
		}

		__result = amount;
		return false;
	}

	private static bool ShouldSkipScaling(
		PowerModel power,
		decimal amount,
		Creature target,
		CardModel? cardSource)
	{
		if (cardSource is null || cardSource.GetType().Assembly != typeof(Squ.SquMod).Assembly)
		{
			return false;
		}

		if (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
		{
			return false;
		}

		return power.GetTypeForAmount(amount) == PowerType.Buff;
	}
}
