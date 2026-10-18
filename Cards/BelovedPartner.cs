using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "beloved_partner")]
public sealed class BelovedPartner : FamilyCardTemplate
{
	public const int RegenAmount = 3;
	public const int TinderAmount = 3;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BelovedPartner.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(RegenAmount),
		new PowerVar<TinderPower>(TinderAmount),
		.. base.CanonicalVars,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>(),
		HoverTipFactory.FromPower<TinderPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
		CardKeyword.Sly,
		SquKeywords.Family,
	];

	public BelovedPartner()
		: base(2)
	{
	}

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseBelovedPartnerExhaustEvent;

	protected override async Task ResolveAdditionalExhaust(
		PlayerChoiceContext choiceContext,
		bool causedByEthereal)
	{
		await PowerCmd.Apply<RegenPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(RegenPower)].BaseValue,
			Owner.Creature,
			this);
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(TinderPower)].BaseValue,
			Owner.Creature,
			this);
	}
}
