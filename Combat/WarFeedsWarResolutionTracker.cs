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
/// 「以战养战」：按牌名（<see cref="ModelId"/>，升级与否同一张）记录本战是否打出过；
/// 同名多张或自动打出多次只算一次。精英/Boss 胜利结束时掉落一张对应的牌。
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

		// ModelId 是牌类型，不是实例：两张同名牌分别打出，只会留下一条。
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
