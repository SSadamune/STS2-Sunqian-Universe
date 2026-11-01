using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
using MegaCrit.Sts2.Core.Localization;
using Squ.Cards;
using Squ.Combat;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 活力增幅：本回合下一张实际消耗活力或酒力的攻击牌获得额外加成。
/// 层数是这一张牌的倍率，不是能作用的牌数。同一张牌两种资源都消耗，也只结算这一次。
/// </summary>
[RegisterPower]
public sealed class VigorAmplificationPower : ModPowerTemplate
{
	internal bool OnlyFirstAttackThisTurn { get; set; }
	public const int BonusStacksPerForm = 1;

	private sealed class Data
	{
		public CardModel? ActiveCard { get; set; }

		public AttackResourceSpendTransaction? ResourceSpend { get; set; }
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/VigorAmplificationPower.png",
		BigIconPath: "res://images/powers/VigorAmplificationPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		..NewsanguoDrunkenMightEffect.HoverTips(),
	];

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			NewsanguoDrunkenMightEffect.AddOrText(description);
			return description;
		}
	}

	protected override object InitInternalData() => new Data();

	public int GetBonusMultiplierFor(CardModel card)
	{
		if (Owner.IsDead
			|| Amount <= 0
			|| card.Owner?.Creature != Owner
			|| card.Type != CardType.Attack
			|| ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(card))
		{
			return 0;
		}

		CardModel? activeCard = GetInternalData<Data>().ActiveCard;
		if (OnlyFirstAttackThisTurn && activeCard is null
			&& CombatState is { } state
			&& TurnCardHistory.Get(state).CountAttacks(card.Owner, 1,
				card.Pile?.Type == PileType.Play ? card : null) > 0)
		{
			return 0;
		}
		return activeCard is null || ReferenceEquals(activeCard, card)
			? Amount
			: 0;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		Data data = GetInternalData<Data>();
		if (data.ActiveCard is not null
			|| cardPlay.PlayIndex != 0
			|| GetBonusMultiplierFor(cardPlay.Card) <= 0
			|| SupremeGeneralKeywordSystem.ShouldSuppressSameOwnerChildAttackResources(
				cardPlay.Card,
				cardPlay))
		{
			return Task.CompletedTask;
		}

		data.ActiveCard = cardPlay.Card;
		data.ResourceSpend = new AttackResourceSpendTransaction(Owner, cardPlay.Card);
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		Data data = GetInternalData<Data>();
		if (!Owner.IsDead
			&& data.ActiveCard is not null
			&& data.ResourceSpend is not null)
		{
			data.ResourceSpend.Record(power, amount, cardSource);
		}

		return Task.CompletedTask;
	}

	public override Task BeforeAttack(AttackCommand command)
	{
		if (command.Attacker == Owner
			&& command.ModelSource is CardModel card
			&& AppliesToActiveCard(card)
			&& command.DamageProps.IsPoweredAttack()
			&& (GetVigorAmount() > 0m || NewsanguoDrunkenMightEffect.GetAmount(Owner) > 0))
		{
			Flash();
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
		int bonusMultiplier = card is null
			? 0
			: GetAppliedMultiplier(card, cardPlay);
		if (dealer != Owner
			|| bonusMultiplier <= 0
			|| !props.IsPoweredAttack())
		{
			return 0m;
		}

		return (GetVigorAmount() + NewsanguoDrunkenMightEffect.GetAmount(Owner)) * bonusMultiplier;
	}

	public override async Task AfterCardPlayedLate(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		Data data = GetInternalData<Data>();
		if (!cardPlay.IsLastInSeries
			|| !ReferenceEquals(data.ActiveCard, cardPlay.Card))
		{
			return;
		}

		bool spentResource = data.ResourceSpend is { SpentAny: true };
		data.ActiveCard = null;
		data.ResourceSpend = null;
		if (spentResource || OnlyFirstAttackThisTurn)
		{
			await PowerCmd.Remove(this);
		}
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side == Owner.Side && participants.Contains(Owner))
		{
			await PowerCmd.Remove(this);
		}
	}

	private bool AppliesToActiveCard(CardModel card) =>
		ReferenceEquals(GetInternalData<Data>().ActiveCard, card);

	private int GetAppliedMultiplier(CardModel card, CardPlay? cardPlay)
	{
		if (AppliesToActiveCard(card))
		{
			return Amount;
		}

		return cardPlay is null
			&& card.Pile?.Type is PileType.Hand or PileType.Play
				? GetBonusMultiplierFor(card)
				: 0;
	}

	private decimal GetVigorAmount() =>
		Owner.GetPower<VigorPower>()?.Amount ?? 0m;
}
