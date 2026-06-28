using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using Squ;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "juggle_multiple_shoots")]
public sealed class JuggleMultipleShoots : ModCardTemplate
{
	private const int ChoiceCount = 3;

	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_JUGGLE_MULTIPLE_SHOOTS.selectionScreenPrompt");

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/JuggleMultipleShoots.png");

	private IHoverTip CreateZeroEnergyHoverTip()
	{
		LocString description = new("cards", "SUNQIAN_UNIVERSE_CARD_JUGGLE_MULTIPLE_SHOOTS.zeroEnergyHoverTip");
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
		return new HoverTip(SquCommonL10n.AnnotationTitle(), description);
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		CreateZeroEnergyHoverTip(),
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public JuggleMultipleShoots()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ScriptSystem.InvalidateScriptsAsync(Owner.Creature);

		int remainingEnergy = Owner.PlayerCombatState?.Energy ?? 0;
		List<CardModel> choices = BuildPowerChoices(remainingEnergy);
		if (choices.Count == 0)
		{
			return;
		}

		if (IsUpgraded)
		{
			foreach (CardModel choice in choices)
			{
				choice.UpgradeInternal();
				choice.FinalizeUpgradeInternal();
			}
		}

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			choices,
			Owner,
			canSkip: true);

		if (selected is not null)
		{
			await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
		}
	}

	private List<CardModel> BuildPowerChoices(int remainingEnergy)
	{
		List<CardModel> pool = GetOtherCharacterPowersAtMostCost(ClampUpperBound(remainingEnergy)).ToList();
		if (pool.Count == 0)
		{
			return [];
		}

		Rng rng = Owner.RunState.Rng.CombatCardGeneration;
		List<CardModel> canonicalChoices = BuildSlottedCanonicalChoices(pool, remainingEnergy, rng);
		return CreateCombatCopies(canonicalChoices);
	}

	private List<CardModel> BuildSlottedCanonicalChoices(
		List<CardModel> pool,
		int remainingEnergy,
		Rng rng)
	{
		var picked = new List<CardModel>();

		for (int slot = 0; slot < ChoiceCount; slot++)
		{
			(int lower, int upper) = GetSlotCostBounds(slot, remainingEnergy);
			TryPickInto(
				pool,
				picked,
				card =>
				{
					int cost = GetCanonicalEnergyCost(card);
					return cost >= lower && cost <= upper;
				},
				rng);
		}

		while (picked.Count < ChoiceCount)
		{
			List<CardModel> remaining = pool.Where(card => !ContainsCanonical(picked, card)).ToList();
			if (remaining.Count == 0)
			{
				break;
			}

			CardModel? filler = rng.NextItem(remaining);
			if (filler is not null)
			{
				picked.Add(filler);
			}
		}

		return picked.Take(ChoiceCount).ToList();
	}

	private static (int Lower, int Upper) GetSlotCostBounds(int slotIndex, int remainingEnergy)
	{
		(int rawLower, int rawUpper) = slotIndex switch
		{
			0 => (remainingEnergy, remainingEnergy),
			1 => (remainingEnergy - 1, remainingEnergy),
			2 => (0, remainingEnergy),
			_ => throw new ArgumentOutOfRangeException(nameof(slotIndex)),
		};

		return (ClampLowerBound(rawLower), ClampUpperBound(rawUpper));
	}

	private static int ClampLowerBound(int rawLower) => Math.Min(rawLower, 3);

	private static int ClampUpperBound(int rawUpper) =>
		rawUpper <= 0 ? 1 : rawUpper;

	private List<CardModel> CreateCombatCopies(IEnumerable<CardModel> canonicalCards)
	{
		ICombatState? combatState = Owner.Creature.CombatState;
		if (combatState == null)
		{
			return [];
		}

		return canonicalCards
			.Select(card => combatState.CreateCard(card, Owner))
			.ToList();
	}

	private static void TryPickInto(
		List<CardModel> pool,
		List<CardModel> picked,
		Func<CardModel, bool> predicate,
		Rng rng)
	{
		if (picked.Count >= ChoiceCount)
		{
			return;
		}

		List<CardModel> matches = pool
			.Where(card => predicate(card) && !ContainsCanonical(picked, card))
			.ToList();
		if (matches.Count == 0)
		{
			return;
		}

		CardModel? pick = rng.NextItem(matches);
		if (pick is not null)
		{
			picked.Add(pick);
		}
	}

	private static bool ContainsCanonical(IReadOnlyList<CardModel> picked, CardModel candidate) =>
		picked.Any(card => card.Id == candidate.Id);

	private IEnumerable<CardModel> GetOtherCharacterPowersAtMostCost(int maxCost)
	{
		List<CardPoolModel> pools = Owner.UnlockState.CharacterCardPools.ToList();
		pools.Remove(Owner.Character.CardPool);

		return pools
			.SelectMany(pool => pool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint))
			.Where(card => IsEligiblePower(card, maxCost));
	}

	private static bool IsEligiblePower(CardModel card, int maxCost) =>
		card.Type == CardType.Power
		&& !card.EnergyCost.CostsX
		&& GetCanonicalEnergyCost(card) <= maxCost;

	private static int GetCanonicalEnergyCost(CardModel card) => card.EnergyCost.Canonical;
}
