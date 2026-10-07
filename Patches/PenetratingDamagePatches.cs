using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Piercing damage keeps the normal powered-attack calculation, but ignores the listed
/// target-side multiplicative defenses.
/// </summary>
[HarmonyPatch]
internal static class PenetratingDamageMultiplierPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return Method<CoveredPower>(nameof(CoveredPower.ModifyDamageMultiplicative));
		yield return Method<GuardedPower>(nameof(GuardedPower.ModifyDamageMultiplicative));
		yield return Method<SoarPower>(nameof(SoarPower.ModifyDamageMultiplicative));
		yield return Method<FlutterPower>(nameof(FlutterPower.ModifyDamageMultiplicative));
		yield return Method<ColossusPower>(nameof(ColossusPower.ModifyDamageMultiplicative));
	}

	private static bool Prefix(
		ValueProp props,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!SquPenetratingDamage.IsPenetrating(props, cardSource))
		{
			return true;
		}

		__result = 1m;
		return false;
	}

	private static MethodInfo Method<T>(string name) =>
		AccessTools.DeclaredMethod(typeof(T), name)
		?? throw new MissingMethodException(typeof(T).FullName, name);
}

/// <summary>Skips damage caps from Hard to Kill and Intangible.</summary>
[HarmonyPatch]
internal static class PenetratingDamageCapPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return Method<HardToKillPower>(nameof(HardToKillPower.ModifyDamageCap));
		yield return Method<IntangiblePower>(nameof(IntangiblePower.ModifyDamageCap));
	}

	private static bool Prefix(
		ValueProp props,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!SquPenetratingDamage.IsPenetrating(props, cardSource))
		{
			return true;
		}

		__result = decimal.MaxValue;
		return false;
	}

	private static MethodInfo Method<T>(string name) =>
		AccessTools.DeclaredMethod(typeof(T), name)
		?? throw new MissingMethodException(typeof(T).FullName, name);
}

/// <summary>
/// Skips the HP-loss limits from Buffer, Intangible, Slippery, and Hardened Shell.
/// Hardened Shell still observes the resulting damage in AfterDamageReceived, so the
/// penetrating hit continues to count toward its per-turn allowance.
/// </summary>
[HarmonyPatch]
internal static class PenetratingDamageHpLossPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return Method<BufferPower>(nameof(BufferPower.ModifyHpLostAfterOstyLate));
		yield return Method<IntangiblePower>(nameof(IntangiblePower.ModifyHpLostAfterOsty));
		yield return Method<SlipperyPower>(nameof(SlipperyPower.ModifyHpLostAfterOsty));
		yield return Method<HardenedShellPower>(nameof(HardenedShellPower.ModifyHpLostBeforeOstyLate));
	}

	private static bool Prefix(
		decimal amount,
		ValueProp props,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!SquPenetratingDamage.IsPenetrating(props, cardSource))
		{
			return true;
		}

		__result = amount;
		return false;
	}

	private static MethodInfo Method<T>(string name) =>
		AccessTools.DeclaredMethod(typeof(T), name)
		?? throw new MissingMethodException(typeof(T).FullName, name);
}

/// <summary>
/// Slippery spends a stack from AfterDamageReceived rather than from its HP-loss modifier.
/// Suppress that trigger for piercing damage; Buffer naturally spends no stack because its
/// skipped modifier is not included in Hook.ModifyHpLost's modifier list.
/// </summary>
[HarmonyPatch(typeof(SlipperyPower), nameof(SlipperyPower.AfterDamageReceived))]
internal static class PenetratingDamageSlipperyConsumptionPatch
{
	private static bool Prefix(
		ValueProp props,
		CardModel? cardSource,
		ref Task __result)
	{
		if (!SquPenetratingDamage.IsPenetrating(props, cardSource))
		{
			return true;
		}

		__result = Task.CompletedTask;
		return false;
	}
}
