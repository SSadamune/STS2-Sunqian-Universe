using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "human_transmutation")]
public sealed class HumanTransmutation : ModCardTemplate
{
	private const int DrawCount = 1;

	private const int MaxExhaustCount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(DrawCount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/HumanTransmutation.png");

	public HumanTransmutation()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DrawCount, Owner);

		CardSelectorPrefs exhaustPrefs = new(
			CardSelectorPrefs.ExhaustSelectionPrompt,
			minCount: 0,
			maxCount: MaxExhaustCount);
		List<CardModel> toExhaust = (await CardSelectCmd.FromHand(
			choiceContext,
			Owner,
			exhaustPrefs,
			null,
			this)).ToList();

		foreach (CardModel card in toExhaust)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}

		if (toExhaust.Count == 0)
		{
			return;
		}

		Rng rng = Owner.RunState.Rng.CombatCardGeneration;
		List<CardModel> choices = [];
		foreach (CardModel exhausted in toExhaust)
		{
			CardModel? attack = PickRandomAttackForRarity(MapExhaustedRarity(exhausted.Rarity), rng);
			if (attack is not null)
			{
				choices.Add(attack);
			}
		}

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

	private CardModel? PickRandomAttackForRarity(CardRarity rarity, Rng rng)
	{
		IEnumerable<CardModel> pool = Owner.Character.CardPool.GetUnlockedCards(
			Owner.UnlockState,
			Owner.RunState.CardMultiplayerConstraint);

		return CardFactory.GetDistinctForCombat(
				Owner,
				pool.Where(card => card.Type == CardType.Attack && card.Rarity == rarity),
				1,
				rng)
			.FirstOrDefault();
	}

	/// <summary>
	/// 被消耗牌的稀有度映射：先古→稀有，事件→罕见，Common/Uncommon/Rare 保持，其余→普通。
	/// </summary>
	private static CardRarity MapExhaustedRarity(CardRarity rarity) =>
		rarity switch
		{
			CardRarity.Ancient => CardRarity.Rare,
			CardRarity.Event => CardRarity.Uncommon,
			CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare => rarity,
			_ => CardRarity.Common,
		};
}
