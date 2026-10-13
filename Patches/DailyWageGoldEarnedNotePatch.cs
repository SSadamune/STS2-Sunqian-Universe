using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ;
using Squ.Relics;
using Squ.RunData;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 《日结工资》累计金币只在对局中的可变实例上显示，遗物收集等图鉴不显示。
/// </summary>
[HarmonyPatch(typeof(RelicModel), "get_DynamicDescription")]
internal static class DailyWageGoldEarnedNotePatch
{
	private static void Postfix(RelicModel __instance, LocString __result)
	{
		if (__instance is not DailyWageRelic relic)
		{
			return;
		}

		if (!ShouldShowGoldEarned(relic))
		{
			__result.Add("GoldEarnedLine", string.Empty);
			return;
		}

		DailyWageRunData.SyncRelic(relic);
		LocString line = new("relics", relic.Id.Entry + ".goldEarnedLine");
		relic.DynamicVars.AddTo(line);
		SquKeywords.AddNestedLoc(__result, "GoldEarnedLine", line);
	}

	private static bool ShouldShowGoldEarned(DailyWageRelic relic) =>
		relic.IsMutable && RunManager.Instance is { IsInProgress: true };
}
