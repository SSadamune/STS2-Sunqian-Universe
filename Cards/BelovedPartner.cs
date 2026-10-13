using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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
	public const int VigorAmount = 3;
	public const int TinderAmount = 3;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BelovedPartner.png");

	protected override bool ResolvesEffectsWhenDiscarded => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(VigorAmount),
		new PowerVar<TinderPower>(TinderAmount),
		.. base.CanonicalVars,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<TinderPower>(),
		.. base.AdditionalHoverTips,
	];

	protected override string DiscardSfxEvent => SquSfx.NeverHadTheseBelovedPartnerDiscardEvent;

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseBelovedPartnerExhaustEvent;

	protected override async Task ResolveAdditionalDiscard(PlayerChoiceContext choiceContext)
	{
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
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
