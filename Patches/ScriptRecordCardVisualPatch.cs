using System;
using System.Collections.Generic;
using System.Reflection;
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
	/// Active white is the same <see cref="NCardHighlight"/> node as the selection glow.
	/// Hiding that node to clear blue also clears white, so an active record that is no
	/// longer selected must stay visible.
	/// </summary>
	internal static bool KeepVisibleOnHide(bool isActive, bool isSelected) =>
		isActive && !isSelected;

	private static readonly FieldInfo? HighlightedCardsField =
		AccessTools.Field(typeof(NCardGrid), "_highlightedCards");

	internal static bool IsSelectionHighlighted(NCard card)
	{
		CardModel? shown = card.Model;
		NGridCardHolder? holder = null;
		for (Node? current = card.GetParent(); current != null; current = current.GetParent())
		{
			holder ??= current as NGridCardHolder;
			if (current is not NCardGrid grid)
			{
				continue;
			}

			if (HighlightedCardsField?.GetValue(grid) is not List<CardModel> selected)
			{
				return false;
			}

			if (shown is not null && selected.Contains(shown))
			{
				return true;
			}

			return holder?.CardModel is CardModel source && selected.Contains(source);
		}

		return false;
	}

	internal static NCard? FindCard(Node node)
	{
		for (Node? current = node; current != null; current = current.GetParent())
		{
			if (current is NCard card)
			{
				return card;
			}
		}

		return null;
	}

	internal static NCardGrid? FindGrid(Node node)
	{
		for (Node? current = node; current != null; current = current.GetParent())
		{
			if (current is NCardGrid grid)
			{
				return grid;
			}
		}

		return null;
	}

	/// <summary>
	/// The grid has only one highlight node. Selection uses its default blue, so an
	/// active record switches to that color while it is in <c>_highlightedCards</c>.
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

		bool isActive = ShouldHighlight(record);
		bool isSelected = IsSelectionHighlighted(card);
		card.CardHighlight.Modulate = ResolveColor(isActive, isSelected);
		return KeepVisibleOnHide(isActive, isSelected);
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

/// <summary>
/// <see cref="NCardHighlight.AnimShow"/> only changes the shader width, so every show
/// has to reapply white or the default selection blue.
/// </summary>
[HarmonyPatch(typeof(NCardHighlight), nameof(NCardHighlight.AnimShow))]
internal static class ScriptRecordCardHighlightShowPatch
{
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(NCardHighlight __instance)
	{
		NCard? card = ScriptRecordCardHighlight.FindCard(__instance);
		if (card?.Model is ScriptRecordCardTemplate)
		{
			ScriptRecordCardHighlight.ApplyColor(card);
		}
	}
}

/// <summary>
/// UnhighlightCard and AssignCardsToRow hide the shared highlight node. Skipping that
/// hide keeps an active script's white glow when the player clears the blue selection.
/// </summary>
[HarmonyPatch(typeof(NCardHighlight), nameof(NCardHighlight.AnimHide))]
internal static class ScriptRecordCardHighlightHidePatch
{
	private static bool _restoringActiveHighlight;

	[HarmonyPrefix]
	private static bool Prefix(NCardHighlight __instance)
	{
		if (_restoringActiveHighlight)
		{
			return true;
		}

		NCard? card = ScriptRecordCardHighlight.FindCard(__instance);
		if (card?.Model is not ScriptRecordCardTemplate
			|| ScriptRecordCardHighlight.FindGrid(__instance) is null
			|| !ScriptRecordCardHighlight.KeepVisibleOnHide(
				ScriptRecordCardHighlight.ShouldHighlight(card.Model),
				ScriptRecordCardHighlight.IsSelectionHighlighted(card)))
		{
			return true;
		}

		_restoringActiveHighlight = true;
		try
		{
			ScriptRecordCardHighlight.ApplyColor(card);
			__instance.AnimShow();
		}
		finally
		{
			_restoringActiveHighlight = false;
		}

		return false;
	}
}

/// <summary>履历牌在规则层仍按技能处理，但在卡面上显示自有类别“剧本”。</summary>
[HarmonyPatch(typeof(NCard), "UpdateTypePlaque")]
internal static class ScriptRecordCardVisualPatch
{
	internal static bool UsesScriptTypePlaque(CardModel? model) =>
		model is ScriptRecordCardTemplate;

	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		if (!UsesScriptTypePlaque(__instance.Model))
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
