using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 活力保持：消耗活力后立刻获得等量活力。层数仅在攻击牌结算后减少。
/// </summary>
[RegisterPower]
public sealed class KeepVigorPower : ModPowerTemplate
{
	private bool _isRefunding;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/KeepVigorPower.png",
		BigIconPath: "res://images/powers/KeepVigorPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (_isRefunding
			|| Owner.IsDead
			|| Amount <= 0m
			|| power is not VigorPower
			|| power.Owner != Owner
			|| amount >= 0m)
		{
			return;
		}

		_isRefunding = true;
		try
		{
			Flash();
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				-amount,
				Owner,
				cardSource);
		}
		finally
		{
			_isRefunding = false;
		}
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| cardPlay.Card.Type != CardType.Attack
			|| ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(cardPlay.Card))
		{
			return;
		}

		Flash();
		await PowerCmd.Decrement(this);
	}
}
