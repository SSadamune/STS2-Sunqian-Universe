using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "next_of_kin")]
public sealed class NextOfKin : FamilyCardTemplate
{
	public const int DrawAmount = 2;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NextOfKin.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(DrawAmount),
		.. base.CanonicalVars,
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
		CardKeyword.Retain,
		SquKeywords.Family,
	];

	public NextOfKin()
		: base(3)
	{
	}

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseNextOfKinExhaustEvent;

	protected override async Task ResolveAdditionalExhaust(
		PlayerChoiceContext choiceContext,
		bool causedByEthereal)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		if (DeckVersion is { Pile.Type: PileType.Deck } deckCard)
		{
			await CardPileCmd.RemoveFromDeck(deckCard);
			DeckVersion = null;
		}
	}
}
