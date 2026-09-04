using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "executioner_script")]
public sealed class ExecutionerScript : ScriptCardTemplate
{
	public const int BaseVigor = 6;
	public const int UpgradedVigor = 8;

	public const int VulnerableAmount = 2;

	public const int UpgradedVulnerableAmount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
		new PowerVar<VulnerablePower>(VulnerableAmount),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ExecutionerScript.png");

	public ExecutionerScript()
		: base(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<VulnerablePower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(VulnerablePower)].BaseValue,
			Owner.Creature,
			this);

		await PowerCmd.Apply<ScriptExecutionerPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(UpgradedVulnerableAmount - VulnerableAmount);
	}
}
