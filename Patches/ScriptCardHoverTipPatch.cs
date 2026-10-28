using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>在剧本牌上追加对应“剧本：XXX”衍生牌的完整卡面提示。</summary>
[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class ScriptCardHoverTipPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (__instance is not ScriptCardTemplate script)
		{
			return;
		}

		foreach (ScriptRecordCardTemplate record in script.CreateScriptRecordPreviews())
		{
			__result = __result.Append(new CardHoverTip(record)).Concat(record.HoverTips);
		}
	}
}
