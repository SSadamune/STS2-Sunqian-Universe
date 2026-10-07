using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// In multiplayer, a card node can temporarily lose its parent while its action is paused
/// for an in-hand card choice. The vanilla resume path always calls Node.Reparent, which
/// rejects orphaned nodes and then leaves the play queue in an invalid visual state.
/// Keep the vanilla behavior for parented nodes and attach orphaned nodes directly.
/// This patch changes presentation nodes only; combat models and synchronized actions are
/// untouched.
/// </summary>
[HarmonyPatch]
internal static class MultiplayerCardChoiceVisualPatch
{
	private static readonly MethodInfo ReparentMethod = AccessTools.Method(
		typeof(Node),
		nameof(Node.Reparent),
		[typeof(Node), typeof(bool)])
		?? throw new MissingMethodException(typeof(Node).FullName, nameof(Node.Reparent));

	private static readonly MethodInfo ReparentOrAttachMethod = AccessTools.Method(
		typeof(MultiplayerCardChoiceVisualPatch),
		nameof(ReparentOrAttach))
		?? throw new MissingMethodException(
			typeof(MultiplayerCardChoiceVisualPatch).FullName,
			nameof(ReparentOrAttach));

	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.DeclaredMethod(
			typeof(NCardPlayQueue),
			nameof(NCardPlayQueue.ReAddCardAfterPlayerChoice))
			?? throw new MissingMethodException(
				typeof(NCardPlayQueue).FullName,
				nameof(NCardPlayQueue.ReAddCardAfterPlayerChoice));

		yield return AccessTools.DeclaredMethod(
			typeof(NCardPlayQueue),
			"BeforeRemoteCardPlayResumedAfterPlayerChoice")
			?? throw new MissingMethodException(
				typeof(NCardPlayQueue).FullName,
				"BeforeRemoteCardPlayResumedAfterPlayerChoice");
	}

	private static IEnumerable<CodeInstruction> Transpiler(
		IEnumerable<CodeInstruction> instructions)
	{
		int replacementCount = 0;
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(ReparentMethod))
			{
				replacementCount++;
				yield return new CodeInstruction(OpCodes.Call, ReparentOrAttachMethod)
					.MoveLabelsFrom(instruction)
					.MoveBlocksFrom(instruction);
				continue;
			}

			yield return instruction;
		}

		if (replacementCount == 0)
		{
			throw new InvalidOperationException(
				"Could not find NCardPlayQueue's Node.Reparent call.");
		}
	}

	private static void ReparentOrAttach(
		Node node,
		Node newParent,
		bool keepGlobalTransform)
	{
		if (!GodotObject.IsInstanceValid(node)
			|| !GodotObject.IsInstanceValid(newParent))
		{
			return;
		}

		if (node.GetParent() is null)
		{
			newParent.AddChild(node);
			return;
		}

		node.Reparent(newParent, keepGlobalTransform);
	}
}
