#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 不怕酸：保留易伤、虚弱与脆弱本身及其正常获得/倒计时，只抵消它们的数值倍率。
/// </summary>
[RegisterPower]
public sealed class NotAfraidOfAcidPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/NotAfraidOfAcidPower.png",
		BigIconPath: "res://images/powers/NotAfraidOfAcidPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<FrailPower>(),
	];

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		decimal multiplier = 1m;

		if (target == Owner && Owner.GetPower<VulnerablePower>() is { Amount: > 0 } vulnerable)
		{
			multiplier *= Reciprocal(vulnerable.ModifyDamageMultiplicative(
				target, amount, props, dealer, cardSource, cardPlay));
		}

		if (dealer == Owner && Owner.GetPower<WeakPower>() is { Amount: > 0 } weak)
		{
			multiplier *= Reciprocal(weak.ModifyDamageMultiplicative(
				target, amount, props, dealer, cardSource, cardPlay));
		}

		return multiplier;
	}

	public override decimal ModifyBlockMultiplicative(
		Creature target,
		decimal block,
		ValueProp props,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (target != Owner || Owner.GetPower<FrailPower>() is not { Amount: > 0 } frail)
		{
			return 1m;
		}

		return Reciprocal(frail.ModifyBlockMultiplicative(
			target, block, props, cardSource, cardPlay));
	}

	public override Task AfterModifyingDamageAmount(CardModel? cardSource)
	{
		Flash();
		return Task.CompletedTask;
	}

	public override Task AfterModifyingBlockAmount(
		decimal modifiedBlock,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		Flash();
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Amount <= 0)
		{
			return;
		}

		await PowerCmd.TickDownDuration(this);
	}

	private static decimal Reciprocal(decimal multiplier) =>
		multiplier == 0m ? 1m : 1m / multiplier;
}
