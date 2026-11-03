#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "lead_civilians_across")]
public sealed class LeadCiviliansAcross : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 4),
		new PowerVar<TemporaryHealthPower>(12),
	];
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<TemporaryHealthPower>(),
	];
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
	public LeadCiviliansAcross() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		await CreatureCmd.Damage(context, Owner.Creature, DynamicVars["HpLoss"].BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered, this, play);
		if (Owner.Creature.IsDead) return;
		await PowerCmd.Apply<TemporaryHealthPower>(context, Owner.Creature,
			DynamicVars[nameof(TemporaryHealthPower)].BaseValue, Owner.Creature, this);
	}
	protected override void OnUpgrade() =>
		DynamicVars[nameof(TemporaryHealthPower)].UpgradeValueBy(3);
}
