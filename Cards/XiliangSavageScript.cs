using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "xiliang_savage_script")]
public sealed class XiliangSavageScript : ScriptCardTemplate
{
	public const int BaseBlock = 12;
	public const int UpgradedBlock = 16;
	public const int BaseVigor = 3;
	public const int UpgradedVigor = 5;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BaseBlock, ValueProp.Move),
		new PowerVar<VigorPower>(BaseVigor),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/XiliangSavageScript.png");

	public XiliangSavageScript()
		: base(2, CardType.Skill, CardRarity.Common, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptXiliangSavagePower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
	}
}
