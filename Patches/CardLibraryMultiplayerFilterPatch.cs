using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

#nullable enable

namespace Squ.Patches;

internal static class CardLibraryMultiplayerPreviewState
{
	// The card library resets this tickbox to checked whenever it opens.
	public static bool ShowMultiplayerCards { get; set; } = true;
}

/// <summary>Keeps canonical card descriptions in sync with the card-library multiplayer tickbox.</summary>
[HarmonyPatch(typeof(NCardLibrary), "ToggleFilterMultiplayerCards")]
internal static class CardLibraryMultiplayerDescriptionTogglePatch
{
	[HarmonyPostfix]
	private static void Postfix(NTickbox __0)
	{
		CardLibraryMultiplayerPreviewState.ShowMultiplayerCards = __0.IsTicked;
	}
}

/// <summary>Mirrors the card library's default checked state each time the submenu opens.</summary>
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
internal static class CardLibraryMultiplayerDescriptionOpenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		CardLibraryMultiplayerPreviewState.ShowMultiplayerCards = true;
	}
}

/// <summary>
/// 勾选「多人模式卡牌」时，原版图鉴只会显示多人专属卡，不会隐藏单人专属卡。
/// 这里在勾选时额外过滤掉 <see cref="CardMultiplayerConstraint.SingleplayerOnly"/>。
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), "UpdateFilter")]
internal static class CardLibraryMultiplayerFilterPatch
{
	private static void Postfix(NCardLibrary __instance)
	{
		NLibraryStatTickbox viewMultiplayerCards = AccessTools.Field(
				typeof(NCardLibrary),
				"_viewMultiplayerCards")
			.GetValue(__instance) as NLibraryStatTickbox
			?? throw new InvalidOperationException("NCardLibrary._viewMultiplayerCards is missing.");

		if (!viewMultiplayerCards.IsTicked)
		{
			return;
		}

		var filterField = AccessTools.Field(typeof(NCardLibrary), "_filter");
		var oldFilter = (Func<CardModel, bool>)filterField.GetValue(__instance)!;
		filterField.SetValue(
			__instance,
			(Func<CardModel, bool>)(card =>
				oldFilter(card)
				&& card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly));
	}
}
