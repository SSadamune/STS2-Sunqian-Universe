using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 卡面不写「蓄能」，图鉴搜索只匹配标题和描述。把「蓄能」/「charge」注册进原版
/// <c>_specialSearchbarKeywords</c>，搜索整词时仍能筛出带蓄能关键词的牌。
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
internal static class ChargeCardLibrarySearchPatch
{
	private static readonly FieldInfo SpecialSearchKeywordsField =
		AccessTools.Field(typeof(NCardLibrary), "_specialSearchbarKeywords")
		?? throw new InvalidOperationException("NCardLibrary._specialSearchbarKeywords is missing.");

	private static void Postfix(NCardLibrary __instance)
	{
		var keywords = SpecialSearchKeywordsField.GetValue(__instance) as Dictionary<string, Func<CardModel, bool>>
			?? throw new InvalidOperationException("NCardLibrary._specialSearchbarKeywords has unexpected type.");

		static bool HasCharge(CardModel card) => card.Keywords.Contains(SquKeywords.Charge);

		keywords.TryAdd("蓄能", HasCharge);
		keywords.TryAdd("charge", HasCharge);

		string localized = SquKeywords.Charge.GetModKeywordTitle().GetFormattedText() ?? "";
		if (localized.Length > 0)
		{
			keywords.TryAdd(localized.ToLowerInvariant(), HasCharge);
		}
	}
}
