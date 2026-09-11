using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 手牌中生效、打出后解除的效果由 <see cref="Charge"/> 传入。
/// 被保留时触发走 <see cref="AfterFlush"/>（与 WatcherMod《时之沙》相同），
/// 不走原版 <c>HasTurnEndInHandEffect</c>（灼烧/悔恨那条：飞到场中再强制进弃牌，会盖掉保留）。
/// </summary>
public abstract class ChargeCardTemplate : ModCardTemplate
{
	public readonly record struct ChargeHooks(
		Func<PlayerChoiceContext, Task>? OnRetained,
		Func<PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?, Task>? OnPowerAmountChanged,
		Action Clear,
		Func<PlayerChoiceContext, CardPlay, Task>? OnCardPlayed = null,
		Func<PlayerChoiceContext, Task>? OnAttacked = null);

	protected ChargeCardTemplate(
		int cost,
		CardType type,
		CardRarity rarity,
		TargetType targetType)
		: base(cost, type, rarity, targetType)
	{
	}

	/// <summary>该牌蓄能正文的 loc 键（不含「蓄能：」外壳）。</summary>
	protected abstract string ChargeEffectLocKey { get; }

	/// <summary>各牌自己的蓄能结算（降费、加伤等）。</summary>
	protected abstract ChargeHooks Charge { get; }

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Charge,
	];

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
		SquKeywords.AddNestedLoc(
			description,
			"ChargeText",
			SquKeywords.FormatChargeCardText(this, ChargeEffectLocKey));
	}

	public override bool ShouldReceiveCombatHooks => true;

	public override Task AfterFlush(
		PlayerChoiceContext choiceContext,
		Player player,
		IReadOnlyCollection<CardModel> flushedCards,
		IReadOnlyCollection<CardModel> retainedCards)
	{
		if (player != Owner || Charge.OnRetained is null || !retainedCards.Contains(this))
		{
			return Task.CompletedTask;
		}

		return Charge.OnRetained(choiceContext);
	}

	/// <summary>
	/// 对齐原版 <c>ThornsPower</c>：在受到攻击伤害结算前触发（含被格挡），每次命中一次。
	/// </summary>
	public override Task BeforeDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (Charge.OnAttacked is null
			|| Pile?.Type != PileType.Hand
			|| target != Owner.Creature
			|| dealer is null
			|| !(props.IsPoweredAttack() || cardSource is Omnislice))
		{
			return Task.CompletedTask;
		}

		return Charge.OnAttacked(choiceContext);
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
		if (cardPlay.Card == this)
		{
			if (cardPlay.PlayIndex == cardPlay.PlayCount - 1)
			{
				Charge.Clear();
			}

			return Task.CompletedTask;
		}

		if (Pile?.Type != PileType.Hand || Charge.OnCardPlayed is null)
		{
			return Task.CompletedTask;
		}

		return Charge.OnCardPlayed(choiceContext, cardPlay);
	}
}
