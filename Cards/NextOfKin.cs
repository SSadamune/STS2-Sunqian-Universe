using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "next_of_kin")]
public sealed class NextOfKin : FamilyCardTemplate
{
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NextOfKin.png");

	protected override int BlockAmount => 0;

	protected override int DrawCards => 2;

	protected override string DiscardSfxEvent => SquSfx.NeverHadTheseNextOfKinDiscardEvent;

	protected override string ExhaustSfxEvent => SquSfx.NeverHadTheseNextOfKinExhaustEvent;
}
