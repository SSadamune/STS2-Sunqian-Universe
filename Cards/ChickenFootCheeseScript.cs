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
public sealed class ChickenFootCheeseScript : ScriptCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 6;
	public const int RandomEnemyTargetCount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<ChickenFootCheeseScriptStrikePreview>(IsUpgraded),
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
		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			this,
			choiceContext,
			RandomEnemyTargetCount,
			hitCountPerTarget: IsUpgraded
				? ScriptChickenFootCheesePower.UpgradedHitCount
				: ScriptChickenFootCheesePower.BaseHitCount,
			cardPlay: cardPlay);

		await PowerCmd.Apply<ScriptChickenFootCheesePower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

}
