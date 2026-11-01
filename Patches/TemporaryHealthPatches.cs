#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Squ.Powers;

namespace Squ.Patches;

/// <summary>Keep the native HP field and its events, extending only its healing cap.</summary>
[HarmonyPatch]
internal static class TemporaryHealthCapPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.SetCurrentHpInternal));
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.SetMaxHpInternal));
		// Heal's history accounting must use the same cap as HealInternal.
		MethodInfo heal = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Heal));
		yield return AccessTools.Method(
			heal.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType, "MoveNext");
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo maxHp = AccessTools.PropertyGetter(typeof(Creature), nameof(Creature.MaxHp));
		MethodInfo cap = AccessTools.Method(typeof(TemporaryHealthCapPatches), nameof(GetHealthCap));
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(maxHp))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = cap;
			}
			yield return instruction;
		}
	}

	internal static int GetHealthCap(Creature creature) =>
		(int)Math.Min(int.MaxValue, (long)creature.MaxHp
			+ Math.Max(0, creature.GetPower<TemporaryHealthPower>()?.Amount ?? 0));
}

/// <summary>Damage results retain the full HP loss, including temporary HP.</summary>
[HarmonyPatch]
internal static class TemporaryHealthConsumptionPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.LoseHpInternal));
		yield return AccessTools.Method(typeof(Creature), nameof(Creature.SetCurrentHpInternal));
	}

	private static void Prefix(Creature __instance, out int __state) =>
		__state = __instance.CurrentHp;

	private static void Postfix(Creature __instance, int __state) =>
		__instance.GetPower<TemporaryHealthPower>()?.Consume(__state - __instance.CurrentHp);
}

/// <summary>Removing the buff, including combat cleanup, discards its remaining HP.</summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.RemoveInternal))]
internal static class TemporaryHealthRemovalPatch
{
	private static void Postfix(PowerModel __instance)
	{
		if (__instance is TemporaryHealthPower power && power.Amount > 0)
		{
			Creature owner = power.Owner;
			int remaining = power.Amount;
			power.SetAmount(0);
			owner.SetCurrentHpInternal(Math.Max(0, owner.CurrentHp - remaining));
		}
	}
}

// Full-heal callers sometimes pass MaxHp - CurrentHp. This becomes negative
// above max HP; healing must never consume temporary HP in that case.
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
internal static class TemporaryHealthHealingPatch
{
	private static void Prefix(Creature creature, ref decimal amount)
	{
		if (creature.HasPower<TemporaryHealthPower>())
		{
			amount = Math.Max(0m, amount);
		}
	}
}

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
internal static class TemporaryHealthDecayLimitPatch
{
	private static void Prefix(Creature __instance, ref decimal amount) =>
		__instance.GetPower<TemporaryHealthPower>()?.LimitTemporaryLoss(ref amount);
}

// Native bars assume CurrentHp <= MaxHp. Keep the total in the numeric label,
// with stacks in the power badge, while preventing the bar from overflowing.
[HarmonyPatch(typeof(NHealthBar), "GetFgWidth", new[] { typeof(int), typeof(float) })]
internal static class TemporaryHealthBarWidthPatch
{
	private static void Postfix(Creature? ____creature, float maxFgWidth, ref float __result)
	{
		if (____creature?.HasPower<TemporaryHealthPower>() == true)
		{
			__result = Math.Min(__result, maxFgWidth);
		}
	}
}
