using System.Collections.Generic;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Cards;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ.Patches;

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

/// <summary>
/// Card grids own highlight visibility and hide it after assigning each card, so
/// model-level ShouldGlowGold is not enough on pile and selection screens.
/// Reapply the script state after the row has finished assigning its cards.
/// </summary>
[HarmonyPatch(typeof(NCardGrid), "AssignCardsToRow")]
internal static class ScriptRecordCardGridHighlightPatch
{
	[HarmonyPostfix]
	private static void Postfix(List<NGridCardHolder> row)
	{
		foreach (NGridCardHolder holder in row)
		{
			if (holder.CardNode is not NCard card
				|| card.Model is not ScriptRecordCardTemplate record)
			{
				continue;
			}

			card.CardHighlight.Modulate = NCardHighlight.gold;
			if (record.ShouldGlowGold)
			{
				card.CardHighlight.AnimShow();
			}
			else
			{
				card.CardHighlight.AnimHide();
			}
		}
	}
}
