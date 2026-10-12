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
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

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
	public static IReadOnlyList<CardPoolModel> SupportedPools
	{
		get
		{
			List<CardPoolModel> pools =
			[
				ModelDb.CardPool<IroncladCardPool>(),
				ModelDb.CardPool<SilentCardPool>(),
				ModelDb.CardPool<DefectCardPool>(),
				ModelDb.CardPool<NecrobinderCardPool>(),
				ModelDb.CardPool<RegentCardPool>(),
				ModelDb.CardPool<SunqianCardPool>(),
			];
			if (NewsanguoMetamorphosisInterop.GetCaoWeiCardPool() is { } caoWeiPool)
			{
				pools.Add(caoWeiPool);
			}

			return pools;
		}
	}

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
		_ when NewsanguoMetamorphosisInterop.IsCaoWeiCardPool(pool) =>
			NewsanguoMetamorphosisInterop.GetCaoWeiDisplayName() ?? pool.Title,
		_ => pool.Title,
	};
}

internal sealed class DefaultMetamorphosisCandidateProvider : IMetamorphosisCandidateProvider
{
	private enum SelectionStep
	{
		UrgentResponse = 1,
		ResourceResponse,
		SunqianCaoWeiBalance,
		PlayablePower,
		HandBalance,
		Fallback,
	}

	private const int FinalStep = (int)SelectionStep.Fallback;

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

		List<CardModel> picked = PickCanonicalChoices(
			source,
			pool,
			count,
			availablePools.Count,
			player.RunState.Rng.CombatCardGeneration);

