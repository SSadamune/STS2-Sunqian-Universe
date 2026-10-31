using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Models.Capabilities;

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

/// <summary>
/// Replaces the basic Strike rules line and Chicken Foot Cheese's appended fragment with the
/// same single sentence used by the script's Strike preview. Other capability fragments remain.
/// </summary>
[HarmonyPatch]
internal static class ScriptChickenFootCheeseDescriptionPatch
{
	private static readonly MethodInfo AddExtraArgsMethod =
		AccessTools.Method(typeof(CardModel), "AddExtraArgsToDescription");
	private const string DescriptionKey =
		"SUNQIAN_UNIVERSE_CHICKEN_FOOT_CHEESE_STRIKE.descriptionFragment";

	private static MethodBase TargetMethod() =>
		AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(method =>
			method.Name == "GetDescriptionForPile"
			&& method.GetParameters().Length == 3);

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(CardModel __instance, PileType pileType, ref string __result)
	{
		if (pileType is not (PileType.Hand or PileType.Play)
			|| !ScriptChickenFootCheesePower.ShouldDisplayRedirectedBasicStrike(__instance)
			|| __instance.Capability<ChickenFootCheeseStrikeCapability>() is not { } capability)
		{
			return;
		}

		LocString originalDescription = __instance.Description;
		__instance.DynamicVars.AddTo(originalDescription);
		// Match the card's own description setup: custom Strike bodies may be
		// nested LocStrings (BodyText), rather than a standalone Damage template.
		AddExtraArgsMethod.Invoke(__instance, [originalDescription]);
		string baseDescription = originalDescription.GetFormattedText() ?? string.Empty;
		LocString redirectedDescription = new("cards", DescriptionKey);
		string replacement = Format(
			redirectedDescription,
			__instance.DynamicVars.Values.Concat(capability.DynamicVars.Values));

		if (baseDescription.Length == 0 || replacement.Length == 0)
		{
			return;
		}

		if (!__result.Contains(replacement))
		{
			return;
		}

		int baseIndex = __result.IndexOf(baseDescription);
		if (baseIndex < 0)
		{
			return;
		}

		int removeLength = baseDescription.Length;
		if (baseIndex + removeLength < __result.Length
			&& __result[baseIndex + removeLength] == '\n')
		{
			removeLength++;
		}

		__result = __result.Remove(baseIndex, removeLength);
	}

	private static string Format(LocString description, IEnumerable<DynamicVar> dynamicVars)
	{
		foreach (DynamicVar dynamicVar in dynamicVars)
		{
			description.Add(dynamicVar);
		}

		return description.GetFormattedText() ?? string.Empty;
	}
}
