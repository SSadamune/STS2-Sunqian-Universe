using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
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
	/// <summary>
	/// Dynamic variables whose mutable base value contains combat-only Charge must compare their
	/// preview against the printed value, rather than against that mutable base value.
	/// </summary>
	protected sealed class ChargedEnergyVar(int value, Func<CardModel, decimal> printedValue) : EnergyVar(value)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			EnchantedValue = printedValue(card);
			PreviewValue = BaseValue;
		}
	}

	protected sealed class ChargedCardsVar(int value, Func<CardModel, decimal> printedValue) : CardsVar(value)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			EnchantedValue = printedValue(card);
			PreviewValue = BaseValue;
		}
	}

	protected sealed class ChargedRepeatVar(int value, Func<CardModel, decimal> printedValue) : RepeatVar(value)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			EnchantedValue = printedValue(card);
			PreviewValue = BaseValue;
		}
	}

	protected sealed class ChargedDamageVar(
		decimal value,
		ValueProp props,
		Func<CardModel, decimal> printedValue) : DamageVar(value, props)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal printed = ApplyEnchantment(card, printedValue(card));
			decimal preview = ApplyEnchantment(card, BaseValue);
			EnchantedValue = printed;

			if (runGlobalHooks)
			{
				preview = Hook.ModifyDamage(
					card.Owner.RunState,
					card.CombatState,
					target,
					card.Owner.Creature,
					BaseValue,
					Props,
					card,
					null,
					ModifyDamageHookType.All,
					previewMode,
					out _);
			}

			PreviewValue = preview;
		}

		private decimal ApplyEnchantment(CardModel card, decimal value)
		{
			if (card.Enchantment is not { } enchantment)
			{
				return value;
			}

			value += enchantment.EnchantDamageAdditive(value, Props);
			return value * enchantment.EnchantDamageMultiplicative(value, Props);
		}
	}

	protected sealed class ChargedBlockVar(
		decimal value,
		ValueProp props,
		Func<CardModel, decimal> printedValue) : BlockVar(value, props)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal printed = ApplyEnchantment(card, printedValue(card));
			decimal preview = ApplyEnchantment(card, BaseValue);
			EnchantedValue = printed;

			if (runGlobalHooks && card.CombatState is { } combatState)
			{
				preview = Hook.ModifyBlock(
					combatState,
					card.Owner.Creature,
					BaseValue,
					Props,
					card,
					null,
					out _);
			}

			PreviewValue = preview;
		}

		private static decimal ApplyEnchantment(CardModel card, decimal value)
		{
			if (card.Enchantment is not { } enchantment)
			{
				return value;
			}

			value += enchantment.EnchantBlockAdditive(value);
			return value * enchantment.EnchantBlockMultiplicative(value);
		}
	}

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

	/// <summary>
	/// 蓄能改写了伤害、抽牌、能量等动态变量后立刻重绘手牌描述。
	/// 《弹指打击》改耗能走 <see cref="NCard.PlayRandomizeCostAnim"/>，不要在动画开始前调用本方法。
	/// </summary>
	protected void RefreshCardVisuals()
	{
		if (Pile is not { } pile)
		{
			return;
		}

		NCard.FindOnTable(this)?.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
	}
}
