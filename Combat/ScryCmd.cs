using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
/// [gold]预见[/gold]命令：查看抽牌堆顶部的若干张牌，并可将其中任意数量置入弃牌堆。
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

	public static async Task<ScryResult> Execute(PlayerChoiceContext choiceContext, Player player, int amount)
	{
		var modifiedAmount = ScryHook.ModifyScryAmount(player, amount, out var modifiers);
		await ScryHook.AfterModifyingScryAmount(choiceContext, player, modifiers, amount, modifiedAmount);

		if (modifiedAmount <= 0) return ScryResult.Empty;

		var drawPile = PileType.Draw.GetPile(player);
		var discardPile = PileType.Discard.GetPile(player);
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

		foreach (var discardedCard in cardsToDiscard)
		{
			await CardPileCmd.Add(discardedCard, discardPile);
			CombatManager.Instance.History.CardDiscarded(combatState, discardedCard);
			await Hook.AfterCardDiscarded(combatState, choiceContext, discardedCard);
		}

		discardPile.InvokeContentsChanged();

		await ScryHook.AfterScryed(choiceContext, player, modifiedAmount, cardsToDiscard.Count, cardsToDiscard);
		return new ScryResult(cardsToDiscard);
	}
}
