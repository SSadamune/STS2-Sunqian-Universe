using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 修改原版火焰药水：造成 10 点伤害并给予 6 层灼烧。
/// </summary>
[HarmonyPatch(typeof(FirePotion))]
internal static class FirePotionBurningPatch
{
	public const decimal DamageAmount = 10m;
	public const decimal BurningAmount = 6m;

	[HarmonyPrefix]
	[HarmonyPatch("OnUse")]
	private static bool OnUsePrefix(
		FirePotion __instance,
		PlayerChoiceContext choiceContext,
		Creature? target,
		ref Task __result)
	{
		__result = OnUseAsync(__instance, choiceContext, target);
		return false;
	}

	private static async Task OnUseAsync(
		FirePotion potion,
		PlayerChoiceContext choiceContext,
		Creature? target)
	{
		ArgumentNullException.ThrowIfNull(target);

		NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(target));
		await CreatureCmd.Damage(
			choiceContext,
			target,
			DamageAmount,
			ValueProp.Unpowered,
			potion.Owner.Creature,
			cardSource: null,
			cardPlay: null);
		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			target,
			BurningAmount,
			potion.Owner.Creature,
			null);
	}
}

[HarmonyPatch(typeof(PotionModel), "get_ExtraHoverTips")]
internal static class FirePotionExtraHoverTipsPatch
{
	private static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (__instance is not FirePotion)
		{
			return;
		}

		__result =
		[
			..__result,
			HoverTipFactory.FromPower<BurningPower>(),
		];
	}
}
