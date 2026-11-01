using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Squ.Cards;
using Squ.Script;

#nullable enable

namespace Squ.Combat;

/// <summary>直接从两种独立剧本记录中选择本次激活的一种。</summary>
public static class BowangParadoxChoice
{
    public static async Task OfferAsync(PlayerChoiceContext choiceContext, Player player, Func<Task>? beforeActivation = null)
    {
        var records = ScriptSystem.ScriptCollectionPile.GetPile(player).Cards;
        CardModel advance = records.OfType<BowangParadoxAdvanceScriptRecord>().Single();
        CardModel arson = records.OfType<BowangParadoxArsonScriptRecord>().Single();
        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, [advance, arson], player, canSkip: false);
        if (chosen is ScriptRecordCardTemplate record)
        {
            await ScriptSystem.ActivatePlayedScriptAsync(choiceContext, player, record, beforeActivation);
        }
    }

    [HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready))]
    private static class SelectionScreenTitlePatch
    {
        private static void Postfix(NChooseACardSelectionScreen __instance)
        {
            if (AccessTools.Field(typeof(NChooseACardSelectionScreen), "_cards").GetValue(__instance)
                    is not IReadOnlyList<CardModel> cards
                || cards.Count != 2
                || !cards.Any(card => card is BowangParadoxAdvanceScriptRecord)
                || !cards.Any(card => card is BowangParadoxArsonScriptRecord)
                || AccessTools.Field(typeof(NChooseACardSelectionScreen), "_banner").GetValue(__instance)
                    is not NCommonBanner banner)
            {
                return;
            }
            banner.label.SetTextAutoSize(new LocString("cards",
                "SUNQIAN_UNIVERSE_CARD_BOWANG_PARADOX_SCRIPT.selectionScreenPrompt").GetRawText());
        }
    }
}
