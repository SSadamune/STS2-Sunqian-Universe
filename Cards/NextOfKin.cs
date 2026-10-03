using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "next_of_kin")]
public sealed class NextOfKin : FamilyCardTemplate
{
	public const int DrawAmount = 3;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NextOfKin.png");

	protected override int BlockAmount => 0;

	protected override int DrawCards => DrawAmount;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
		CardKeyword.Sly,
		SquKeywords.Family,
	];

	public NextOfKin()
		: base(1)
	{
	}

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseNextOfKinExhaustEvent;

	protected override Task ResolveAdditionalExhaust(
		PlayerChoiceContext choiceContext,
		bool causedByEthereal) =>
		CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
}
