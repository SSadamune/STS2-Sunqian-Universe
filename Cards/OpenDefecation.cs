using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "open_defecation")]
public sealed class OpenDefecation : ModCardTemplate
{
	private const int Threshold = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Threshold", Threshold),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromCard<Splash>(IsUpgraded),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/OpenDefecation.png");

	protected override bool ShouldGlowGoldInternal =>
		Pile?.Type == PileType.Hand
		&& PileType.Hand.GetPile(Owner).Cards.Count(card => card != this && !card.CanPlay()) >= Threshold;

	public OpenDefecation()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		ArgumentNullException.ThrowIfNull(combatState, nameof(combatState));

		CardPile hand = PileType.Hand.GetPile(Owner);
		List<CardModel> unplayableCards = hand.Cards
			.Where(card => !card.CanPlay())
			.ToList();

		foreach (CardModel card in unplayableCards)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}

		if (unplayableCards.Count >= Threshold)
		{
			CardModel splash = combatState.CreateCard<Splash>(Owner);
			if (IsUpgraded)
			{
				splash.UpgradeInternal();
				splash.FinalizeUpgradeInternal();
			}
			splash.AddKeyword(CardKeyword.Exhaust);
			await CardPileCmd.AddGeneratedCardToCombat(splash, PileType.Hand, Owner);
		}
	}

	protected override void OnUpgrade()
	{
	}
}
