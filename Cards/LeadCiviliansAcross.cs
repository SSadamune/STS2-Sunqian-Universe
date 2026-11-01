#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "lead_civilians_across")]
public sealed class LeadCiviliansAcross : SelfHarmCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SelfHarm", 6),
		new PowerVar<TemporaryHealthPower>(12),
	];
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/RelocateCapital.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquKeywords.CreateSelfHarmHoverTip(DynamicVars["SelfHarm"]),
		HoverTipFactory.FromPower<TemporaryHealthPower>(),
	];
	public LeadCiviliansAcross() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
	protected override async Task PlayAfterSelfHarm(PlayerChoiceContext context, CardPlay play) =>
		await PowerCmd.Apply<TemporaryHealthPower>(context, Owner.Creature,
			DynamicVars[nameof(TemporaryHealthPower)].BaseValue, Owner.Creature, this);
	protected override void OnUpgrade() =>
		DynamicVars[nameof(TemporaryHealthPower)].UpgradeValueBy(4);
}
