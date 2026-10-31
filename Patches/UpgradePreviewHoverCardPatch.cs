#nullable enable
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Squ.Patches;

/// <summary>
/// The base-game hover-card container always renders cards in Normal mode. Re-render only
/// explicitly linked upgrade-preview hover cards in Upgrade mode so both DynamicVar changes
/// and IfUpgraded text use the game's standard green upgrade highlighting.
/// </summary>
[HarmonyPatch(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add), [typeof(CardHoverTip)])]
internal static class UpgradePreviewHoverCardPatch
{
	[HarmonyPostfix]
	private static void Postfix(NHoverTipCardContainer __instance, CardHoverTip cardTip)
	{
		if (cardTip.Card.UpgradePreviewType == CardUpgradePreviewType.None
			|| __instance.GetChildCount() == 0)
		{
			return;
		}

		Node newestTip = __instance.GetChild(__instance.GetChildCount() - 1);
		NCard cardNode = newestTip.GetNode<NCard>("%Card");
		cardNode.UpdateVisuals(PileType.Deck, CardPreviewMode.Upgrade);
	}
}
