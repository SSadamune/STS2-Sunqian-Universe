using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Combat;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class WarFeedsWarCombatEndPatch
{
	private static void Postfix(CombatRoom __2) =>
		WarFeedsWarResolutionTracker.TryOfferCombatRewards(__2);
}
