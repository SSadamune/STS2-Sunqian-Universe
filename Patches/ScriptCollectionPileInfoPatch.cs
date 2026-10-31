using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Script;

namespace Squ.Patches;

[HarmonyPatch(typeof(NCardPileScreen), "_Ready")]
internal static class ScriptCollectionPileInfoPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCardPileScreen __instance)
	{
		if (__instance.Pile.Type != ScriptSystem.ScriptCollectionPile)
		{
			return;
		}

		var label = __instance.GetNode<MegaRichTextLabel>("%BottomLabel");
		label.Text = "[center]" + new LocString(
			SquCommonL10n.Table, ScriptSystem.ViewPromptKey).GetFormattedText();
		label.Visible = true;
	}
}
