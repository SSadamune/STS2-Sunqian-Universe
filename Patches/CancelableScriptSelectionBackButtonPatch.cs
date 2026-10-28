using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using Squ.Script;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// The combat-pile selector has a confirm button but no back button. The Box Lunch
/// selection is deliberately cancelable, so add the game's ordinary red back button
/// only for that prompt and complete the selection with an empty result when pressed.
/// </summary>
[HarmonyPatch(typeof(NCombatPileCardSelectScreen), nameof(NCombatPileCardSelectScreen._Ready))]
internal static class CancelableScriptSelectionBackButtonPatch
{
	private const string BackButtonScenePath = "res://scenes/ui/back_button.tscn";

	private static readonly FieldInfo CompletionSourceField =
		AccessTools.Field(typeof(NCardGridSelectionScreen), "_completionSource");

	private static readonly MethodInfo UnsubscribeFromPileMethod =
		AccessTools.Method(typeof(NCombatPileCardSelectScreen), "UnsubscribeFromPile");

	[HarmonyPostfix]
	private static void Postfix(
		NCombatPileCardSelectScreen __instance,
		CardSelectorPrefs ____prefs)
	{
		if (!____prefs.Cancelable
			|| ____prefs.Prompt.LocTable != SquCommonL10n.Table
			|| (____prefs.Prompt.LocEntryKey != ScriptSystem.SelectionPromptKey
				&& ____prefs.Prompt.LocEntryKey != ScriptSystem.ReselectionPromptKey))
		{
			return;
		}

		NBackButton backButton = PreloadManager.Cache
			.GetScene(BackButtonScenePath)
			.Instantiate<NBackButton>();
		backButton.Name = "ScriptSelectionBackButton";
		backButton.Connect(
			NClickableControl.SignalName.Released,
			Callable.From<NButton>(_ => CancelSelection(__instance)));
		__instance.AddChildSafely(backButton);
		Callable.From(backButton.Enable).CallDeferred();
	}

	private static void CancelSelection(NCombatPileCardSelectScreen screen)
	{
		UnsubscribeFromPileMethod.Invoke(screen, null);
		var completion = (TaskCompletionSource<IEnumerable<CardModel>>)
			CompletionSourceField.GetValue(screen)!;
		if (!completion.TrySetResult(Array.Empty<CardModel>()))
		{
			return;
		}

		NOverlayStack.Instance?.Remove(screen);
	}
}
