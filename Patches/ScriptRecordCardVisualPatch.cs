using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
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
		if (__instance.Model is not ScriptRecordCardTemplate)
		{
			return;
		}

		__instance.GetNode<TextureRect>("%EnergyIcon").Visible = false;
		__instance.GetNode<MegaLabel>("%EnergyLabel").Visible = false;
		__instance.GetNode<TextureRect>("%UnplayableEnergyIcon").Visible = false;
	}
}
