using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Cards;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Keeps adjacent automatic keyword declarations on opted-in cards on the same
/// visual row. Native and RitsuLib keywords are grouped according to their
/// normal placement without moving them across the card's rules text.
/// </summary>
[HarmonyPatch]
internal static class RowKeywordsCardPatch
{
	private static MethodBase TargetMethod() =>
		AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(method =>
			method.Name == "GetDescriptionForPile"
			&& method.GetParameters().Length == 3);

	[HarmonyFinalizer]
	private static Exception? Finalizer(
		CardModel __instance,
		ref string __result,
		Exception? __exception)
	{
		if (__exception is not null
			|| __instance is not IRowKeywordsCard { MergeKeywordRows: true }
			|| string.IsNullOrEmpty(__result))
		{
			return __exception;
		}

		List<string> lines = __result.Split('\n').ToList();
		MoveKeywordsBelowRules(__instance, lines);
		MergeLeadingKeywords(__instance, lines);
		MergeTrailingKeywords(__instance, lines);
		__result = string.Join("\n", lines);
		return null;
	}

	private static void MoveKeywordsBelowRules(CardModel card, List<string> lines)
	{
		if (card is not IRowKeywordsCard rowCard)
		{
			return;
		}

		HashSet<string> markers = GetKeywordMarkers(card, rowCard.KeywordsBelowRules);
		if (markers.Count == 0)
		{
			return;
		}

		var moved = new List<string>();
		for (int i = 0; i < lines.Count;)
		{
			if (!StartsWithKeyword(lines[i], markers))
			{
				i++;
				continue;
			}

			moved.Add(lines[i]);
			lines.RemoveAt(i);
		}

		if (moved.Count == 0)
		{
			return;
		}

		// 蓄能整句写在正文里，但仍是后缀块。移下来的关键词接在这一行前面，
		// 这样「保留」位于正文之后、蓄能之前，并与「蓄能」二字同一行。
		int chargeLine = IndexOfChargeLine(card, lines);
		int insertAt = chargeLine >= 0
			? chargeLine
			: lines.Count - CountTrailingKeywordLines(card, lines);
		lines.InsertRange(insertAt, moved);
		MergeRange(lines, insertAt, moved.Count + (chargeLine >= 0 ? 1 : 0));
	}

	private static int IndexOfChargeLine(CardModel card, List<string> lines)
	{
		if (!card.Keywords.Contains(SquKeywords.Charge))
		{
			return -1;
		}

		string marker = GetKeywordMarker(SquKeywords.Charge);
		return marker.Length == 0
			? -1
			: lines.FindIndex(line => line.StartsWith(marker, StringComparison.Ordinal));
	}

	private static void MergeLeadingKeywords(CardModel card, List<string> lines)
	{
		HashSet<string> keywordMarkers = GetModKeywordMarkers(
			card,
			ModKeywordCardDescriptionPlacement.BeforeCardDescription);

		foreach (CardKeyword keyword in CardKeywordOrder.beforeDescription)
		{
			if (ShouldRenderBeforeDescription(card, keyword))
			{
				keywordMarkers.Add(GetKeywordMarker(keyword));
			}
		}

		if (card.DynamicVars.ContainsKey(ForgeVar.defaultName)
			&& HoverTipFactory.Static(StaticHoverTip.Forge) is HoverTip forgeTip
			&& !string.IsNullOrEmpty(forgeTip.Title))
		{
			keywordMarkers.Add($"[gold]{forgeTip.Title}[/gold]");
		}

		int count = 0;
		while (count < lines.Count && StartsWithKeyword(lines[count], keywordMarkers))
		{
			count++;
		}

		MergeRange(lines, 0, count);
	}

	private static void MergeTrailingKeywords(CardModel card, List<string> lines)
	{
		HashSet<string> keywordMarkers = GetModKeywordMarkers(
			card,
			ModKeywordCardDescriptionPlacement.AfterCardDescription);

		foreach (CardKeyword keyword in CardKeywordOrder.afterDescription.Where(card.Keywords.Contains))
		{
			keywordMarkers.Add(GetKeywordMarker(keyword));
		}

		int count = CountTrailingKeywordLines(lines, keywordMarkers);
		if (count > 0 && count < lines.Count
			&& card is IRowKeywordsCard { MergeTrailingKeywordsWithLastRule: true }
			&& !StartsWithKeyword(lines[^(count + 1)], GetKeywordsBelowRulesMarkers(card)))
		{
			count++;
		}
		MergeRange(lines, lines.Count - count, count);
	}

