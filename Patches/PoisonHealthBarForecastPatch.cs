using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Combat;

namespace Squ.Patches;

/// <summary>
/// Makes vanilla's poison health-bar segment share defensive turn-start resources with Burning.
/// Non-UI mechanics that intentionally use the old poison forecast call the legacy helper directly.
/// </summary>
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
internal static class PoisonHealthBarForecastPatch
{
	[HarmonyPostfix]
	private static void ApplyDefensivePowerForecast(PoisonPower __instance, ref int __result)
	{
		__result = SquTurnStartDamageForecast.Calculate(__instance.Owner).PoisonDamage;
	}
}
