using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Interop;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(PowerModel), "get_SmartDescription")]
internal static class GrandGobletNewsanguoNotePatch
{
	private static void Postfix(PowerModel __instance, LocString __result)
	{
		if (__instance is not GrandGobletPower)
		{
			return;
		}

		NewsanguoVigorSwap.AddCombatPowerNote(__result, __instance);
	}
}
