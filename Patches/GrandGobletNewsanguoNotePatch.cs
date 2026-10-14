using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Interop;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(PowerModel), "get_SmartDescription")]
internal static class InteropPowerDescriptionPatch
{
	private static void Postfix(PowerModel __instance, LocString __result)
	{
		switch (__instance)
		{
			case GrandGobletPower:
				NewsanguoVigorSwap.AddCombatPowerCondition(__result, __instance);
				break;
			case GoodFirePower:
				YjcmGoodFireInterop.AddCombatCondition(__result, __instance);
				break;
		}
	}
}
