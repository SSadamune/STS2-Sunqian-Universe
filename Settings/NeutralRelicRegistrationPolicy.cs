#nullable enable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Squ.Character;
using Squ.Relics;
using Squ.RunData;

namespace Squ.Settings;

/// <summary>
/// Controls whether this mod's shared relics enter the neutral relic pool.
/// Models remain registered so saves and direct model lookup stay compatible.
/// </summary>
public static class NeutralRelicRegistrationPolicy
{
	public static bool ShouldInclude()
	{
		NeutralContentPermissionMode mode =
			ContentSettingsRunData.NeutralRelicRegistration;

		return mode switch
		{
			NeutralContentPermissionMode.Allow => true,
			NeutralContentPermissionMode.WhenModCharacterPresent => HasModCharacter(),
			_ => false,
		};
	}

	private static bool HasModCharacter()
	{
		IRunState? runState = RunManager.Instance.DebugOnlyGetState();
		return runState?.Players.Any(player => player.Character is SunqianCharacter) == true;
	}

	private static bool IsAffectedRelic(RelicModel relic) =>
		relic is SevenStarDaggerRelic;

	[HarmonyPatch(typeof(RelicModel), "get_HoverTips")]
	private static class AffectedRelicHoverTipsPatch
	{
		private static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			if (!IsAffectedRelic(__instance) || !ShouldInclude())
			{
				return;
			}

			__result =
			[
				..__result,
				new HoverTip(
					SquCommonL10n.AnnotationTitle(),
					SquCommonL10n.NeutralRelicAddedAnnotation()),
			];
		}
	}

	[HarmonyPatch(
		typeof(SharedRelicPool),
		nameof(SharedRelicPool.GetUnlockedRelics),
		[typeof(UnlockState)])]
	private static class GetUnlockedRelicsPatch
	{
		private static void Postfix(ref IEnumerable<RelicModel> __result)
		{
			if (!ShouldInclude())
			{
				__result = __result.Where(relic => !IsAffectedRelic(relic));
			}
		}
	}
}
