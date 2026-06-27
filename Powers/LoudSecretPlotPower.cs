using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 大声密谋：持有者打出以全体敌人或随机多名敌人为目标的攻击/技能牌，且场上仅有一名可为目标的敌人时，
/// 该牌额外结算等同于层数的次数（<see cref="ModifyCardPlayCount"/>）。
/// </summary>
[RegisterPower]
public sealed class LoudSecretPlotPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/LoudSecretPlotPower.png",
		BigIconPath: "res://images/powers/LoudSecretPlotPowerBig.png");

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != Owner || !ShouldReplay(card))
		{
			return playCount;
		}

		return playCount + Amount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner.Creature == Owner && ShouldReplay(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private bool ShouldReplay(CardModel card)
	{
		if (!IsQualifyingCardType(card) || !HasMultiTargetIntent(card))
		{
			return false;
		}

		ICombatState? combatState = CombatState;
		return combatState != null && combatState.HittableEnemies.Count == 1;
	}

	private static bool IsQualifyingCardType(CardModel card) =>
		card.Type is CardType.Attack or CardType.Skill;

	private static bool HasMultiTargetIntent(CardModel card)
	{
		if (card.TargetType == TargetType.AllEnemies)
		{
			return true;
		}

		if (card is ISoloMultitargetReplayOptIn optIn)
		{
			return optIn.QualifiesForSoloMultitargetReplay();
		}

		return false;
	}
}
