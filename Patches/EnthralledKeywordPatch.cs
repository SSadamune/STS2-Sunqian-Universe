using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Combat;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ShouldPlay))]
internal static class EnthralledKeywordShouldPlayPatch
{
	private static void Postfix(
		AbstractModel __instance,
		CardModel card,
		AutoPlayType autoPlayType,
		ref bool __result)
	{
		if (!__result || __instance is not CardModel listener)
		{
			return;
		}

		if (!listener.Keywords.Contains(SquKeywords.Enthralled))
		{
			return;
		}

		__result = EnthralledKeyword.AllowsPlay(listener, card, autoPlayType);
	}
}

/// <summary>
/// 不改诅咒源码：手牌里同时有词条牌时，让原版「执迷」把带词条的牌视为同类。
/// </summary>
[HarmonyPatch(typeof(Enthralled), nameof(Enthralled.ShouldPlay))]
internal static class EnthralledCurseAllowsKeywordCardsPatch
{
	private static void Postfix(CardModel card, ref bool __result)
	{
		if (__result)
		{
			return;
		}

		if (card.Keywords.Contains(SquKeywords.Enthralled))
		{
			__result = true;
		}
	}
}

[HarmonyPatch(typeof(CardModel), "get_ShouldGlowRedInternal")]
internal static class EnthralledKeywordGlowPatch
{
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (__result || !__instance.Keywords.Contains(SquKeywords.Enthralled))
		{
			return;
		}

		if (__instance.Pile?.Type == PileType.Hand)
		{
			__result = true;
		}
	}
}
