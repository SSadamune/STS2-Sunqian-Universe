#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Relics;

namespace Squ.Patches;

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
internal static class DeathDeniedRelicDescriptionPatch
{
	private static void Postfix(RelicModel __instance, ref LocString __result)
	{
		if (__instance is not DeathDeniedRelic { IsMutable: true } relic
			|| !CombatManager.Instance.IsInProgress) return;
		LocString description = new("relics", relic.Id.Entry + ".combatDescription");
		description.Add("BaseDescription", __result);
		description.Add("HpLost", relic.HpLostThisCombat);
		__result = description;
	}
}
