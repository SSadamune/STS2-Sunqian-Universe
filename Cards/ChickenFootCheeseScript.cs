using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "chicken_foot_cheese_script")]
public sealed class ChickenFootCheeseScript : ScriptCardTemplate<ChickenFootCheeseScriptRecord>, IRandomEnemyTargetCount
{
	public const int BaseDamage = 6;
	public const int BaseFollowUpDamage = ScriptChickenFootCheesePower.BaseFollowUpDamage;
	public const int UpgradedFollowUpDamage = ScriptChickenFootCheesePower.UpgradedFollowUpDamage;
	public const int RandomEnemyTargetCount = 2;
	public const string FollowUpDamageVarName = "FollowUpDamage";
	public const string ScriptFollowUpDamageVarName = "ScriptFollowUpDamage";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new DamageVar(FollowUpDamageVarName, BaseFollowUpDamage, ValueProp.Move),
		new DynamicVar(ScriptFollowUpDamageVarName, BaseFollowUpDamage),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..SquCardHoverTipFactory.FromCardWithCardHoverTips<ChickenFootCheeseScriptStrikePreview>(this, IsUpgraded),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ChickenFootCheeseScript.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public ChickenFootCheeseScript()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
	{
	}

	public int GetRandomEnemyTargetCount() => RandomEnemyTargetCount;

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.ChickenFootCheeseScriptChenGongEvent);
		List<MegaCrit.Sts2.Core.Entities.Creatures.Creature> targets =
			SquRandomEnemyTargeting.SelectRandomEnemies(
				this,
				RandomEnemyTargetCount,
				cardPlay.Target);
		SquVigorSnapshot.AttackSequence damageSequence =
			SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);
		await SquRandomEnemyTargeting.ExecuteDamageAgainstTargets(
			this,
			choiceContext,
			targets,
			damageSequence.ResolveNextAttackDamage(),
			hitFx: null,
			cardPlay: cardPlay,
			beforeHit: SquHitVfx.SmallMagicMissiles);
		await SquRandomEnemyTargeting.ExecuteDamageAgainstTargets(
			this,
			choiceContext,
			targets,
			damageSequence.ResolveNextAttackDamage(DynamicVars[FollowUpDamageVarName].BaseValue),
			hitFx: null,
			cardPlay: cardPlay,
			beforeHit: SquHitVfx.SmallMagicMissiles);

		await UnlockScriptAsync(choiceContext);
	}

	protected override void OnUpgrade()
	{
		int increase = UpgradedFollowUpDamage - BaseFollowUpDamage;
		DynamicVars[FollowUpDamageVarName].UpgradeValueBy(increase);
		DynamicVars[ScriptFollowUpDamageVarName].UpgradeValueBy(increase);
	}

}
