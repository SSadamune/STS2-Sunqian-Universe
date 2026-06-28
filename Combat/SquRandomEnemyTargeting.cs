using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using Squ.Powers;
using STS2RitsuLib.Combat.CardTargeting;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Shared helpers for picking distinct random enemy targets.
/// </summary>
public static class SquRandomEnemyTargeting
{
	public static IEnumerable<Creature> PickRandomEnemiesUnique(
		ICombatState combatState,
		int count,
		Rng rng)
	{
		if (count <= 0)
		{
			yield break;
		}

		List<Creature> alive = combatState.HittableEnemies
			.Where(creature => creature.IsAlive)
			.ToList();
		if (alive.Count == 0)
		{
			yield break;
		}

		int pickCount = Math.Min(count, alive.Count);
		List<Creature> pool = alive.ToList();

		for (int i = pool.Count - 1; i > 0; i--)
		{
			int swapIndex = rng.NextInt(0, i + 1);
			(pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
		}

		foreach (Creature creature in pool.Take(pickCount))
		{
			yield return creature;
		}
	}

	public static bool UsesRandomEnemiesTargeting(CardModel card) =>
		SquTargetTypes.IsRandomEnemiesTarget(card.TargetType);

	public static int GetRandomEnemyTargetCount(CardModel card)
	{
		if (!UsesRandomEnemiesTargeting(card))
		{
			return 0;
		}

		if (ChickenFootCheeseStrikePower.ShouldRedirectBasicStrike(card))
		{
			return ChickenFootCheeseStrikePower.RedirectRandomEnemyCount;
		}

		if (card is IRandomEnemyTargetCount provider)
		{
			return provider.GetRandomEnemyTargetCount();
		}

		return 0;
	}

	public static List<Creature> GetTargets(CardModel card, Creature? selectedTarget)
	{
		if (!UsesRandomEnemiesTargeting(card))
		{
			return CardModelTargetingExtensions.GetTargets(card, selectedTarget);
		}

		ICombatState? combatState = card.CombatState;
		if (combatState == null)
		{
			return [];
		}

		return PickRandomEnemiesUnique(
			combatState,
			GetRandomEnemyTargetCount(card),
			card.Owner.RunState.Rng.CombatTargets).ToList();
	}
}
