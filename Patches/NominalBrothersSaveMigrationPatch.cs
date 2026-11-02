#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Squ.Patches;

// Keep runs and history created before the public model IDs were renamed readable.
[HarmonyPatch(typeof(ModelId), nameof(ModelId.Deserialize))]
internal static class NominalBrothersSaveMigrationPatch
{
	private static void Prefix(ref string json)
	{
		json = json switch
		{
			"CARD.SUNQIAN_UNIVERSE_CARD_FAIR_WEATHER_BROTHERS" => "CARD.SUNQIAN_UNIVERSE_CARD_NOMINAL_BROTHERS",
			"POWER.SUNQIAN_UNIVERSE_POWER_BROTHER_ESTRANGEMENT_POWER" => "POWER.SUNQIAN_UNIVERSE_POWER_ACTUALLY_NOT_CLOSE_POWER",
			_ => json
		};
	}
}
