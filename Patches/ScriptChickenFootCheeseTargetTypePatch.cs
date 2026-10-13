using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(CardModel), "get_TargetType")]
internal static class ScriptChickenFootCheeseTargetTypePatch
{
	private static void Postfix(CardModel __instance, ref TargetType __result)
	{
		if (ScriptChickenFootCheesePower.ShouldDisplayRedirectedBasicStrike(__instance))
		{
			__result = SquTargetTypes.RandomEnemies;
		}
	}
}
