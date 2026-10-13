using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using Squ.RunData;

#nullable enable

namespace Squ.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.ReviveBeforeCombatEnd))]
internal static class SelfDecapitationRevivePatch
{
	private static void Postfix(Player __instance, ref Task __result)
	{
		__result = RestoreRecordedHealth(__result, __instance);
	}

	private static async Task RestoreRecordedHealth(Task vanillaRevive, Player player)
	{
		await vanillaRevive;

		if (!SelfDecapitationReviveData.TryGet(player, out int healthLost))
		{
			return;
		}

		await CreatureCmd.Heal(player.Creature, healthLost);

		SelfDecapitationReviveData.Clear(player);
	}
}
