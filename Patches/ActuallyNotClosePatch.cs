#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Monsters;
using Squ.Powers;
namespace Squ.Patches;

[HarmonyPatch(typeof(DieForYouPower), nameof(DieForYouPower.ModifyUnblockedDamageTarget))]
internal static class ActuallyNotClosePatch
{
	private static bool Prefix(DieForYouPower __instance, Creature target, ref Creature __result)
	{
		if (__instance.Owner.Monster is not SecondBrother || !__instance.Owner.HasPower<ActuallyNotClosePower>()) return true;
		__result = target;
		return false;
	}
}
