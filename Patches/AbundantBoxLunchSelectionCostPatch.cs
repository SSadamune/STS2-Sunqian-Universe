using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Script;

#nullable enable

namespace Squ.Patches;

/// <summary>Refreshes the Abundant Box Lunch energy cost in the selection screen footer.</summary>
[HarmonyPatch(typeof(NCombatPileCardSelectScreen), "UpdateConfirmButton")]
internal static class AbundantBoxLunchSelectionCostPatch
{
	private static void Postfix(
		CardSelectorPrefs ____prefs,
		HashSet<CardModel> ____selectedCards,
		MegaRichTextLabel ____infoLabel)
	{
		if (____prefs.Prompt.LocTable != "cards"
			|| ____prefs.Prompt.LocEntryKey != ScriptSystem.PaidSelectionPromptKey
			|| !____prefs.Prompt.Variables.TryGetValue(
				ScriptSystem.FreeSelectionCountVariable,
				out object? freeSelectionValue)
			|| freeSelectionValue is not decimal freeSelectionDecimal
			|| !____prefs.Prompt.Variables.TryGetValue(
				ScriptSystem.EnergyPrefixVariable,
				out object? energyPrefixValue)
			|| energyPrefixValue is not string energyPrefix)
		{
			return;
		}

		int freeSelectionCount = (int)freeSelectionDecimal;
		int energyCost = System.Math.Max(0, ____selectedCards.Count - freeSelectionCount);
		____infoLabel.Text = ScriptSystem.CreatePaidSelectionPrompt(
			freeSelectionCount,
			energyCost,
			energyPrefix).GetFormattedText();
	}
}
