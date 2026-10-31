using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Interop;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 上将军的说明在战斗内追加「与酒力」；不在战斗中时，带这个关键词的牌改为显示联动悬停提示。
/// </summary>
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword), typeof(CardKeyword))]
internal static class SupremeGeneralKeywordDescriptionPatch
{
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(CardKeyword keyword, ref IHoverTip __result)
	{
		if (keyword != SquKeywords.SupremeGeneral
			|| NewsanguoDrunkenMightEffect.OrText().Length == 0)
		{
			return;
		}

		// 关键词原文不能带未声明的占位符，否则 RitsuLib 在格式化时会直接报错。
		// 需要追加「与酒力」时，改用单独的说明。
		LocString description = new(
			"card_keywords",
			"SUNQIAN_UNIVERSE_KEYWORD_SUPREME_GENERAL.descriptionWithDrunkenMight");
		NewsanguoDrunkenMightEffect.AddOrText(description);
		__result = new HoverTip(ModKeywordRegistry.GetTitle(SquKeywords.SupremeGeneralId), description);
	}
}

[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class SupremeGeneralDrunkenMightHoverTipPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (!__instance.Keywords.Contains(SquKeywords.SupremeGeneral))
		{
			return;
		}

		IHoverTip[] tips = [.. NewsanguoDrunkenMightEffect.HoverTips()];
		if (tips.Length == 0)
		{
			return;
		}

		__result = [.. __result, .. tips];
	}
}
