using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Squ.Relics;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Makes a strong Monster encounter use the same base reward recipe as an Elite encounter
/// for each player who owns 配角工牌. The room itself remains a Monster room, so other
/// players in multiplayer keep their normal rewards.
/// </summary>
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
internal static class SupportingActorBadgeGenerateRewardsPatch
{
	private static bool Prefix(Player __0, AbstractRoom __1, ref List<Reward> __result)
	{
		Player player = __0;
		if (__1 is not CombatRoom combatRoom
			|| !SupportingActorBadgeRelic.IsStrongMonsterEncounter(combatRoom)
			|| player.GetRelic<SupportingActorBadgeRelic>() is not SupportingActorBadgeRelic badge)
		{
			return true;
		}

		List<Reward> rewards =
		[
			new GoldReward(combatRoom.Encounter.MinGoldReward, combatRoom.Encounter.MaxGoldReward, player),
		];

		if (player.PlayerOdds.PotionReward.Roll(player, RoomType.Elite))
		{
			rewards.Add(new PotionReward(player));
		}

		rewards.Add(new CardReward(
			CardCreationOptions.ForRoom(player, RoomType.Elite)
				.WithFlags(CardCreationFlags.IsFromCombat),
			3,
			player));
		rewards.Add(new RelicReward(player));

		badge.Flash();
		__result = rewards;
		return false;
	}
}

/// <summary>
/// Tutorial rewards bypass the normal reward generator. Strong encounters affected by
/// 配角工牌 must bypass that exception so the elite-equivalent recipe above is used.
/// </summary>
[HarmonyPatch(typeof(RewardsSet), "TryGenerateTutorialRewards")]
internal static class SupportingActorBadgeTutorialRewardsPatch
{
	private static bool Prefix(Player __0, AbstractRoom __1, ref bool __result)
	{
		if (__1 is CombatRoom combatRoom
			&& SupportingActorBadgeRelic.IsStrongMonsterEncounter(combatRoom)
			&& __0.GetRelic<SupportingActorBadgeRelic>() is not null)
		{
			__result = false;
			return false;
		}

		return true;
	}
}
