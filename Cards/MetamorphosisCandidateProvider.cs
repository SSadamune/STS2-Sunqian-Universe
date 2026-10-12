#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
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
		SpecialConditionCards,
		SunqianCaoWeiResponse,
		DefensiveResponse,
		HandBalance,
		Fallback,
	}

	private const int FinalStep = (int)SelectionStep.Fallback;
	private const int PileSearchCardLimit = 200;

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
	/// 3-5 = urgent responses; 6-7 = resource/early-turn responses; 8 = excess block;
	/// 9 = Power; 10 = unnamed-card hand type balance; 11 = any card;
	/// 12 = Sunqian/Cao Wei cross-character pool; 13 = high offensive resources;
	/// 14 = early safe unnamed Power; 15 = Eternal Armor while Plating is active;
	/// 16 = Vital Spark counter; 17 = high Dexterity; 18 = All for One density;
	/// 19 = Iteration status generation; 20 = defense pressure;
	/// 21 = Sleight of Flesh debuff density; 22 = Supermassive exhaust density.
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
					|| MatchesDangerousEnemyCondition(combatState, card)
					|| MatchesVitalSparkCounterCondition(combatState, card)),
			SelectionStep.ResourceResponse => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& (MatchesStrandedCardCondition(hand, card, remainingEnergy)
					|| MatchesExcessEnergyCondition(
						player,
						combatState,
						hand,
						card,
						remainingEnergy)
					|| MatchesHighOffensiveResourcesCondition(player, card)
					|| MatchesHighDexterityCondition(player, card)
					|| MatchesDefensePressureCondition(player, combatState, card)),
			SelectionStep.SpecialConditionCards => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& (MatchesExcessBlockCondition(player, combatState, card)
					|| MatchesAllForOneDensityCondition(player, card)
					|| MatchesIterationStatusDensityCondition(player, card)
					|| MatchesSleightOfFleshDebuffDensityCondition(player, card)
					|| MatchesSupermassiveExhaustDensityCondition(player, card)),
			SelectionStep.SunqianCaoWeiResponse => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& MatchesSunqianCaoWeiCondition(player, choice.CharacterPool)
				&& (MatchesStatusCleanupCondition(hand, card)
					|| MatchesMultipleEnemiesCondition(combatState, card)
					|| MatchesDangerousEnemyCondition(combatState, card)
					|| MatchesEarlySafePowerCondition(player, combatState, card)
					|| MatchesPlatingEternalArmorCondition(player, card)
					|| MatchesVitalSparkCounterCondition(combatState, card)),
			SelectionStep.DefensiveResponse => matchesRoleCondition
				&& MatchesPlayableCondition(card, remainingEnergy)
				&& (MatchesEarlySafePowerCondition(player, combatState, card)
					|| MatchesPlatingEternalArmorCondition(player, card)),
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
		&& (card is BurningPact or OpenDefecation or Purity
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.MyThreeGeneralsEntry));

	private static bool MatchesMultipleEnemiesCondition(
		ICombatState combatState,
		CardModel card) =>
		combatState.HittableEnemies.Count >= 3
		&& card is Whirlwind or Misery or FireNova;

	private static bool MatchesDangerousEnemyCondition(
		ICombatState combatState,
		CardModel card) =>
		combatState.HittableEnemies.Any(enemy =>
			GetIntentDamage(combatState, enemy) > 30)
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
		Player player,
		ICombatState combatState,
		IReadOnlyList<CardModel> hand,
		CardModel card,
		int remainingEnergy)
	{
		int attacksAndSkills = hand.Count(candidate =>
			candidate.Type is CardType.Attack or CardType.Skill);
		int cardsPlayedThisTurn = CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
			entry.HappenedThisTurn(combatState)
			&& entry.CardPlay.Player == player
			&& entry.CardPlay.PlayIndex == 0);
		return (remainingEnergy > attacksAndSkills || cardsPlayedThisTurn <= 2)
			&& (card is Backflip
				or BurningPact
				or Skim
				or Audition
				or BuddyLongtaodi
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.LetsDiscussEntry)
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.AutoPilotEntry)
				|| NewsanguoMetamorphosisInterop.IsCard(
					card,
					NewsanguoMetamorphosisInterop.AlwaysMineEntry));
	}

	private static bool MatchesExcessBlockCondition(
		Player player,
		ICombatState combatState,
		CardModel card)
	{
		int totalIntentDamage = GetTotalIntentDamage(combatState);
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

	private static bool MatchesEarlySafePowerCondition(
		Player player,
		ICombatState combatState,
		CardModel card) =>
		player.PlayerCombatState?.TurnNumber <= 3
		&& player.Creature.Block - GetTotalIntentDamage(combatState) >= -20
		&& card.Type == CardType.Power
		&& !IsExplicitlyNamedByAnotherCondition(card);

	private static bool MatchesPlatingEternalArmorCondition(
		Player player,
		CardModel card) =>
		player.Creature.GetPowerAmount<PlatingPower>() > 1
		&& card is EternalArmor;

	private static bool MatchesVitalSparkCounterCondition(
		ICombatState combatState,
		CardModel card) =>
		combatState.Enemies.Any(enemy =>
			enemy.IsAlive && enemy.HasPower<VitalSparkPower>())
		&& (card is Fisticuffs or EternalArmor or WaterFireInvincible
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.UnclesAndAuntsEntry));

	private static bool MatchesHighDexterityCondition(
		Player player,
		CardModel card) =>
		player.Creature.GetPowerAmount<DexterityPower>() > 2
		&& (card is MusouAttack
			or WorryWithoutWorry
			or Glitterstream
			or DodgeAndRoll
			|| NewsanguoMetamorphosisInterop.IsCard(
				card,
				NewsanguoMetamorphosisInterop.UnclesAndAuntsEntry));

	private static bool MatchesAllForOneDensityCondition(
		Player player,
		CardModel card)
	{
		if (card is not AllForOne)
		{
			return false;
		}

		return HasAtLeastMatchingCardsWithinLimit(
			PileType.Draw.GetPile(player).Cards,
			PileType.Discard.GetPile(player).Cards,
			5,
			candidate => candidate.Type == CardType.Attack
				&& !candidate.EnergyCost.CostsX
				&& candidate.EnergyCost.GetResolved() == 0);
	}

	private static bool MatchesIterationStatusDensityCondition(
		Player player,
		CardModel card) =>
		card is Iteration
		&& HasAtLeastMatchingCardsWithinLimit(
			PileType.Hand.GetPile(player).Cards,
			PileType.Draw.GetPile(player).Cards,
			3,
			GeneratesStatusCard);

	private static bool MatchesDefensePressureCondition(
		Player player,
		ICombatState combatState,
		CardModel card) =>
		GetTotalIntentDamage(combatState) - player.Creature.Block >= 20
		&& card is Bulwark
			or FightThrough
			or Defy
			or Backflip
			or CloakAndDagger
			or Rage
			or WaterFireInvincible;

	private static bool MatchesSleightOfFleshDebuffDensityCondition(
		Player player,
		CardModel card) =>
		card is SleightOfFlesh
		&& HasAtLeastMatchingCardsWithinLimit(
			PileType.Hand.GetPile(player).Cards,
			PileType.Draw.GetPile(player).Cards,
			5,
			AppliesRelevantDebuff);

	private static bool MatchesSupermassiveExhaustDensityCondition(
		Player player,
		CardModel card) =>
		card is Supermassive
		&& PileType.Exhaust.GetPile(player).Cards.Count >= 12;

	private static bool HasAtLeastMatchingCardsWithinLimit(
		IReadOnlyList<CardModel> firstPile,
		IReadOnlyList<CardModel> secondPile,
		int requiredCount,
		Func<CardModel, bool> predicate)
	{
		if (firstPile.Count + secondPile.Count > PileSearchCardLimit)
		{
			return false;
		}

		int matchingCount = 0;
		foreach (CardModel candidate in firstPile.Concat(secondPile))
		{
			if (predicate(candidate) && ++matchingCount >= requiredCount)
			{
				return true;
			}
		}

		return false;
	}

	private static bool GeneratesStatusCard(CardModel card) =>
		card.HoverTips
			.OfType<CardHoverTip>()
			.Any(tip => tip.Card.Rarity == CardRarity.Status);

	private static bool AppliesRelevantDebuff(CardModel card) =>
		SquCardTags.AppliesBurning(card)
		|| card.DynamicVars.ContainsKey(nameof(VulnerablePower))
		|| card.DynamicVars.ContainsKey(nameof(WeakPower))
		|| card.DynamicVars.ContainsKey(nameof(DoomPower))
		|| card.DynamicVars.ContainsKey(nameof(PoisonPower))
		|| card.DynamicVars.ContainsKey(nameof(BurningPower));

	private static bool MatchesSunqianCaoWeiCondition(
		Player player,
		CardPoolModel? characterPool) =>
		(player.Character is not SunqianCharacter && characterPool is SunqianCardPool)
		|| (!NewsanguoPublicApiInterop.IsCaoWeiCharacter(player.Character)
			&& characterPool is not null
			&& NewsanguoMetamorphosisInterop.IsCaoWeiCardPool(characterPool));

	private static bool MatchesHandBalanceCondition(
		IReadOnlyList<CardModel> hand,
		CardModel card)
	{
		if (IsExplicitlyNamedByAnotherCondition(card))
		{
			return false;
		}

		int attacks = hand.Count(candidate => candidate.Type == CardType.Attack);
		int skills = hand.Count(candidate => candidate.Type == CardType.Skill);
		return skills > attacks
			? card.Type == CardType.Attack
			: card.Type == CardType.Skill;
	}

	private static bool IsExplicitlyNamedByAnotherCondition(CardModel card) =>
		card is BurningPact
			or OpenDefecation
			or Purity
			or Whirlwind
			or Misery
			or FireNova
			or Defy
			or Wine
			or Production
			or Backflip
			or Skim
			or Audition
			or BuddyLongtaodi
			or Afterimage
			or TwoWordPoetScript
			or EternalArmor
			or Fisticuffs
			or WaterFireInvincible
			or MusouAttack
			or WorryWithoutWorry
			or Glitterstream
			or DodgeAndRoll
			or AllForOne
			or Iteration
			or Bulwark
			or FightThrough
			or CloakAndDagger
			or Rage
			or SleightOfFlesh
			or Supermassive
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.MyThreeGeneralsEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.RetireEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.HeavenAndEarthEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.AutoPilotEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.LetsDiscussEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.AlwaysMineEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.MaClanQuadBlastEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.TenThousandTransparentHolesEntry)
		|| NewsanguoMetamorphosisInterop.IsCard(
			card,
			NewsanguoMetamorphosisInterop.UnclesAndAuntsEntry);

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

	private static int GetTotalIntentDamage(ICombatState combatState) =>
		combatState.HittableEnemies.Sum(enemy => GetIntentDamage(combatState, enemy));

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
			ModelDb.Card<DodgeAndRoll>(),
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
			ModelDb.Card<Glitterstream>(),
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
			ModelDb.Card<OpenDefecation>(),
			ModelDb.Card<FireNova>(),
			ModelDb.Card<MusouAttack>(),
			ModelDb.Card<WorryWithoutWorry>(),
			ModelDb.Card<Audition>(),
			ModelDb.Card<TwoWordPoetScript>(),
			ModelDb.Card<WaterFireInvincible>(),
			ModelDb.Card<BuddyLongtaodi>(),
		];
		cards.AddRange(NewsanguoMetamorphosisInterop.GetCaoWeiCandidateCards());
		cards.AddRange(NewsanguoMetamorphosisInterop.GetShuHanCandidateCards());
		return cards;
	}
}
