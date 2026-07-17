using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 可叠层剧本：行刑指挥官。场上仅有一名敌人时，持有者打出的群体目标攻击牌伤害提高
/// <see cref="Amount"/>%（未升级剧本叠 +50，升级叠 +75）。
/// </summary>
[RegisterPower]
public sealed class ScriptExecutionCommanderPower : StackableScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptExecutionCommanderPower.png",
		BigIconPath: "res://images/powers/ScriptExecutionCommanderPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..base.AdditionalHoverTips,
		HoverTipFactory.FromKeyword(SquKeywords.MultiTarget),
	];

	protected override void OnStackedFrom(CardModel? cardSource)
	{
	}

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (Amount <= 0m || !ShouldBoost(card, dealer) || !props.IsPoweredAttack())
		{
			return 1m;
		}

		return 1m + Amount / 100m;
	}

	private bool ShouldBoost(CardModel? card, Creature? dealer)
	{
		if (dealer != Owner || card is null || card.Type != CardType.Attack)
		{
			return false;
		}

		if (!MultiTargetCardIntent.HasMultiTargetIntent(card))
		{
			return false;
		}

		ICombatState? combatState = CombatState;
		return combatState != null && combatState.HittableEnemies.Count == 1;
	}
}
