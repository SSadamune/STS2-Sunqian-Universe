using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 倾巢而出（All In）：打出时若手牌中仅剩该攻击/技能牌，其造成的伤害与获得的格挡增加 <see cref="Amount"/>%。
/// 层数叠加时 <see cref="Amount"/> 为各层加成百分比之和。
/// </summary>
[RegisterPower]
public sealed class AllInPower : ModPowerTemplate
{
	public const decimal BaseBonusPercent = 50m;
	public const decimal UpgradedBonusPercent = 75m;

	private CardModel? _boostedCard;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/AllInPower.png",
		BigIconPath: "res://images/powers/AllInPowerBig.png");

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		// 新一轮出牌（含上一次多段出牌被战斗中断后的残留标记）。
		if (cardPlay.PlayIndex == 0)
		{
			_boostedCard = null;
		}

		if (cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.PlayIndex != 0
			|| !IsQualifyingCard(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		// 牌已离手；若打出前手牌仅此一张，此刻手牌应为空。
		if (PileType.Hand.GetPile(cardPlay.Card.Owner).Cards.Count == 0)
		{
			_boostedCard = cardPlay.Card;
		}

		return Task.CompletedTask;
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card == _boostedCard && IsFinalPlayIteration(cardPlay))
		{
			_boostedCard = null;
		}

		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (!ShouldBoost(card, dealer))
		{
			return 1m;
		}

		Flash();
		return 1m + Amount / 100m;
	}

	public override decimal ModifyBlockMultiplicative(
		Creature target,
		decimal block,
		ValueProp props,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (!ShouldBoost(cardSource, target))
		{
			return 1m;
		}

		Flash();
		return 1m + Amount / 100m;
	}

	private bool ShouldBoost(CardModel? card, Creature? actor)
	{
		if (card is null || actor != Owner || card.Owner != Owner.Player || !IsQualifyingCard(card))
		{
			return false;
		}

		// 打出结算：BeforeCardPlayed 已标记。
		if (card == _boostedCard)
		{
			return true;
		}

		// 手牌预览：最后一张仍在手牌中。
		CardPile hand = PileType.Hand.GetPile(card.Owner);
		return hand.Cards.Count == 1 && hand.Cards[0] == card;
	}

	private static bool IsQualifyingCard(CardModel card) =>
		card.Type is CardType.Attack or CardType.Skill;

	/// <summary>
	/// 仅在整次出牌（含仁义双剑等多段结算）的最后一击后清除加成标记。
	/// <see cref="CardPlay.PlayCount"/> 异常为 0 时按单次出牌处理，避免标记泄漏。
	/// </summary>
	private static bool IsFinalPlayIteration(CardPlay cardPlay)
	{
		if (cardPlay.PlayCount <= 1)
		{
			return true;
		}

		return cardPlay.PlayIndex >= cardPlay.PlayCount - 1;
	}
}
