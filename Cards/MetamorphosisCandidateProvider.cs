#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Random;
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

namespace Squ.Cards;

/// <summary>
/// Supplies the three cards offered by <see cref="Metamorphosis"/>. Replacing
/// <see cref="Metamorphosis.CandidateProvider"/> allows a future implementation to use
/// deck/hand-specific heuristics without changing the card's resolution flow.
/// </summary>
public sealed record MetamorphosisCandidate(CardModel Card, CardPoolModel? CharacterPool);

public interface IMetamorphosisCandidateProvider
{
	IReadOnlyList<MetamorphosisCandidate> BuildChoices(Metamorphosis source, int count);
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
			if (NewsanguoMetamorphosisInterop.GetShuHanCardPool() is { } shuHanPool)
			{
				pools.Add(shuHanPool);
			}

			return pools;
		}
	}

	public static bool IsSupported(CardPoolModel pool) =>
		SupportedPools.Any(candidate => candidate.Id == pool.Id);

	public static IReadOnlyList<CardPoolModel> GetSupportedPools(Player player)
	{
		HashSet<ModelId> playerPoolIds = CharacterCardPoolMembership
			.GetCharacterPools(player)
			.Select(pool => pool.Id)
			.ToHashSet();
		return SupportedPools
			.Where(pool => playerPoolIds.Contains(pool.Id))
			.ToList();
	}

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
		_ when NewsanguoMetamorphosisInterop.IsShuHanCardPool(pool) =>
			NewsanguoMetamorphosisInterop.GetShuHanDisplayName() ?? pool.Title,
		_ => pool.Title,
	};
}

internal sealed class DefaultMetamorphosisCandidateProvider : IMetamorphosisCandidateProvider
{
	private sealed record CanonicalChoice(CardModel Card, CardPoolModel? CharacterPool);

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

	public IReadOnlyList<MetamorphosisCandidate> BuildChoices(Metamorphosis source, int count)
	{
		Player player = source.Owner;
		ICombatState? combatState = source.CombatState;
		if (combatState is null || count <= 0)
		{
			return [];
		}

		MetamorphosisPower? power = player.Creature.GetPower<MetamorphosisPower>();
		List<CardPoolModel> availablePools = MetamorphosisCharacterCatalog
			.GetSupportedPools(player)
			.Where(pool => pool.Id != player.Character.CardPool.Id)
			.Where(pool => power is null || !power.HasRecorded(pool))
			.ToList();

		List<CanonicalChoice> pool = availablePools.Count > 0
			? BuildCharacterChoices(player, availablePools)
			: BuildColorlessChoices();

		if (pool.Count == 0)
		{
			return [];
		}

		List<CanonicalChoice> picked = PickCanonicalChoices(
			source,
			pool,
			count,
			availablePools.Count,
			player.RunState.Rng.CombatCardGeneration);

		return picked
			.Select(choice => new MetamorphosisCandidate(
				combatState.CreateCard(choice.Card, player),
				choice.CharacterPool))
			.ToList();
	}

	private static List<CanonicalChoice> PickCanonicalChoices(
		Metamorphosis source,
		List<CanonicalChoice> pool,
		int count,
		int remainingCharacterCount,
		Rng rng)
	{
		List<CanonicalChoice> picked = [];
		int firstStepToCheck = 1;

		while (picked.Count < count)
		{
			CanonicalChoice? selectedChoice = null;
			int matchedStep = firstStepToCheck;
			for (int step = firstStepToCheck; step <= FinalStep; step++)
			{
				List<CanonicalChoice> matches = pool
					.Where(choice => !picked.Any(existing =>
						existing.Card.Id == choice.Card.Id))
					.Where(choice => MatchesStep(
						source,
						choice,
						picked,
						remainingCharacterCount,
						step))
					.GroupBy(choice => choice.Card.Id)
					.Select(group => group.First())
					.ToList();
				if (matches.Count == 0)
				{
					continue;
				}

				selectedChoice = rng.NextItem(matches);
				matchedStep = step;
				break;
			}

			if (selectedChoice is null)
			{
				break;
			}

			picked.Add(selectedChoice);
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
		CanonicalChoice choice,
		IReadOnlyList<CanonicalChoice> picked,
		int remainingCharacterCount,
		int stepNumber)
	{
		Player player = source.Owner;
		ICombatState combatState = source.CombatState!;
		IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(player).Cards;
		CardModel card = choice.Card;
		int remainingEnergy = player.PlayerCombatState?.Energy ?? 0;
		bool matchesRoleCondition = MatchesRoleDeduplicationCondition(
			choice,
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
				&& MatchesSunqianCaoWeiCondition(player, choice.CharacterPool),
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
		CanonicalChoice choice,
		IReadOnlyList<CanonicalChoice> picked,
		int remainingCharacterCount)
	{
		if (remainingCharacterCount == 0)
		{
			return choice.CharacterPool is null
				&& CharacterCardPoolMembership.IsColorless(choice.Card);
		}

		return choice.CharacterPool is not null
			&& (remainingCharacterCount < 3
				|| picked.All(existing =>
					existing.CharacterPool?.Id != choice.CharacterPool.Id));
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
				NewsanguoMetamorphosisInterop.RetireEntry)
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.HeavenAndEarthEntry));

	private static bool MatchesStrandedCardCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card,
		int remainingEnergy) =>
		remainingEnergy == 0
			&& hand.Any(candidate => !candidate.EnergyCost.CostsX
				&& candidate.EnergyCost.GetResolved() > 0)
			&& (card is Wine or Production
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.AutoPilotEntry));

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
					NewsanguoMetamorphosisInterop.LetsDiscussEntry)
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.AutoPilotEntry));
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
					NewsanguoMetamorphosisInterop.MaClanQuadBlastEntry)
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.TenThousandTransparentHolesEntry));
	}

	private static bool MatchesSunqianCaoWeiCondition(
		Player player,
		CardPoolModel? characterPool) =>
		(player.Character is not SunqianCharacter && characterPool is SunqianCardPool)
		|| (!NewsanguoPublicApiInterop.IsCaoWeiCharacter(player.Character)
			&& characterPool is not null
			&& NewsanguoMetamorphosisInterop.IsCaoWeiCardPool(characterPool));

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

	private static List<CanonicalChoice> BuildCharacterChoices(
		Player player,
		IEnumerable<CardPoolModel> pools)
	{
		HashSet<ModelId> allowedIds = BuildTemporaryCharacterPool()
			.Select(card => card.Id)
			.ToHashSet();
		return pools
			.SelectMany(pool => CardFactory.FilterForCombat(pool.GetUnlockedCards(
					player.UnlockState,
					player.RunState.CardMultiplayerConstraint))
				.Where(card => allowedIds.Contains(card.Id))
				.Select(card => new CanonicalChoice(card, pool)))
			.ToList();
	}

	private static List<CanonicalChoice> BuildColorlessChoices() =>
		CardFactory.FilterForCombat(BuildColorlessPool())
			.Where(CharacterCardPoolMembership.IsColorless)
			.Select(card => new CanonicalChoice(card, null))
			.ToList();

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
		cards.AddRange(NewsanguoMetamorphosisInterop.GetShuHanCandidateCards());
		return cards;
	}
}
