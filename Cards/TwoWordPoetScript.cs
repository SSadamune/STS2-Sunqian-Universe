using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "two_word_poet_script")]
public sealed class TwoWordPoetScript : ScriptCardTemplate
{
	public const int CanonicalDamage = 2;
	public const int CanonicalHits = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, ValueProp.Move),
		new SnapshotDamageVar(CanonicalDamage),
		new RepeatVar(CanonicalHits),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.StackableScript),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	public override TargetType TargetType =>
		IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

	public TwoWordPoetScript()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.TwoWordWineAndSongEvent);
		decimal snapshotDamagePerHit = ScriptTwoWordPoetPower.CalculateSnapshotDamagePerHit(
			this,
			Owner.Creature);

		AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this, cardPlay)
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

	/// <summary>
	/// 剧本行预览与 <see cref="ScriptTwoWordPoetPower.CalculateSnapshotDamagePerHit"/> 对齐：
	/// 含力量、活力等攻击方修正，不含当前指向目标的易伤、缓慢。
	/// </summary>
	private sealed class SnapshotDamageVar : DamageVar
	{
		public SnapshotDamageVar(decimal damage)
			: base(ScriptTwoWordPoetPower.SnapshotDamageVarName, damage, ValueProp.Move)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			BaseValue = card.DynamicVars.Damage.BaseValue;
			base.UpdateCardPreview(card, previewMode, target: null, runGlobalHooks);
			if (runGlobalHooks && card.Owner?.Creature is { } dealer)
			{
				PreviewValue = ScriptTwoWordPoetPower.CalculateSnapshotDamagePerHit(card, dealer);
			}
		}
	}
}
