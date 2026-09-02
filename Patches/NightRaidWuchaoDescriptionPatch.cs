using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 夜袭乌巢为受影响的打击攻击牌按需追加灼烧描述。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), [typeof(PileType), typeof(Creature)])]
internal static class NightRaidWuchaoDescriptionPatch
{
	private const string DescriptionKey =
		"SUNQIAN_UNIVERSE_POWER_SCRIPT_NIGHT_RAID_WUCHAO_POWER.cardDescription";

	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (!SquCardTags.AppliesBurning(__instance)
			|| __instance.Owner?.Creature?.GetPower<Powers.ScriptNightRaidWuchaoPower>() is not { Amount: > 0 } power)
		{
			return;
		}

		var description = new LocString("powers", DescriptionKey);
		description.Add("Amount", power.Amount);
		__result += description.GetFormattedText();
	}
}
