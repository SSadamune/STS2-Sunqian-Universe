using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;

#nullable enable

namespace Squ.Patches;

/// <summary>Displays combat-granted Slight Revision data on any card model.</summary>
[HarmonyPatch]
internal static class SlightRevisionCardDescriptionPatch
{
	// Both normal card faces and upgrade previews ultimately pass through this
	// private overload.  Patching the public wrapper misses the renderer's path
	// in the current game build.
	[HarmonyTargetMethod]
	private static MethodBase TargetDescriptionMethod()
	{
		return AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(method =>
			method.Name == nameof(CardModel.GetDescriptionForPile) && method.GetParameters().Length == 3);
	}

	[HarmonyPostfix]
	private static void AddGrantedRevisionDescription(CardModel __instance, ref string __result)
	{
		__result += SlightRevisionSystem.GetGrantedDescriptionSuffix(__instance);
	}
}


[HarmonyPatch]
internal static class SlightRevisionCardHoverTipsPatch
{
	[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
	[HarmonyPostfix]
	private static void AddGrantedRevisionHoverTips(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		IEnumerable<IHoverTip> revisionTips = SlightRevisionSystem.GetGrantedHoverTips(__instance);
		if (revisionTips.Any())
		{
			__result = __result.Concat(revisionTips);
		}
	}
}
