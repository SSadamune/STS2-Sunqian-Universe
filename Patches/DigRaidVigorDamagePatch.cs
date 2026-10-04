using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Upgraded Dig Raid still spends Vigor and applies it to Burning, but its damage no longer
/// receives Vigor's additive bonus. Other powered damage modifiers remain intact.
/// </summary>
[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.ModifyDamageAdditive))]
internal static class DigRaidVigorDamagePatch
{
	private static void Postfix(CardModel? cardSource, ref decimal __result)
	{
		if (!DigRaid.DamageReceivesVigor(cardSource))
		{
			__result = 0m;
		}
	}
}