		return picked
			.Select(card => combatState.CreateCard(card, player))
			.ToList();
	}

	private static List<CardModel> PickCanonicalChoices(
		Metamorphosis source,
		List<CardModel> pool,
		int count,
		int remainingCharacterCount,
		Rng rng)
	{
		List<CardModel> picked = [];
		int firstStepToCheck = 1;

		while (picked.Count < count)
		{
			CardModel? choice = null;
			int matchedStep = firstStepToCheck;
			for (int step = firstStepToCheck; step <= FinalStep; step++)
			{
				List<CardModel> matches = pool
					.Where(card => !picked.Any(existing => existing.Id == card.Id))
					.Where(card => MatchesStep(
						source,
						card,
						picked,
						remainingCharacterCount,
						step))
					.ToList();
				if (matches.Count == 0)
				{
					continue;
				}

				choice = rng.NextItem(matches);
				matchedStep = step;
				break;
			}

			if (choice is null)
			{
				break;
			}

			picked.Add(choice);
			// The final step is the catch-all and remains available for all remaining slots.
			firstStepToCheck = Math.Min(matchedStep + 1, FinalStep);
		}

		return picked;
	}

	/// <summary>
	/// Each step combines reusable conditions:
	/// 1 = role deduplication/colorless phase; 2 = playable resource cost;
	/// 3-5 = urgent responses; 6-8 = resource responses;
	/// 9 = Power; 10 = hand type balance; 11 = any card;
	/// 12 = Sunqian/Cao Wei cross-character pool; 13 = high offensive resources.
	/// </summary>
	private static bool MatchesStep(
		Metamorphosis source,
		CardModel card,
		IReadOnlyList<CardModel> picked,
		int remainingCharacterCount,
		int stepNumber)
	{
		Player player = source.Owner;
		ICombatState combatState = source.CombatState!;
		IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(player).Cards;
		int remainingEnergy = player.PlayerCombatState?.Energy ?? 0;
		bool matchesRoleCondition = MatchesRoleDeduplicationCondition(
			card,
			picked,
			remainingCharacterCount);

		return (SelectionStep)stepNumber switch
		{
			SelectionStep.UrgentResponse => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& (MatchesStatusCleanupCondition(hand, card)
					|| MatchesMultipleEnemiesCondition(combatState, card)
					|| MatchesDangerousEnemyCondition(combatState, card)),
			SelectionStep.ResourceResponse => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& (MatchesStrandedCardCondition(hand, card, remainingEnergy)
					|| MatchesExcessEnergyCondition(hand, card, remainingEnergy)
					|| MatchesExcessBlockCondition(player, combatState, card)
					|| MatchesHighOffensiveResourcesCondition(player, card)),
			SelectionStep.SunqianCaoWeiBalance => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& MatchesHandBalanceCondition(hand, card)
				&& MatchesSunqianCaoWeiCondition(player, card),
			SelectionStep.PlayablePower => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& MatchesPowerCardCondition(card),
			SelectionStep.HandBalance => matchesRoleCondition
				&& MatchesHandBalanceCondition(hand, card),
			SelectionStep.Fallback => matchesRoleCondition
				&& MatchesAnyCardCondition(card),
			_ => false,
		};
	}

	private static bool MatchesRoleDeduplicationCondition(
		CardModel card,
		IReadOnlyList<CardModel> picked,
		int remainingCharacterCount)
	{
		if (remainingCharacterCount == 0)
		{
			return card.Pool.IsColorless;
		}

		return !card.Pool.IsColorless
			&& (remainingCharacterCount < 3
				|| picked.All(existing => existing.Pool.Id != card.Pool.Id));
	}

	private static bool MatchesPlayableCondition(CardModel card, int remainingEnergy) =>
		(card.EnergyCost.CostsX || card.EnergyCost.Canonical <= remainingEnergy)
		&& !ConsumesSecondaryResources(card);

	private static bool ConsumesSecondaryResources(CardModel card)
	{
		if (card.HasStarCostX || card.CanonicalStarCost > 0)
		{
			return true;
		}

		return SecondaryResourcePaymentResolver.Plan(card).Lines.Any(line =>
			line.CostsX || line.Cost > 0);
	}

	private static bool MatchesStatusCleanupCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card) =>
		hand.Any(candidate => candidate.Rarity == CardRarity.Status)
		&& (card is BurningPact or WhyDoubtWei or Purity
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.MyThreeGeneralsEntry));

	private static bool MatchesMultipleEnemiesCondition(
		ICombatState combatState,
		CardModel card) =>
		combatState.HittableEnemies.Count > 1
		&& card is Whirlwind or Misery;

	private static bool MatchesDangerousEnemyCondition(
		ICombatState combatState,
		CardModel card) =>
		combatState.HittableEnemies.Any(enemy =>
			!enemy.HasPower<WeakPower>() && GetIntentDamage(combatState, enemy) > 30)
		&& (card is Defy
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.RetireEntry));

	private static bool MatchesStrandedCardCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card,
		int remainingEnergy) =>
		remainingEnergy == 0
			&& hand.Any(candidate => !candidate.EnergyCost.CostsX
				&& candidate.EnergyCost.GetResolved() > 0)
			&& card is Wine or Production;

	private static bool MatchesExcessEnergyCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card,
		int remainingEnergy)
	{
		int attacksAndSkills = hand.Count(candidate =>
			candidate.Type is CardType.Attack or CardType.Skill);
		return remainingEnergy > attacksAndSkills
			&& (card is Backflip or BurningPact or Skim or Audition
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.LetsDiscussEntry));
	}

	private static bool MatchesExcessBlockCondition(
		Player player,
		ICombatState combatState,
		CardModel card)
	{
		int totalIntentDamage = combatState.HittableEnemies.Sum(enemy =>
			GetIntentDamage(combatState, enemy));
		return player.Creature.Block > totalIntentDamage + 10
			&& card is Afterimage;
	}

	private static bool MatchesHighOffensiveResourcesCondition(
		Player player,
		CardModel card)
	{
		decimal total = player.Creature.GetPowerAmount<VigorPower>()
			+ player.Creature.GetPowerAmount<StrengthPower>()
			+ NewsanguoMetamorphosisInterop.GetDrunkenMight(player);
		return total > 5
			&& (card is Whirlwind or TwoWordPoetScript
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.MaClanQuadBlastEntry));
	}

	private static bool MatchesSunqianCaoWeiCondition(Player player, CardModel card) =>
		(player.Character is not SunqianCharacter && card.Pool is SunqianCardPool)
		|| (!NewsanguoPublicApiInterop.IsCaoWeiCharacter(player.Character)
			&& NewsanguoMetamorphosisInterop.IsCaoWeiCardPool(card.Pool));

	private static bool MatchesPowerCardCondition(CardModel card) =>
		card.Type == CardType.Power;

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

	private static bool MatchesAnyCardCondition(CardModel _) => true;

	private static int GetIntentDamage(ICombatState combatState, Creature enemy)
	{
		if (enemy.Monster?.NextMove?.Intents is not { } intents)
		{
			return 0;
		}

		IEnumerable<Creature> targets = combatState.GetOpponentsOf(enemy);
		return intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage(targets, enemy));
	}

	private static List<CardModel> BuildColorlessPool() =>
	[
		ModelDb.Card<Purity>(),
		ModelDb.Card<Production>(),
		ModelDb.Card<Salvo>(),
		ModelDb.Card<Fisticuffs>(),
		ModelDb.Card<GoldAxe>(),
		ModelDb.Card<EternalArmor>(),
	];

	private static List<CardModel> BuildTemporaryCharacterPool()
	{
		List<CardModel> cards =
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
			ModelDb.Card<TwoWordPoetScript>(),
			ModelDb.Card<NeverHadThese>(),
			ModelDb.Card<WaterFireInvincible>(),
			ModelDb.Card<BuddyLongtaodi>(),
		];
		cards.AddRange(NewsanguoMetamorphosisInterop.GetCaoWeiCandidateCards());
		return cards;
	}
}
