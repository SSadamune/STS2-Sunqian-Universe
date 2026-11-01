using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Squ.Combat;
using Squ.Interop;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Coordinates Poison's UI-only forecast with Burning. When both debuffs share an
/// enemy turn-start batch, the dedicated vanilla Poison overlay is replaced by an
/// ordered RitsuLib segment; otherwise the optional defense-aware result is used
/// when available and the vanilla result is retained when it is not.
/// </summary>
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
internal static class PoisonHealthBarForecastPatch
{
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void CoordinateOptionalDebuffPreview(PoisonPower __instance, ref int __result)
	{
		if (!PoisonHealthBarPreviewScopePatch.IsActive)
		{
			return;
		}

		if (OrderedPoisonHealthBarForecastSource.UsesSharedEnemyTurnStartSequence(
			__instance.Owner))
		{
			// RitsuLib renders Poison and Burning together as ordered custom segments.
			// Suppress only the otherwise-duplicated vanilla Poison overlay.
			__result = 0;
			return;
		}

		if (DefenseAwareDebuffPreviewInterop.TryGetPreviewHpLoss(
				__instance.Owner,
				__instance,
				out int hpLoss))
		{
			__result = hpLoss;
		}
	}

	/// <summary>
	/// Invokes the unpatched game implementation for the UI fallback. This avoids
	/// another mod's Poison patch leaking into the optional-API failure path.
	/// </summary>
	[HarmonyReversePatch]
	[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int CalculateVanillaPreview(PoisonPower instance) =>
		throw new NotSupportedException("Harmony reverse patch was not applied.");
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
