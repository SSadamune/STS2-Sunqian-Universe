#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Monsters;
using Squ.Powers;

namespace Squ.Patches;

[HarmonyPatch]
internal static class SecondBrotherPowerDescriptionPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.PropertyGetter(typeof(PowerModel), nameof(PowerModel.Description));
		yield return AccessTools.PropertyGetter(typeof(PowerModel), nameof(PowerModel.SmartDescription));
	}
	private static void Postfix(PowerModel __instance, ref LocString __result)
	{
		if (__instance is DieForYouPower { IsMutable: true, Owner.Monster: SecondBrother })
			__result = new LocString("powers", __instance.Owner.HasPower<ActuallyNotClosePower>()
				? "SUNQIAN_UNIVERSE_POWER_SECOND_BROTHER_DIE_FOR_YOU.disabledDescription"
				: "SUNQIAN_UNIVERSE_POWER_SECOND_BROTHER_DIE_FOR_YOU.description");
	}
}
