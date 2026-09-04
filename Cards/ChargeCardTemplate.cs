using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 「蓄能」牌：手牌中生效的效果由 <see cref="Charge"/> 传入，打出后统一解除。
/// 回合结束降费等不走原版 <c>HasTurnEndInHandEffect</c>（灼烧/悔恨那条：飞到场中再强制进弃牌，会盖掉保留）。
/// </summary>
public abstract class ChargeCardTemplate : ModCardTemplate
{
	public readonly record struct ChargeHooks(
		Func<PlayerChoiceContext, Task>? OnTurnEndInHand,
		Func<PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?, Task>? OnPowerAmountChanged,
		Action Clear);

	protected ChargeCardTemplate(
		int cost,
		CardType type,
		CardRarity rarity,
		TargetType targetType)
		: base(cost, type, rarity, targetType)
	{
	}

	/// <summary>该牌蓄能正文的 loc 键（不含「蓄能&lt; &gt;」外壳）。</summary>
	protected abstract string ChargeEffectLocKey { get; }

	/// <summary>各牌自己的蓄能结算（降费、加伤等）。</summary>
	protected abstract ChargeHooks Charge { get; }

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Charge,
	];

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("ChargeText", SquKeywords.FormatChargeCardText(this, ChargeEffectLocKey));
	}

	public override Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Pile?.Type != PileType.Hand || Charge.OnTurnEndInHand is null)
		{
			return Task.CompletedTask;
		}

		return Charge.OnTurnEndInHand(choiceContext);
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (Pile?.Type != PileType.Hand || Charge.OnPowerAmountChanged is null)
		{
			return Task.CompletedTask;
		}

		return Charge.OnPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card != this || cardPlay.PlayIndex != cardPlay.PlayCount - 1)
		{
			return Task.CompletedTask;
		}

		Charge.Clear();
		return Task.CompletedTask;
	}
}
