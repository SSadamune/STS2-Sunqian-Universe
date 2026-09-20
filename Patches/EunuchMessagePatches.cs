using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 「小黄门的口信」视为保留，但不在卡面上写出原版「保留」词条。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_ShouldRetainThisTurn")]
internal static class EunuchMessageRetainPatch
{
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (!__result && __instance.HasEunuchMessage())
		{
			__result = true;
		}
	}
}

/// <summary>
/// 口信会使 <see cref="CardModel.ShouldRetainThisTurn"/> 为真，原版会因此在卡面插入「保留」。
/// 牌本身没有保留词条时，从描述里去掉这行。打在实际拼卡面的三参数方法上，
/// 以便在注入「小黄门的口信」之前先清掉误加的保留。
/// </summary>
[HarmonyPatch]
internal static class EunuchMessageHideVanillaRetainTextPatch
{
	private static MethodBase TargetMethod()
	{
		Type previewType = typeof(CardModel).GetNestedType("DescriptionPreviewType", BindingFlags.NonPublic)
			?? throw new MissingMemberException(typeof(CardModel).FullName, "DescriptionPreviewType");
		return AccessTools.Method(
			typeof(CardModel),
			"GetDescriptionForPile",
			[typeof(PileType), previewType, typeof(Creature)])
			?? throw new MissingMethodException(typeof(CardModel).FullName, "GetDescriptionForPile");
	}

	[HarmonyPostfix]
	[HarmonyPriority(Priority.First)]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (string.IsNullOrEmpty(__result)
			|| !__instance.HasEunuchMessage()
			|| __instance.Keywords.Contains(CardKeyword.Retain))
		{
			return;
		}

		string retainTitle = new LocString("card_keywords", "RETAIN.title").GetFormattedText() ?? "";
		if (retainTitle.Length == 0)
		{
			return;
		}

		string retainMarker = "[gold]" + retainTitle + "[/gold]";
		string[] kept = __result.Split('\n')
			.Where(line => !line.StartsWith(retainMarker, StringComparison.Ordinal))
			.ToArray();
		__result = string.Join('\n', kept);
	}
}

/// <summary>
/// 离开手牌之前清掉口信：先移除词条，再把牌移出手里。
/// 这样打出该牌后若剧本在结算中失效，正在结算的牌不会被消耗。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.RemoveFromCurrentPile))]
internal static class EunuchMessageClearBeforeLeaveHandPatch
{
	private static void Prefix(CardModel __instance)
	{
		if (__instance.Pile?.Type == PileType.Hand)
		{
			ScriptEunuchPower.ClearMarkIfPresent(__instance);
		}
	}
}

/// <summary>
/// 口信描述提到保留：悬停口信后再跟一条原版保留提示（同虚无→消耗），卡面仍不写保留。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
[HarmonyPriority(int.MinValue)]
internal static class EunuchMessageRetainHoverTipPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (!__instance.HasEunuchMessage())
		{
			return;
		}

		IHoverTip retainTip = HoverTipFactory.FromKeyword(CardKeyword.Retain);
		List<IHoverTip> tips = __result.ToList();
		if (tips.Contains(retainTip))
		{
			return;
		}

		IHoverTip messageTip = HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage);
		int messageIndex = tips.IndexOf(messageTip);
		if (messageIndex >= 0)
		{
			tips.Insert(messageIndex + 1, retainTip);
		}
		else
		{
			tips.Add(retainTip);
		}

		__result = tips;
	}
}
