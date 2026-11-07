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
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "stand_by_righteousness")]
[RegisterCharacterStarterCard(typeof(LiubeiCharacter), 1)]
public sealed class StandByRighteousness : SelfHarmCardTemplate
{
	public const string RighteousnessVar = "Righteousness";
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SelfHarm", 6),
		new DynamicVar(RighteousnessVar, 6),
		new BlockVar(6, ValueProp.Move),
	];
	public override bool GainsBlock => true;
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/prisoner/StandByRighteousness.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..SelfHarmSystem.HasSelfHarm(this)
			? new[] { SquKeywords.CreateSelfHarmHoverTip(SelfHarmSystem.GetAmountVar(this)) }
			: System.Array.Empty<IHoverTip>(),
		SquKeywords.CreateRighteousnessHoverTip(DynamicVars[RighteousnessVar]),
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	public StandByRighteousness() : base(2, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

	protected override async Task PlayAfterSelfHarm(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
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
