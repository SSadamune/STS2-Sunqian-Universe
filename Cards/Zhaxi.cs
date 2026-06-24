using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "zhaxi")]
public sealed class Zhaxi : ModCardTemplate
{
	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_ZHAXI.selectionScreenPrompt");

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Zhaxi.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
	];

	public Zhaxi()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, false)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int remainingEnergy = Owner.PlayerCombatState?.Energy ?? 0;
		int powerCost = ResolvePowerCostFromRemainingEnergy(remainingEnergy);

		List<CardModel> candidatePool = GetOtherCharacterPowersAtCost(powerCost).ToList();
		if (candidatePool.Count == 0)
		{
			return;
		}

		CardModel? selected = IsUpgraded
			? await SelectPowerAsync(choiceContext, candidatePool)
			: RollRandomPower(candidatePool);

		if (selected is null)
		{
			return;
		}

		await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
	}

	private CardModel? RollRandomPower(IReadOnlyList<CardModel> candidatePool) =>
		CardFactory
			.GetDistinctForCombat(
				Owner,
				candidatePool,
				1,
				Owner.RunState.Rng.CombatCardGeneration)
			.FirstOrDefault();

	private async Task<CardModel?> SelectPowerAsync(
		PlayerChoiceContext choiceContext,
		IReadOnlyList<CardModel> candidatePool)
	{
		int choiceCount = Math.Min(3, candidatePool.Count);
		List<CardModel> choices = CardFactory
			.GetDistinctForCombat(
				Owner,
				candidatePool,
				choiceCount,
				Owner.RunState.Rng.CombatCardGeneration)
			.ToList();

		if (choices.Count == 0)
		{
			return null;
		}

		return await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			choices,
			Owner,
			canSkip: true);
	}

	private IEnumerable<CardModel> GetOtherCharacterPowersAtCost(int powerCost)
	{
		List<CardPoolModel> pools = Owner.UnlockState.CharacterCardPools.ToList();
		pools.Remove(Owner.Character.CardPool);

		return pools
			.SelectMany(pool => pool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint))
			.Where(card => MatchesPowerCost(card, powerCost));
	}

	private static int ResolvePowerCostFromRemainingEnergy(int remainingEnergy) =>
		remainingEnergy <= 0 ? 1 : Math.Min(remainingEnergy, 3);

	private static bool MatchesPowerCost(CardModel card, int powerCost) =>
		card.Type == CardType.Power
		&& !card.EnergyCost.CostsX
		&& card.EnergyCost.Canonical == powerCost;
}
