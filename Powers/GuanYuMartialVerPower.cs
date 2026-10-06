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
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 武关羽：每回合第一张攻击牌结算后返还其消耗的活力；每花费 1 点能量获得活力。
/// </summary>
[RegisterPower]
public sealed class GuanYuMartialVerPower : ModPowerTemplate
{
	private sealed class Data
	{
		public CardModel? PendingAttack;

		public decimal VigorSpent;

		public bool RefundedThisTurn;

		public Dictionary<CardModel, int> PendingEnergySpent { get; } = [];
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public bool FormUpgraded { get; private set; }

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.ForEnergy(this),
	];

	public void SetFormUpgraded(bool upgraded) => FormUpgraded = upgraded;

	protected override object InitInternalData() => new Data();

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (!ShouldTrackAttack(cardPlay))
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		data.PendingAttack = cardPlay.Card;
		data.VigorSpent = 0m;
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power is not VigorPower || power.Owner != Owner || amount >= 0m)
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		if (data.PendingAttack != null)
		{
			data.VigorSpent += -amount;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsLastInSeries || cardPlay.Card.Owner.Creature != Owner)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.PendingEnergySpent.Remove(cardPlay.Card, out int energySpent);

		decimal vigorSpent = 0m;
		if (ReferenceEquals(data.PendingAttack, cardPlay.Card))
		{
			vigorSpent = data.VigorSpent;
			data.PendingAttack = null;
			data.VigorSpent = 0m;
			data.RefundedThisTurn = true;
		}

		if (Owner.IsDead)
		{
			return;
		}

		if (vigorSpent > 0m)
		{
			Flash();
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				vigorSpent,
				Owner,
				cardPlay.Card);
			if (Owner.GetPower<VigorPower>() is { } restored)
			{
				AttackVigorResolution.ClearVigorAttackBinding(restored);
			}
		}

		if (Amount > 0 && energySpent > 0)
		{
			Flash();
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				Amount * energySpent,
				Owner,
				cardPlay.Card);
		}
	}

	public override Task AfterEnergySpent(CardModel card, int amount)
	{
		if (Owner.IsDead
			|| Amount <= 0
			|| amount <= 0
			|| card.Owner?.Creature != Owner)
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().PendingEnergySpent[card] = amount;
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.PendingAttack = null;
		data.VigorSpent = 0m;
		data.RefundedThisTurn = false;
		data.PendingEnergySpent.Clear();
		await GuanDiFormChoice.OfferRechoiceAsync(this, combatState);
	}

	private bool ShouldTrackAttack(CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.Card.Type != CardType.Attack
			|| ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(cardPlay.Card))
		{
			return false;
		}

		Data data = GetInternalData<Data>();
		return !data.RefundedThisTurn && data.PendingAttack == null;
	}
}
