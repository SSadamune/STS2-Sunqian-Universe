using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "beloved_partner")]
public sealed class BelovedPartner : FamilyCardTemplate
{
	public const int VigorAmount = 3;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BelovedPartner.png");

	protected override int BlockAmount => 3;

	protected override int DrawCards => 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(VigorAmount),
		.. base.CanonicalVars,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		.. base.AdditionalHoverTips,
	];

	protected override string DiscardSfxEvent => SquSfx.NeverHadTheseBelovedPartnerDiscardEvent;

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseBelovedPartnerExhaustEvent;

	protected override Task ResolveAdditionalDiscard(PlayerChoiceContext choiceContext) =>
		PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);
}
