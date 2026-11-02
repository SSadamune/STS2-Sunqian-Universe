#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Combat;

namespace Squ.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class GrantedSelfHarmPaymentPatch
{
	private static void Postfix(CardPlay cardPlay, ref Task __result) => __result = Resolve(__result, cardPlay);
	private static async Task Resolve(Task original, CardPlay play)
	{
		await original;
		if (play.Card is not SelfHarmCardTemplate && SelfHarmSystem.HasSelfHarm(play.Card))
			await SelfHarmSystem.ResolvePayment(new BlockingPlayerChoiceContext(), play.Card, play);
	}
}

[HarmonyPatch]
internal static class GrantedSelfHarmDescriptionPatch
{
	private static System.Reflection.MethodBase TargetMethod() => typeof(CardModel).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
		.Single(method => method.Name == "GetDescriptionForPile" && method.GetParameters().Length == 3);
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (__instance is SelfHarmCardTemplate || !SelfHarmSystem.HasSelfHarm(__instance)) return;
		LocString clause = new("card_keywords", "SUNQIAN_UNIVERSE_SELF_HARM.cardText");
		clause.AddObj("SelfHarm", new DynamicVar("SelfHarm", SelfHarmSystem.GetAmount(__instance)));
		__result = clause.GetFormattedText() + __result;
	}
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
internal static class GrantedSelfHarmHoverPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (__instance is SelfHarmCardTemplate || !SelfHarmSystem.HasSelfHarm(__instance)) return;
		__result = __result.Append(SquKeywords.CreateSelfHarmHoverTip(new DynamicVar("SelfHarm", SelfHarmSystem.GetAmount(__instance))));
	}
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard), "UpdateEnergyCostVisuals")]
internal static class SelfHarmXCostDisplayPatch
{
	private static void Postfix(MegaCrit.Sts2.Core.Nodes.Cards.NCard __instance,
		MegaCrit.Sts2.addons.mega_text.MegaLabel ____energyLabel)
	{
		if (__instance.Visibility == MegaCrit.Sts2.Core.Entities.UI.ModelVisibility.Visible
			&& __instance.Model is { EnergyCost.CostsX: true } card && SelfHarmSystem.IsActive(card))
			____energyLabel.SetTextAutoSize("0");
	}
}
