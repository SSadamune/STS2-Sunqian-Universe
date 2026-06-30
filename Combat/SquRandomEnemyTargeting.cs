using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

	public static int GetEffectiveRandomEnemyHitCount(ICombatState combatState, int requestedHitCount)
	{
		if (requestedHitCount <= 0)
		{
			return 0;
		}

		int aliveCount = combatState.HittableEnemies.Count(creature => creature.IsAlive);
		return Math.Min(requestedHitCount, aliveCount);
	}

	/// <summary>
	/// One <see cref="AttackCommand"/> with distinct random targets per hit so card-sourced
	/// bonuses (e.g. Vigor) apply to every hit before <c>AfterAttack</c> consumes them.
	/// </summary>
	public static async Task<int> ExecuteDistinctRandomEnemyDamage(
		CardModel card,
		PlayerChoiceContext choiceContext,
		int requestedHitCount,
		decimal? damagePerHit = null,
		string hitFx = "vfx/vfx_attack_slash")
	{
		ICombatState? combatState = card.CombatState;
		if (combatState == null || requestedHitCount <= 0)
		{
			return 0;
		}

		int hitCount = GetEffectiveRandomEnemyHitCount(combatState, requestedHitCount);
		if (hitCount <= 0)
		{
			return 0;
		}

		await DamageCmd.Attack(damagePerHit ?? card.DynamicVars.Damage.BaseValue)
			.FromCard(card)
			.TargetingRandomOpponents(combatState, allowDuplicates: false)
			.WithHitCount(hitCount)
			.WithHitFx(hitFx)
			.Execute(choiceContext);

		return hitCount;
	}
}
