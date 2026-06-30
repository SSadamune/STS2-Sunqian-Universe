using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 行刑指挥官剧本：场上仅有一名敌人时，持有者打出的以全体敌人或随机多名敌人为目标的攻击牌伤害提高 50%。
/// </summary>
[RegisterPower]
public sealed class ScriptExecutionCommanderPower : ScriptPowerTemplate
{
	public const decimal DamageMultiplier = 1.5m;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptExecutionCommanderPower.png",
		BigIconPath: "res://images/powers/ScriptExecutionCommanderPowerBig.png");

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card)
	{
		if (!ShouldBoost(card, dealer) || !props.IsPoweredAttack())
		{
			return 1m;
		}

		return DamageMultiplier;
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
