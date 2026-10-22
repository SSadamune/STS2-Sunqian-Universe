using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Squ.Interop;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// When DefenseAwareDebuffPreview is loaded, keeps its Poison segment coordinated
/// with Burning so both sources share the same ordered defensive-state simulation.
/// Without that optional mod, the vanilla result is left untouched.
/// </summary>
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
internal static class PoisonHealthBarForecastPatch
{
	[HarmonyPostfix]
	private static void CoordinateOptionalDebuffPreview(PoisonPower __instance, ref int __result)
	{
		if (PoisonHealthBarPreviewScopePatch.IsActive
			&& DefenseAwareDebuffPreviewInterop.TryGetPreviewHpLoss(
			__instance.Owner,
			__instance,
			out int hpLoss))
		{
			__result = hpLoss;
		}
	}
}

/// <summary>
/// Restricts the Poison postfix to the vanilla health-bar rendering paths.
/// Gameplay calls to CalculateTotalDamageNextTurn retain the vanilla result.
/// </summary>
[HarmonyPatch]
internal static class PoisonHealthBarPreviewScopePatch
{
	[ThreadStatic]
	private static int _depth;

	internal static bool IsActive => _depth > 0;

	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (string methodName in new[] { "RefreshForeground", "RefreshText" })
		{
			MethodInfo? method = AccessTools.DeclaredMethod(typeof(NHealthBar), methodName);
			if (method is null)
			{
				SquMod.Logger.Error(
					$"Could not find NHealthBar.{methodName}; coordinated Poison preview will be unavailable on that path.");
				continue;
			}

			yield return method;
		}
	}

	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	private static void EnterPreviewScope() => _depth++;

	[HarmonyFinalizer]
	[HarmonyPriority(Priority.Last)]
	private static Exception? ExitPreviewScope(Exception? __exception)
	{
		_depth = Math.Max(0, _depth - 1);
		return __exception;
	}
}
