using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 「以战养战」描述依赖 <c>{CardName}</c>，需卡牌上下文才能格式化。
/// RitsuLib 的 <c>HoverTipFactory.FromKeyword</c> 补丁会无上下文调用
/// <c>ModKeywordRegistry.CreateHoverTip</c>，导致 SmartFormat 报错。
/// 卡牌侧提示改由 <see cref="SquKeywords.CreateWarFeedsWarHoverTip"/> 提供。
/// </summary>
[HarmonyPatch]
internal static class WarFeedsWarSuppressGenericKeywordHoverTipPatch
{
	private static MethodBase TargetMethod()
	{
		var patchType = AccessTools.TypeByName("STS2RitsuLib.Keywords.Patches.HoverTipFactoryFromKeywordPatch")
			?? throw new InvalidOperationException("HoverTipFactoryFromKeywordPatch not found.");
		return AccessTools.Method(patchType, "Prefix")
			?? throw new MissingMethodException(patchType.FullName, "Prefix");
	}

	[HarmonyPrefix]
	private static bool Prefix(CardKeyword keyword)
	{
		return keyword != SquKeywords.WarFeedsWar;
	}
}

[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class WarFeedsWarEnsureCardHoverTipPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (!__instance.Keywords.Contains(SquKeywords.WarFeedsWar))
		{
			return;
		}

		__result =
		[
			.. __result,
			SquKeywords.CreateWarFeedsWarHoverTip(__instance),
		];
	}
}
