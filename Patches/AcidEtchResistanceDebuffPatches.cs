#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;

namespace Squ.Patches;

/// <summary>
/// 《酸蚀抗性》保留易伤、虚弱、脆弱本身及其正常获得和倒计时，
/// 但让这些原版能力的数值倍率直接变为 1。
/// </summary>
internal static class AcidEtchResistanceDebuffPatches
{
	public static bool IgnoreWhenProtected(PowerModel debuff, ref decimal result)
	{
		if (debuff.Owner.GetPower<AcidEtchResistancePower>() is not { Amount: > 0 })
		{
			return true;
		}

		result = 1m;
		return false;
	}
}

[HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
internal static class AcidEtchResistanceWeakPatch
{
	private static bool Prefix(WeakPower __instance, ref decimal __result) =>
		AcidEtchResistanceDebuffPatches.IgnoreWhenProtected(__instance, ref __result);
}

[HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
internal static class AcidEtchResistanceVulnerablePatch
{
	private static bool Prefix(VulnerablePower __instance, ref decimal __result) =>
		AcidEtchResistanceDebuffPatches.IgnoreWhenProtected(__instance, ref __result);
}

[HarmonyPatch(typeof(FrailPower), nameof(FrailPower.ModifyBlockMultiplicative))]
internal static class AcidEtchResistanceFrailPatch
{
	private static bool Prefix(FrailPower __instance, ref decimal __result) =>
		AcidEtchResistanceDebuffPatches.IgnoreWhenProtected(__instance, ref __result);
}
