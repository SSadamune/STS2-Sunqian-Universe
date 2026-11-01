using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Godot;
using Squ.Script;

namespace Squ.Patches;

// Wrap uses the native single-click completion, with no confirmation button.
[HarmonyPatch(typeof(NCombatPileCardSelectScreen), nameof(NCombatPileCardSelectScreen._Ready))]
internal static class WrapSelectionScreenPatch
{
    internal static bool HidesConfirmation(CardSelectorPrefs prefs) =>
        prefs.Prompt.LocTable == SquCommonL10n.Table
        && prefs.Prompt.LocEntryKey == ScriptSystem.WrapSelectionPromptKey;

    [HarmonyPostfix]
    private static void Postfix(NCombatPileCardSelectScreen __instance, CardSelectorPrefs ____prefs)
    {
        if (HidesConfirmation(____prefs))
            __instance.GetNode<Control>("%Confirm").Hide();
    }
}
