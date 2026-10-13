using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Interop;
using Squ.Potions;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(PotionModel), "get_DynamicDescription")]
internal static class PeiguoBrewNewsanguoNotePatch
{
	private static void Postfix(PotionModel __instance, LocString __result)
	{
		if (__instance is not PeiguoBrewPotion)
		{
			return;
		}

		NewsanguoVigorSwap.AddCombatNote(__result, __instance);
	}
}
