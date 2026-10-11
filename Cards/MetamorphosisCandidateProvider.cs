#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Random;
using Squ.Character;
using Squ.Powers;

namespace Squ.Cards;

/// <summary>
/// Supplies the three cards offered by <see cref="Metamorphosis"/>. Replacing
/// <see cref="Metamorphosis.CandidateProvider"/> allows a future implementation to use
/// deck/hand-specific heuristics without changing the card's resolution flow.
/// </summary>
public interface IMetamorphosisCandidateProvider
{
	IReadOnlyList<CardModel> BuildChoices(Metamorphosis source, int count);
}

internal static class MetamorphosisCharacterCatalog
{
	public static IReadOnlyList<CardPoolModel> SupportedPools =>
	[
		ModelDb.CardPool<IroncladCardPool>(),
		ModelDb.CardPool<SilentCardPool>(),
		ModelDb.CardPool<DefectCardPool>(),
		ModelDb.CardPool<NecrobinderCardPool>(),
		ModelDb.CardPool<RegentCardPool>(),
		ModelDb.CardPool<SunqianCardPool>(),
	];

	public static bool IsSupported(CardPoolModel pool) =>
		SupportedPools.Any(candidate => candidate.Id == pool.Id);

	public static string GetDisplayName(CardPoolModel pool) => pool switch
	{
		IroncladCardPool => ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Ironclad>()
			.Title.GetFormattedText() ?? pool.Title,
		SilentCardPool => ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Silent>()
			.Title.GetFormattedText() ?? pool.Title,
		DefectCardPool => ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Defect>()
			.Title.GetFormattedText() ?? pool.Title,
		NecrobinderCardPool => ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Necrobinder>()
			.Title.GetFormattedText() ?? pool.Title,
		RegentCardPool => ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Regent>()
			.Title.GetFormattedText() ?? pool.Title,
		SunqianCardPool => ModelDb.Character<SunqianCharacter>()
			.Title.GetFormattedText() ?? pool.Title,
		_ => pool.Title,
	};
}

internal sealed class DefaultMetamorphosisCandidateProvider : IMetamorphosisCandidateProvider
{
	private const int FinalCondition = 5;

	public IReadOnlyList<CardModel> BuildChoices(Metamorphosis source, int count)
	{
		Player player = source.Owner;
		ICombatState? combatState = source.CombatState;
		if (combatState is null || count <= 0)
		{
			return [];
		}

		MetamorphosisPower? power = player.Creature.GetPower<MetamorphosisPower>();
		List<CardPoolModel> availablePools = MetamorphosisCharacterCatalog.SupportedPools
			.Where(pool => pool.Id != player.Character.CardPool.Id)
			.Where(pool => power is null || !power.HasRecorded(pool))
			.ToList();

		List<CardModel> pool = availablePools.Count > 0
			? BuildTemporaryCharacterPool()
				.Where(card => availablePools.Any(candidate => candidate.Id == card.Pool.Id))
				.ToList()
			: BuildColorlessPool();

		if (pool.Count == 0)
		{
			return [];
		}

		bool requireDistinctCharacters = availablePools.Count >= count;
		List<CardModel> picked = PickCanonicalChoices(
			source,
			pool,
			count,
			requireDistinctCharacters,
			player.RunState.Rng.CombatCardGeneration);

		return picked
			.Select(card => combatState.CreateCard(card, player))
			.ToList();
	}

	private static List<CardModel> PickCanonicalChoices(
		Metamorphosis source,
		List<CardModel> pool,
		int count,
		bool requireDistinctCharacters,
		Rng rng)
	{
		List<CardModel> picked = [];
		int firstConditionToCheck = 1;

		while (picked.Count < count)
		{
			CardModel? choice = null;
			int matchedCondition = firstConditionToCheck;
			for (int condition = firstConditionToCheck; condition <= FinalCondition; condition++)
			{
				List<CardModel> matches = pool
					.Where(card => !picked.Any(existing => existing.Id == card.Id))
					.Where(card => !requireDistinctCharacters
						|| !picked.Any(existing => existing.Pool.Id == card.Pool.Id))
					.Where(card => MatchesCondition(source, card, condition))
					.ToList();
				if (matches.Count == 0)
				{
					continue;
				}

				choice = rng.NextItem(matches);
				matchedCondition = condition;
				break;
			}

			if (choice is null)
			{
				break;
			}

			picked.Add(choice);
			// Condition five is the catch-all and remains available for all remaining slots.
			firstConditionToCheck = Math.Min(matchedCondition + 1, FinalCondition);
		}

		return picked;
	}

	private static bool MatchesCondition(Metamorphosis source, CardModel card, int condition)
	{
		Player player = source.Owner;
		ICombatState combatState = source.CombatState!;
		IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(player).Cards;
		int remainingEnergy = player.PlayerCombatState?.Energy ?? 0;

		return condition switch
		{
			1 => IsAffordable(card, remainingEnergy) && MatchesUrgentCondition(combatState, hand, card),
			2 => IsAffordable(card, remainingEnergy)
				&& MatchesResourceCondition(player, combatState, hand, card, remainingEnergy),
			3 => IsAffordable(card, remainingEnergy) && card.Type == CardType.Power,
			4 => MatchesHandBalanceCondition(hand, card),
			5 => true,
			_ => false,
		};
	}

