using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Squ.Script;

#nullable enable

namespace Squ.Patches;

internal sealed class ScriptActivationSelectionOrder
{
	private readonly List<CardModel> _cards = [];

	internal IReadOnlyList<CardModel> Cards => _cards;

	internal CardModel? MakeRoomFor(
		IReadOnlyCollection<CardModel> selectedCards,
		CardModel clickedCard,
		int maxSelect)
	{
		Reconcile(selectedCards);
		if (maxSelect <= 0
			|| selectedCards.Contains(clickedCard)
			|| selectedCards.Count < maxSelect)
		{
			return null;
		}

		CardModel? oldest = _cards.FirstOrDefault(selectedCards.Contains)
			?? selectedCards.FirstOrDefault();
		if (oldest is not null)
		{
			_cards.Remove(oldest);
		}
		return oldest;
	}

	internal void RecordResult(
		IReadOnlyCollection<CardModel> selectedCards,
		CardModel clickedCard)
	{
		Reconcile(selectedCards);
		_cards.Remove(clickedCard);
		if (selectedCards.Contains(clickedCard))
		{
			_cards.Add(clickedCard);
		}
	}

	private void Reconcile(IReadOnlyCollection<CardModel> selectedCards)
	{
		_cards.RemoveAll(card => !selectedCards.Contains(card));
		foreach (CardModel selected in selectedCards)
		{
			if (!_cards.Contains(selected))
			{
				_cards.Add(selected);
			}
		}
	}
}

/// <summary>
/// The vanilla selector ignores a newly clicked card once MaxSelect is reached.
/// Script activation instead rotates the oldest selection out before vanilla handles
/// the click, preserving ordinary toggle and confirmation behavior.
/// </summary>
[HarmonyPatch(typeof(NCombatPileCardSelectScreen), "OnCardClicked")]
internal static class ScriptActivationSelectionRotationPatch
{
	private static readonly ConditionalWeakTable<
		NCombatPileCardSelectScreen,
		ScriptActivationSelectionOrder> SelectionOrders = new();

	[HarmonyPrefix]
	private static void Prefix(
		NCombatPileCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards)
	{
		if (!IsScriptActivationPrompt(____prefs))
		{
			return;
		}

		ScriptActivationSelectionOrder order = SelectionOrders.GetValue(
			__instance,
			_ => new ScriptActivationSelectionOrder());
		CardModel? removed = order.MakeRoomFor(
			____selectedCards,
			card,
			____prefs.MaxSelect);
		if (removed is null || !____selectedCards.Remove(removed))
		{
			return;
		}

		__instance.GetNode<NCardGrid>("%CardGrid").UnhighlightCard(removed);
	}

	[HarmonyPostfix]
	private static void Postfix(
		NCombatPileCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards)
	{
		if (!IsScriptActivationPrompt(____prefs))
		{
			return;
		}

		SelectionOrders.GetValue(
			__instance,
			_ => new ScriptActivationSelectionOrder())
			.RecordResult(____selectedCards, card);
	}

	private static bool IsScriptActivationPrompt(CardSelectorPrefs prefs) =>
		prefs.Prompt.LocTable == "cards"
		&& (prefs.Prompt.LocEntryKey == ScriptSystem.SelectionPromptKey
			|| prefs.Prompt.LocEntryKey == ScriptSystem.PaidSelectionPromptKey);
}
