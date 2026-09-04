using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Squ.Audio;
using Squ.Cards;
using Squ;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 「以战养战」：追踪本战打出过的带该词条的牌；精英/Boss 胜利结束时提供可选的额外同名牌奖励。
/// </summary>
public static class WarFeedsWarResolutionTracker
{
	private static readonly Dictionary<ulong, HashSet<ModelId>> PlayedCardIdsByPlayer = [];

	public static void RecordPlayed(CardModel card)
	{
		if (card.Owner is not Player owner || !card.Keywords.Contains(SquKeywords.WarFeedsWar))
		{
			return;
		}

		if (!PlayedCardIdsByPlayer.TryGetValue(owner.NetId, out HashSet<ModelId>? playedIds))
		{
			playedIds = [];
			PlayedCardIdsByPlayer[owner.NetId] = playedIds;
		}

		playedIds.Add(card.Id);
	}

	public static void TryOfferCombatRewards(CombatRoom room)
	{
		if (room.RoomType is not RoomType.Elite and not RoomType.Boss)
		{
			return;
		}

		bool offeredBlitzkriegReward = false;

		foreach (Player player in room.CombatState.Players)
		{
			if (!PlayedCardIdsByPlayer.TryGetValue(player.NetId, out HashSet<ModelId>? playedIds))
			{
				continue;
			}

			if (player.RunState is not RunState runState)
			{
				continue;
			}

			foreach (ModelId cardId in playedIds)
			{
				CardModel canonical = ModelDb.GetById<CardModel>(cardId);
				CardModel reward = runState.CreateCard(canonical, player);
				room.AddExtraReward(player, new SpecialCardReward(reward, player));

				if (cardId == ModelDb.Card<Blitzkrieg>().Id)
				{
					offeredBlitzkriegReward = true;
				}
			}
		}

		if (offeredBlitzkriegReward)
		{
			SquSfx.PlayDuringCombatEnd(SquSfx.BlitzkriegThreeHoursBreakJingzhouEvent);
		}
	}

	public static void ClearCombat() => PlayedCardIdsByPlayer.Clear();
}
