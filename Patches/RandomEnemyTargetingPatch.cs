using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;
using STS2RitsuLib.Combat.CardTargeting;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(CardModelTargetingExtensions), nameof(CardModelTargetingExtensions.GetTargets))]
internal static class RandomEnemyTargetingPatch
{
	private static void Postfix(CardModel card, Creature? selectedTarget, ref List<Creature> __result)
	{
		if (!SquRandomEnemyTargeting.UsesRandomEnemiesTargeting(card))
		{
			return;
		}

		__result = SquRandomEnemyTargeting.GetTargets(card, selectedTarget);
	}
}
