#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Squ.Monsters;

namespace Squ.Patches;

// Only visual classes are patched: gameplay keeps Second Brother's distinct identity.
[HarmonyPatch]
internal static class SecondBrotherPresentationPatches
{
	internal static MonsterModel? AsOstyPresentation(MonsterModel? monster) =>
		monster is Osty or SecondBrother ? monster : null;
	private static IEnumerable<Type> Types(Type type) => new[] { type }
		.Concat(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(Types));
	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (Type type in Types(typeof(NCombatRoom)).Concat(Types(typeof(NCreature))))
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			if (method.ContainsGenericParameters || method.GetMethodBody() == null) continue;
			if (PatchProcessor.GetOriginalInstructions(method).Any(IsOstyCheck)) yield return method;
		}
	}
	private static bool IsOstyCheck(CodeInstruction instruction) =>
		instruction.opcode == OpCodes.Isinst && Equals(instruction.operand, typeof(Osty));
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (IsOstyCheck(instruction))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = AccessTools.Method(typeof(SecondBrotherPresentationPatches), nameof(AsOstyPresentation));
			}
			yield return instruction;
		}
	}
}
