#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>吾亦过江：攻击牌获得第二份活力与力量加成。</summary>
[RegisterPower]
public sealed class CrossTheRiverPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/CrossTheRiverPower.png",
		BigIconPath: "res://images/powers/CrossTheRiverPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override Task BeforeAttack(AttackCommand command)
	{
		if (command.Attacker == Owner
			&& command.DamageProps.IsPoweredAttack()
			&& command.ModelSource is CardModel { Type: CardType.Attack } card
			&& GetDamageModifier(card) != 0m)
		{
			SquSfx.PlayRandom(CombatState?.RunState, SquSfx.CrossTheRiverTriggerEvents);
		}

		return Task.CompletedTask;
	}

	public override decimal ModifyDamageAdditive(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (dealer != Owner
			|| card?.Type != CardType.Attack
			|| !props.IsPoweredAttack())
		{
			return 0m;
		}

		return GetDamageModifier(card);
	}

	private decimal GetDamageModifier(CardModel card) =>
		(DigRaid.DamageReceivesVigor(card) ? GetVigorAmount() : 0m)
		+ GetStrengthAmount();

	private decimal GetVigorAmount() =>
		Owner.GetPower<VigorPower>()?.Amount ?? 0m;

	private decimal GetStrengthAmount() =>
		Owner.GetPower<StrengthPower>()?.Amount ?? 0m;

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side == Owner.Side && participants.Contains(Owner) && Amount > 0)
		{
			await PowerCmd.TickDownDuration(this);
		}
	}
}
