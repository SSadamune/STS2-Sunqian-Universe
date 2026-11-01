using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using Squ.Combat;

namespace Squ.Patches;

/// <summary>
/// Retargets only the attacks emitted by a redirected basic Strike, leaving the card's original
/// OnPlay task and all of its non-damage effects intact.
/// </summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
internal static class BasicStrikeRedirectAttackCommandPatch
{
	[HarmonyPriority(Priority.First)]
	private static void Prefix(AttackCommand __instance) =>
		SquBasicStrikeRedirect.TryRedirectAttack(__instance);
}
