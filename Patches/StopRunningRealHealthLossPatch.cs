#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Squ.Powers;

namespace Squ.Patches;

[HarmonyPatch]
internal static class StopRunningRealHealthLossPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.LoseHpInternal));
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.SetCurrentHpInternal));
	}
	private static void Prefix(Creature __instance, out (int Hp, int Temporary) __state) =>
		__state = (__instance.CurrentHp, __instance.GetPower<TemporaryHealthPower>()?.Amount ?? 0);
	private static void Postfix(Creature __instance, (int Hp, int Temporary) __state)
	{
		int loss = Math.Max(0, __state.Hp - __instance.CurrentHp - __state.Temporary);
		__instance.GetPower<StopRunningPower>()?.RecordRealLoss(loss);
		__instance.GetPower<StopFleeingUntilEnemyTurnEndPower>()?.RecordRealLoss(loss);
	}
}
