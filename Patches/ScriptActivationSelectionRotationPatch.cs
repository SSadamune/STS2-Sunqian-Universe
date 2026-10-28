using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Squ.Cards;
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
/// The vanilla grid ignores a newly clicked card once MaxSelect is reached.
/// Opted-in prompts rotate the oldest selection out before vanilla handles the
/// click, then still require the confirm button.
/// </summary>
internal static class CardGridSelectionRotation
{
	internal static CardModel? FindScriptConstraintRemoval(
		IReadOnlyCollection<CardModel> selected, CardModel clicked, IReadOnlyList<CardModel> ordered)
	{
		if (selected.Contains(clicked) || clicked is not ScriptRecordCardTemplate incoming)
		{
			return null;
		}
		CardModel? conflict = selected.OfType<ScriptRecordCardTemplate>().FirstOrDefault(record =>
			incoming.MutuallyExclusiveGroup is not null
			&& record.MutuallyExclusiveGroup == incoming.MutuallyExclusiveGroup);
		if (conflict is not null)
		{
			return conflict;
		}
		if (incoming.CountsTowardActivationLimit
			&& selected.OfType<ScriptRecordCardTemplate>().Count(record => record.CountsTowardActivationLimit)
				>= ScriptSystem.GetActivationLimit(incoming.Owner))
		{
			return ordered.Concat(selected).OfType<ScriptRecordCardTemplate>().FirstOrDefault(record =>
				selected.Contains(record) && record.CountsTowardActivationLimit);
		}
		return null;
	}
	private static readonly ConditionalWeakTable<
		NCardGridSelectionScreen,
		ScriptActivationSelectionOrder> SelectionOrders = new();

	internal static bool UsesRotation(CardSelectorPrefs prefs)
	{
		string key = prefs.Prompt.LocEntryKey;
		if (prefs.Prompt.LocTable == SquCommonL10n.Table)
		{
			return key == ScriptSystem.SelectionPromptKey
				|| key == ScriptSystem.ReselectionPromptKey;
		}

		return prefs.Prompt.LocTable == "cards"
			&& (key == FarmingGeneral.SelectionPromptKey
				|| key == GoldenUprising.SelectionPromptKey);
	}

	internal static void BeforeClick(
		NCardGridSelectionScreen screen,
		CardModel card,
		CardSelectorPrefs prefs,
		HashSet<CardModel> selectedCards)
	{
		if (!UsesRotation(prefs))
		{
			return;
		}

		ScriptActivationSelectionOrder order = SelectionOrders.GetValue(
			screen,
			_ => new ScriptActivationSelectionOrder());
		order.MakeRoomFor(selectedCards, card, int.MaxValue);
		CardModel? removed = FindScriptConstraintRemoval(selectedCards, card, order.Cards)
			?? order.MakeRoomFor(selectedCards, card, prefs.MaxSelect);
		if (removed is null || !selectedCards.Remove(removed))
		{
			return;
		}

		screen.GetNode<NCardGrid>("%CardGrid").UnhighlightCard(removed);
	}

	internal static void AfterClick(
		NCardGridSelectionScreen screen,
		CardModel card,
		CardSelectorPrefs prefs,
		HashSet<CardModel> selectedCards)
	{
		if (!UsesRotation(prefs))
		{
			return;
		}

		SelectionOrders.GetValue(screen, _ => new ScriptActivationSelectionOrder())
			.RecordResult(selectedCards, card);
	}
}

[HarmonyPatch(typeof(NCombatPileCardSelectScreen), "OnCardClicked")]
internal static class ScriptActivationSelectionRotationPatch
{
	[HarmonyPrefix]
	private static void Prefix(
		NCombatPileCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards) =>
		CardGridSelectionRotation.BeforeClick(__instance, card, ____prefs, ____selectedCards);

	[HarmonyPostfix]
	private static void Postfix(
		NCombatPileCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards) =>
		CardGridSelectionRotation.AfterClick(__instance, card, ____prefs, ____selectedCards);
}

[HarmonyPatch(typeof(NSimpleCardSelectScreen), "OnCardClicked")]
internal static class SimpleCardSelectionRotationPatch
{
	[HarmonyPrefix]
	private static void Prefix(
		NSimpleCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards) =>
		CardGridSelectionRotation.BeforeClick(__instance, card, ____prefs, ____selectedCards);

	[HarmonyPostfix]
	private static void Postfix(
		NSimpleCardSelectScreen __instance,
		CardModel card,
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards) =>
		CardGridSelectionRotation.AfterClick(__instance, card, ____prefs, ____selectedCards);
}