	private static int CountTrailingKeywordLines(CardModel card, List<string> lines) =>
		CountTrailingKeywordLines(lines, GetTrailingKeywordMarkers(card));

	private static int CountTrailingKeywordLines(
		List<string> lines,
		HashSet<string> keywordMarkers)
	{
		int count = 0;
		while (count < lines.Count && StartsWithKeyword(lines[^(count + 1)], keywordMarkers))
		{
			count++;
		}

		return count;
	}

	private static HashSet<string> GetTrailingKeywordMarkers(CardModel card)
	{
		HashSet<string> keywordMarkers = GetModKeywordMarkers(
			card,
			ModKeywordCardDescriptionPlacement.AfterCardDescription);
		foreach (CardKeyword keyword in CardKeywordOrder.afterDescription.Where(card.Keywords.Contains))
		{
			keywordMarkers.Add(GetKeywordMarker(keyword));
		}

		return keywordMarkers;
	}

	private static HashSet<string> GetKeywordsBelowRulesMarkers(CardModel card)
	{
		if (card is not IRowKeywordsCard rowCard)
		{
			return [];
		}

		return GetKeywordMarkers(card, rowCard.KeywordsBelowRules);
	}

	private static HashSet<string> GetKeywordMarkers(CardModel card, IEnumerable<CardKeyword> keywords)
	{
		var markers = new HashSet<string>(StringComparer.Ordinal);
		foreach (CardKeyword keyword in keywords)
		{
			if (!CardShowsKeyword(card, keyword))
			{
				continue;
			}

			string marker = GetKeywordMarker(keyword);
			if (marker.Length > 0)
			{
				markers.Add(marker);
			}
		}

		return markers;
	}

	private static bool CardShowsKeyword(CardModel card, CardKeyword keyword) =>
		card.Keywords.Contains(keyword) || keyword switch
		{
			CardKeyword.Sly => card.IsSlyThisTurn,
			CardKeyword.Retain => card.ShouldRetainThisTurn,
			_ => false,
		};

	private static HashSet<string> GetModKeywordMarkers(
		CardModel card,
		ModKeywordCardDescriptionPlacement placement)
	{
		var markers = new HashSet<string>(StringComparer.Ordinal);
		foreach (CardKeyword keyword in card.Keywords)
		{
			if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out ModKeywordDefinition definition)
				&& definition.CardDescriptionPlacement == placement)
			{
				markers.Add(GetKeywordMarker(keyword));
			}
		}

		return markers;
	}

	private static bool ShouldRenderBeforeDescription(CardModel card, CardKeyword keyword) =>
		keyword switch
		{
			CardKeyword.Sly => card.IsSlyThisTurn,
			CardKeyword.Retain => card.ShouldRetainThisTurn,
			_ => card.Keywords.Contains(keyword),
		};

	private static string GetKeywordMarker(CardKeyword keyword)
	{
		if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out _))
		{
			return $"[gold]{ModKeywordRegistry.GetTitle(keyword).GetFormattedText()}[/gold]";
		}

		IHoverTip hoverTip = HoverTipFactory.FromKeyword(keyword);
		return hoverTip is HoverTip keywordTip
			? $"[gold]{keywordTip.Title}[/gold]"
			: string.Empty;
	}

	private static bool StartsWithKeyword(string line, HashSet<string> keywordMarkers) =>
		keywordMarkers.Any(marker =>
			marker.Length > 0 && line.StartsWith(marker, StringComparison.Ordinal));

	private static void MergeRange(List<string> lines, int start, int count)
	{
		if (count <= 1)
		{
			return;
		}

		string merged = string.Join("", lines.Skip(start).Take(count));
		lines.RemoveRange(start, count);
		lines.Insert(start, merged);
	}
}
