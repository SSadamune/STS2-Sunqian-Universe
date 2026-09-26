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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using Squ.Settings;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 修改原版火焰药水：造成 2 点伤害并给予 14 层灼烧。
/// </summary>
[HarmonyPatch(typeof(FirePotion))]
internal static class FirePotionBurningPatch
{
	public const decimal DamageAmount = 2m;
	public const decimal BurningAmount = 14m;

	[HarmonyPrefix]
	[HarmonyPatch("OnUse")]
	private static bool OnUsePrefix(
		FirePotion __instance,
		PlayerChoiceContext choiceContext,
		Creature? target,
		ref Task __result)
	{
		if (!NeutralPotionModificationPolicy.ShouldApply(__instance))
		{
			return true;
		}

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
		if (__instance is not FirePotion || !NeutralPotionModificationPolicy.ShouldApply(__instance))
		{
			return;
		}

		__result =
		[
			..__result,
			HoverTipFactory.FromPower<BurningPower>(),
			new HoverTip(
				SquCommonL10n.AnnotationTitle(),
				SquCommonL10n.NeutralPotionModifiedAnnotation()),
		];
	}
}

[HarmonyPatch(typeof(PotionModel), "get_DynamicDescription")]
internal static class FirePotionDescriptionPatch
{
	private static void Postfix(PotionModel __instance, ref LocString __result)
	{
		if (__instance is not FirePotion || !NeutralPotionModificationPolicy.ShouldApply(__instance))
		{
			return;
		}

		__result = new LocString("potions", "SUNQIAN_UNIVERSE_FIRE_POTION_MODIFIED.description");
		__result.Add("Damage", FirePotionBurningPatch.DamageAmount);
		__result.Add(nameof(BurningPower), FirePotionBurningPatch.BurningAmount);
	}
}
