using System;
using System.Collections.Generic;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Cards;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ.Patches;

internal static class ScriptRecordCardHighlight
{
	internal static readonly Color ActiveColor = new(1f, 1f, 1f, 0.98f);

	internal static bool ShouldHighlight(CardModel? model) =>
		model is ScriptRecordCardTemplate { ShouldGlowGold: true };

	internal static Color ResolveColor(bool isActive, bool isSelected) =>
		isActive && !isSelected
			? ActiveColor
			: NCardHighlight.playableColor;

	/// <summary>
	/// Set the color without changing visibility. NCardGrid owns ordinary selection
	/// visibility, so inactive records selected by the player must remain highlighted.
	/// </summary>
	internal static bool ApplyColor(NCard? card)
	{
		if (card is null)
		{
			return false;
		}
		if (card.Model is not ScriptRecordCardTemplate record)
		{
			card.CardHighlight.Modulate = NCardHighlight.playableColor;
			return false;
		}

		bool shouldHighlight = ShouldHighlight(record);
		card.CardHighlight.Modulate = ResolveColor(shouldHighlight, isSelected: false);
		return shouldHighlight;
	}

	internal static void ShowSelected(NCard? card)
	{
		if (card is null)
		{
			return;
		}

		card.CardHighlight.Modulate = ResolveColor(
			ShouldHighlight(card.Model),
			isSelected: true);
		card.CardHighlight.AnimShow();
	}

	internal static void ShowIfActive(NCard? card)
	{
		if (ApplyColor(card))
		{
			card!.CardHighlight.AnimShow();
		}
	}

	internal static void RestoreHighlights(
		IEnumerable<NGridCardHolder> holders,
		ICollection<CardModel> selectedCards)
	{
		foreach (NGridCardHolder holder in holders)
		{
			if (selectedCards.Contains(holder.CardModel))
			{
				ShowSelected(holder.CardNode);
			}
			else
			{
				ShowIfActive(holder.CardNode);
			}
		}
	}
}

/// <summary>
/// The first grid allocation creates NCard instances directly and never calls
/// ReassignToCard or AssignCardsToRow. Apply active-script highlights after that
/// initial allocation has finished.
/// </summary>
[HarmonyPatch(typeof(NCardGrid), "InitGrid", new Type[] { })]
internal static class ScriptRecordCardGridInitialHighlightPatch
{
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(
		NCardGrid __instance,
		List<CardModel> ____highlightedCards) =>
		ScriptRecordCardHighlight.RestoreHighlights(
			__instance.CurrentlyDisplayedCardHolders,
			____highlightedCards);
}

/// <summary>
/// Pile and selection grids recycle card holders. Reset the color whenever a holder
/// receives a card; the grid still makes the final visibility decision afterwards.
/// </summary>
[HarmonyPatch(typeof(NCardHolder), nameof(NCardHolder.ReassignToCard))]
internal static class ScriptRecordCardHolderHighlightPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCardHolder __instance) =>
		ScriptRecordCardHighlight.ApplyColor(__instance.CardNode);
}

/// <summary>
/// In 0.111.0 AssignCardsToRow calls ReassignToCard and then immediately hides any
/// card absent from NCardGrid's private selection list. Reapply the persistent active
/// script highlight after that final grid-owned show/hide step.
/// </summary>
[HarmonyPatch(typeof(NCardGrid), "AssignCardsToRow")]
internal static class ScriptRecordCardGridRowHighlightPatch
{
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(
		List<NGridCardHolder> row,
		List<CardModel> ____highlightedCards) =>
		ScriptRecordCardHighlight.RestoreHighlights(row, ____highlightedCards);
}

/// <summary>Vanilla selection blue takes priority over the persistent active-script white.</summary>
[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.HighlightCard))]
internal static class ScriptRecordCardGridHighlightPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCardGrid __instance, CardModel card)
	{
		if (card is ScriptRecordCardTemplate)
		{
			ScriptRecordCardHighlight.ShowSelected(__instance.GetCardNode(card));
		}
	}
}

/// <summary>An active script remains marked after the selection UI toggles it off.</summary>
[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.UnhighlightCard))]
internal static class ScriptRecordCardGridUnhighlightPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCardGrid __instance, CardModel card)
	{
		if (ScriptRecordCardHighlight.ShouldHighlight(card))
		{
			ScriptRecordCardHighlight.ShowIfActive(__instance.GetCardNode(card));
		}
	}
}

/// <summary>履历牌在规则层仍按技能处理，但在卡面上显示自有类别“剧本”。</summary>
[HarmonyPatch(typeof(NCard), "UpdateTypePlaque")]
internal static class ScriptRecordCardVisualPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		if (__instance.Model is not ScriptRecordCardTemplate)
		{
			return;
		}

		MegaLabel typeLabel = __instance.GetNode<MegaLabel>("%TypeLabel");
		typeLabel.SetTextAutoSize(
			ModKeywordRegistry.GetTitle(SquKeywords.ScriptId).GetFormattedText());
	}
}

/// <summary>履历牌只作为剧本集中的标记卡，不显示任何能量费用元素。</summary>
[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
internal static class ScriptRecordCardEnergyVisualPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		bool isScriptRecord = __instance.Model is ScriptRecordCardTemplate;
		__instance.GetNode<MegaLabel>("%EnergyLabel").Visible = !isScriptRecord;
		if (!isScriptRecord)
		{
			return;
		}

		__instance.GetNode<TextureRect>("%EnergyIcon").Visible = false;
		__instance.GetNode<TextureRect>("%UnplayableEnergyIcon").Visible = false;
	}
}