	private static bool MatchesUrgentCondition(
		ICombatState combatState,
		IReadOnlyList<CardModel> hand,
		CardModel card)
	{
		bool hasStatus = hand.Any(candidate => candidate.Rarity == CardRarity.Status);
		bool hasMultipleEnemies = combatState.HittableEnemies.Count > 1;
		bool hasDangerousUnweakenedEnemy = combatState.HittableEnemies.Any(enemy =>
			!enemy.HasPower<WeakPower>() && GetIntentDamage(combatState, enemy) > 30);

		return (hasStatus && card is BurningPact or WhyDoubtWei)
			|| (hasMultipleEnemies && card is Whirlwind or Misery)
			|| (hasDangerousUnweakenedEnemy && card is Defy);
	}

	private static bool MatchesResourceCondition(
		Player player,
		ICombatState combatState,
		IReadOnlyList<CardModel> hand,
		CardModel card,
		int remainingEnergy)
	{
		bool strandedCostlyCard = remainingEnergy == 0
			&& hand.Any(candidate => !candidate.EnergyCost.CostsX
				&& candidate.EnergyCost.GetResolved() > 0);
		bool energyExceedsHand = remainingEnergy > hand.Count;
		int totalIntentDamage = combatState.HittableEnemies.Sum(enemy =>
			GetIntentDamage(combatState, enemy));
		bool excessBlock = player.Creature.Block > totalIntentDamage + 10;

		return (strandedCostlyCard && card is Wine)
			|| (energyExceedsHand && card is Backflip or BurningPact or Skim)
			|| (excessBlock && card is Afterimage);
	}

	private static bool MatchesHandBalanceCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card)
	{
		int attacks = hand.Count(candidate => candidate.Type == CardType.Attack);
		int skills = hand.Count(candidate => candidate.Type == CardType.Skill);
		return skills > attacks
			? card.Type == CardType.Attack
			: card.Type == CardType.Skill;
	}

	private static int GetIntentDamage(ICombatState combatState, Creature enemy)
	{
		if (enemy.Monster?.NextMove?.Intents is not { } intents)
		{
			return 0;
		}

		IEnumerable<Creature> targets = combatState.GetOpponentsOf(enemy);
		return intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage(targets, enemy));
	}

	private static bool IsAffordable(CardModel card, int remainingEnergy) =>
		card.EnergyCost.CostsX || card.EnergyCost.Canonical <= remainingEnergy;

	private static List<CardModel> BuildColorlessPool() =>
	[
		ModelDb.Card<Purity>(),
		ModelDb.Card<Production>(),
		ModelDb.Card<Salvo>(),
		ModelDb.Card<Fisticuffs>(),
		ModelDb.Card<GoldAxe>(),
		ModelDb.Card<EternalArmor>(),
	];

	private static List<CardModel> BuildTemporaryCharacterPool() =>
	[
		// Ironclad
		ModelDb.Card<Whirlwind>(),
		ModelDb.Card<BurningPact>(),
		ModelDb.Card<PommelStrike>(),
		ModelDb.Card<Rage>(),
		ModelDb.Card<DarkEmbrace>(),
		// Silent
		ModelDb.Card<Prepared>(),
		ModelDb.Card<CloakAndDagger>(),
		ModelDb.Card<Backflip>(),
		ModelDb.Card<Neutralize>(),
		ModelDb.Card<Afterimage>(),
		ModelDb.Card<WellLaidPlans>(),
		// Regent
		ModelDb.Card<KnowThyPlace>(),
		ModelDb.Card<Supermassive>(),
		ModelDb.Card<Bulwark>(),
		ModelDb.Card<PillarOfCreation>(),
		ModelDb.Card<CosmicIndifference>(),
		ModelDb.Card<Arsenal>(),
		// Necrobinder
		ModelDb.Card<Misery>(),
		ModelDb.Card<Defy>(),
		ModelDb.Card<SleightOfFlesh>(),
		ModelDb.Card<Graveblast>(),
		ModelDb.Card<Neurosurge>(),
		// Defect
		ModelDb.Card<AllForOne>(),
		ModelDb.Card<Tempest>(),
		ModelDb.Card<Skim>(),
		ModelDb.Card<Iteration>(),
		ModelDb.Card<FightThrough>(),
		ModelDb.Card<Supercritical>(),
		// Supporting Actor
		ModelDb.Card<Wine>(),
		ModelDb.Card<WhyDoubtWei>(),
		ModelDb.Card<Audition>(),
		ModelDb.Card<NeverHadThese>(),
		ModelDb.Card<WaterFireInvincible>(),
		ModelDb.Card<BuddyLongtaodi>(),
	];
}
