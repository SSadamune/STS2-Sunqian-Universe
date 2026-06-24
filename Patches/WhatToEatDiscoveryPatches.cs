using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using Squ.Script;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.FromSerializable))]
internal static class WhatToEatDiscoveryProgressLoadPatch
{
	private static void Postfix(ProgressState __result)
	{
		WhatToEatDiscoverySync.SyncFromSoloDiscovery(__result);
	}
}

[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.MarkCardAsSeen))]
internal static class WhatToEatDiscoveryMarkSeenPatch
{
	private static void Postfix(ProgressState __instance, ModelId cardId)
	{
		WhatToEatDiscoverySync.OnCardMarkedAsSeen(__instance, cardId);
	}
}

[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
internal static class WhatToEatDiscoveryCardLibraryRefreshPatch
{
	private static void Prefix()
	{
		ProgressState? progress = SaveManager.Instance?.Progress;
		if (progress is not null)
		{
			WhatToEatDiscoverySync.SyncFromSoloDiscovery(progress);
		}
	}
}
