using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "two_word_poet_script")]
public sealed class TwoWordPoetScript : ScriptCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2m, ValueProp.Move),
		new RepeatVar(2),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<ScriptTwoWordPoetPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	public override TargetType TargetType =>
		IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

	public TwoWordPoetScript()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, false)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal snapshotDamagePerHit = ScriptTwoWordPoetPower.CalculateSnapshotDamagePerHit(
			this,
			Owner.Creature);

		AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this)
			.WithHitFx("vfx/vfx_attack_slash");

		if (IsUpgraded)
		{
			ArgumentNullException.ThrowIfNull(CombatState, "CombatState");
			await attack.TargetingAllOpponents(CombatState).Execute(choiceContext);
		}
		else
		{
			ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
			await attack.Targeting(cardPlay.Target).Execute(choiceContext);
		}

		await PowerCmd.Apply<ScriptTwoWordPoetPower>(
			choiceContext,
			Owner.Creature,
			snapshotDamagePerHit,
			Owner.Creature,
			this);
	}
}
