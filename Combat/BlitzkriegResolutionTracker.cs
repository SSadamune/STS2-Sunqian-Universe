using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Squ.Audio;
using Squ.Cards;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 追踪本战是否打出过闪电战；精英/Boss 胜利结束时提供可选的额外闪电战奖励。
/// </summary>
public static class BlitzkriegResolutionTracker
{
	private static readonly HashSet<ulong> PlayersWhoPlayed = [];

	public static void RecordPlayed(Player player) => PlayersWhoPlayed.Add(player.NetId);

	public static void TryOfferCombatRewards(CombatRoom room)
	{
		if (room.RoomType is not RoomType.Elite and not RoomType.Boss)
		{
			return;
		}

		bool offeredAny = false;

		foreach (Player player in room.CombatState.Players)
		{
			if (!PlayersWhoPlayed.Contains(player.NetId))
			{
				continue;
			}

			if (player.RunState is not RunState runState)
			{
				continue;
			}

			CardModel blitzkrieg = runState.CreateCard<Blitzkrieg>(player);
			room.AddExtraReward(player, new SpecialCardReward(blitzkrieg, player));
			offeredAny = true;
		}

		if (offeredAny)
		{
			SquSfx.PlayDuringCombatEnd(SquSfx.BlitzkriegThreeHoursBreakJingzhouEvent);
		}
	}

	public static void ClearCombat() => PlayersWhoPlayed.Clear();
}
