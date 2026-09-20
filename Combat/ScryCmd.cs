using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 一次[gold]预见[/gold]结算的结果，记录本次被弃掉的牌。
/// </summary>
public readonly record struct ScryResult
{
	private readonly IReadOnlyList<CardModel>? _discarded;

	public ScryResult(IReadOnlyList<CardModel> discarded) => _discarded = discarded;

	public IReadOnlyList<CardModel> Discarded => _discarded ?? [];

	public static ScryResult Empty => default;
}

/// <summary>
/// [gold]预见[/gold]命令：查看抽牌堆顶部的若干张牌，并可丢弃其中任意数量。
/// 数量会先经 <see cref="IModifyScryAmount"/> 修改，结算后触发 <see cref="IAfterScryed"/>。
/// </summary>
public static class ScryCmd
{
	/// <summary>
	/// 以卡牌上名为 "Scry" 的 <see cref="ScryVar"/> 作为基础数量执行预见。
	/// </summary>
	public static Task<ScryResult> Execute(PlayerChoiceContext choiceContext, CardModel card)
	{
		return Execute(choiceContext, card.Owner, card.DynamicVars.Scry().IntValue);
	}

	/// <param name="onCardChosen">
	/// 若提供，被玩家选中的每张牌改由此回调处理（例如改为消耗而非丢弃），
	/// 调用方需自行负责该牌的去向；不提供时使用默认的丢弃行为。
	/// </param>
	public static async Task<ScryResult> Execute(
		PlayerChoiceContext choiceContext,
		Player player,
		int amount,
		Func<PlayerChoiceContext, CardModel, Task>? onCardChosen = null)
	{
		var modifiedAmount = ScryHook.ModifyScryAmount(player, amount, out var modifiers);
		await ScryHook.AfterModifyingScryAmount(choiceContext, player, modifiers, amount, modifiedAmount);

		if (modifiedAmount <= 0) return ScryResult.Empty;

		var drawPile = PileType.Draw.GetPile(player);
		var combatState = player.Creature.CombatState;
		if (combatState == null) return ScryResult.Empty;

		var cardsToScry = drawPile.Cards.Take(modifiedAmount).ToList();
		if (cardsToScry.Count == 0) return ScryResult.Empty;

		var prefs = new CardSelectorPrefs(
			CardSelectorPrefs.DiscardSelectionPrompt,
			0,
			cardsToScry.Count);

		var cardsToDiscard = (await CardSelectCmd.FromSimpleGrid(
			choiceContext,
			cardsToScry,
			player,
			prefs)).ToList();

		if (onCardChosen == null)
		{
			await DiscardSelectedCards(choiceContext, player, combatState, cardsToDiscard);
		}
		else
		{
			foreach (var chosenCard in cardsToDiscard)
			{
				await onCardChosen(choiceContext, chosenCard);
			}
		}

		await ScryHook.AfterScryed(choiceContext, player, modifiedAmount, cardsToDiscard.Count, cardsToDiscard);
		return new ScryResult(cardsToDiscard);
	}

	private static async Task DiscardSelectedCards(
		PlayerChoiceContext choiceContext,
		Player player,
		ICombatState combatState,
		IReadOnlyList<CardModel> cards)
	{
		List<CardModel> slyCards = cards.Where(card => card.IsSlyThisTurn).ToList();
		var discardPile = PileType.Discard.GetPile(player);

		// 先完成全部移动，再分发弃牌 Hook。这样由弃牌触发的嵌套预见
		// 只能读取更新后的抽牌堆，不会再次看到本次已选中的牌。
		await CardPileCmd.Add(cards, discardPile);

		foreach (var card in cards)
		{
			CombatManager.Instance.History.CardDiscarded(combatState, card);
			await Hook.AfterCardDiscarded(combatState, choiceContext, card);
		}

		discardPile.InvokeContentsChanged();

		// 对齐 CardCmd.Discard：预见弃牌属于正式弃牌，并触发奇巧自动打出。
		foreach (var slyCard in slyCards)
		{
			await CardCmd.AutoPlay(choiceContext, slyCard, null, AutoPlayType.SlyDiscard);
		}
	}
}
