using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>鸡脚芝士剧本悬停提示中展示的强化版打击；不进入任何卡池。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "chicken_foot_cheese_script_strike_preview")]
public sealed class ChickenFootCheeseScriptStrikePreview : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 6;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/StrikeLongtao.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public ChickenFootCheeseScriptStrikePreview()
		: base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => ScriptChickenFootCheesePower.RedirectRandomEnemyCount;

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			this,
			choiceContext,
			GetRandomEnemyTargetCount(),
			hitCountPerTarget: IsUpgraded
				? ScriptChickenFootCheesePower.UpgradedHitCount
				: ScriptChickenFootCheesePower.BaseHitCount,
			cardPlay: cardPlay);
}
