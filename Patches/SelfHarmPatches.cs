#nullable enable
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;

namespace Squ.Patches;

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers))]
internal static class SelfHarmEnergyCostPatch
{
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(CardModel ____card, CostModifiers modifiers, ref int __result)
	{
		if (modifiers.HasFlag(CostModifiers.Local) && SelfHarmSystem.IsActive(____card)) __result = 0;
	}
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class SelfHarmPaymentSnapshotPatch
{
	private static void Prefix(CardModel __instance) => SelfHarmSystem.PreparePayment(__instance);
}

[HarmonyPatch(typeof(CardCostHelper), nameof(CardCostHelper.GetEnergyCostColor))]
internal static class SelfHarmCostColorPatch
{
	private static void Postfix(CardModel card, ref CardCostColor __result)
	{
		if (SelfHarmSystem.IsActive(card)) __result = CardCostColor.Decreased;
	}
}

// Re-evaluate both the displayed zero and the color immediately when HP changes.
[HarmonyPatch(typeof(Creature), nameof(Creature.CurrentHp), MethodType.Setter)]
internal static class SelfHarmHpDisplayRefreshPatch
{
	private static void Postfix(Creature __instance)
	{
		var cards = __instance.Player?.PlayerCombatState?.AllCards;
		if (cards == null) return;
		foreach (CardModel card in cards.Where(card => card is ISelfHarmCard).ToArray())
			card.InvokeEnergyCostChanged();
	}
}
