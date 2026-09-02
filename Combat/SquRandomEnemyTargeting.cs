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

		return SelectRandomEnemies(card, GetRandomEnemyTargetCount(card));
	}

	/// <summary>
	/// 为一项随机多目标效果选择一次目标集合。调用方必须在该效果的后续结算中复用返回的集合。
	/// </summary>
	public static List<Creature> SelectRandomEnemies(CardModel card, int requestedTargetCount)
	{
		ICombatState? combatState = card.CombatState;
		if (combatState == null || requestedTargetCount <= 0)
		{
			return [];
		}

		return PickRandomEnemiesUnique(
			combatState,
			requestedTargetCount,
			card.Owner.RunState.Rng.CombatTargets).ToList();
	}

	/// <summary>
	/// 用一条 <see cref="AttackCommand"/> 对一次选定的随机目标集合造成伤害。
	/// 同一条命令的所有命中都复用该集合；再次调用此方法才会选择新的集合。
	/// </summary>
	public static async Task<int> ExecuteDistinctRandomEnemyDamage(
		CardModel card,
		PlayerChoiceContext choiceContext,
		int requestedTargetCount,
		decimal? damagePerHit = null,
		int hitCountPerTarget = 1,
		string hitFx = "vfx/vfx_attack_slash",
		CardPlay? cardPlay = null)
	{
		ICombatState? combatState = card.CombatState;
		if (combatState == null || requestedTargetCount <= 0 || hitCountPerTarget <= 0)
		{
			return 0;
		}

		List<Creature> targets = SelectRandomEnemies(card, requestedTargetCount);
		if (targets.Count == 0)
		{
			return 0;
		}

		await DamageCmd.Attack(damagePerHit ?? card.DynamicVars.Damage.BaseValue)
			.FromCard(card, cardPlay)
			.TargetingAllOpponents(combatState)
			.TargetingFiltered(targets)
			.WithHitCount(hitCountPerTarget)
			.WithHitFx(hitFx)
			.Execute(choiceContext);

		return targets.Count;
	}
}
