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

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "stand_by_righteousness")]
[RegisterCharacterStarterCard(typeof(LiubeiCharacter), 1)]
public sealed class StandByRighteousness : ModCardTemplate
{
	public const string RighteousnessVar = "Righteousness";
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 4),
		new DynamicVar(RighteousnessVar, 6),
		new BlockVar(8, ValueProp.Move),
	];
	public override bool GainsBlock => true;
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BenevolenceRighteousnessDualSwords.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquKeywords.CreateRighteousnessHoverTip(DynamicVars[RighteousnessVar]),
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	public StandByRighteousness() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["HpLoss"].BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);
		if (Owner.Creature.IsDead) return;
		await PowerCmd.Apply<RighteousnessPower>(choiceContext, Owner.Creature,
			DynamicVars[RighteousnessVar].BaseValue, Owner.Creature, this);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[RighteousnessVar].UpgradeValueBy(2);
		DynamicVars.Block.UpgradeValueBy(2);
	}
}
