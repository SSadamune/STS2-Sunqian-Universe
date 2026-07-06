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
/// 倾巢而出（All In）：打出时若手牌中仅剩该攻击/技能牌，其造成的伤害与获得的格挡翻倍。
/// <para>
/// 原版在 <c>OnPlay</c> 之前会调用 <c>CardPileCmd.AddDuringManualCardPlay</c> 将牌移出手牌，
/// 故打出时无法在手牌中见到该牌；「打出前手牌仅余此牌」等价于
/// <c>BeforeCardPlayed</c> 首次结算时手牌为空。
/// </para>
/// </summary>
[RegisterPower]
public sealed class AllInPower : ModPowerTemplate
{
	public const decimal Multiplier = 2m;

	private CardModel? _boostedCard;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/AllInPower.png",
		BigIconPath: "res://images/powers/AllInPowerBig.png");

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
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
		if (cardPlay.Card == _boostedCard)
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
		if (!ShouldDouble(card, dealer))
		{
			return 1m;
		}

		Flash();
		return Multiplier;
	}

	public override decimal ModifyBlockMultiplicative(
		Creature target,
		decimal block,
		ValueProp props,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (!ShouldDouble(cardSource, target))
		{
			return 1m;
		}

		Flash();
		return Multiplier;
	}

	private bool ShouldDouble(CardModel? card, Creature? actor) =>
		card is not null
		&& actor == Owner
		&& card == _boostedCard
		&& IsQualifyingCard(card);

	private static bool IsQualifyingCard(CardModel card) =>
		card.Type is CardType.Attack or CardType.Skill;
}
