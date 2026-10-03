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
/// 「以战养战」：按牌名（<see cref="ModelId"/>，升级与否同一张）记录本战是否打出过，
/// 并保留该牌名是否至少有一张为升级版。同名多张或自动打出多次只奖励一次；
/// 精英/Boss 胜利结束时掉落一张对应的牌，且只要打出过升级版，奖励也为升级版。
/// </summary>
public static class WarFeedsWarResolutionTracker
{
	private static readonly Dictionary<ulong, Dictionary<ModelId, bool>> PlayedCardsByPlayer = [];

	public static void RecordPlayed(CardModel card)
	{
		if (card.Owner is not Player owner || !card.Keywords.Contains(SquKeywords.WarFeedsWar))
		{
			return;
		}

		if (!PlayedCardsByPlayer.TryGetValue(
			owner.NetId,
			out Dictionary<ModelId, bool>? playedCards))
		{
			playedCards = [];
			PlayedCardsByPlayer[owner.NetId] = playedCards;
		}

		// ModelId 是牌类型，不是实例：两张同名牌分别打出，只保留一条，并让升级版优先。
		playedCards[card.Id] = card.IsUpgraded
			|| (playedCards.TryGetValue(card.Id, out bool upgraded) && upgraded);
	}

	public static void TryOfferCombatRewards(CombatRoom room)
	{
		if (room.RoomType is not RoomType.Elite and not RoomType.Boss)
		{
			return;
		}

		bool offeredBlitzkriegReward = false;
		bool offeredGreatestPassReward = false;

		foreach (Player player in room.CombatState.Players)
		{
			if (!PlayedCardsByPlayer.TryGetValue(
				player.NetId,
				out Dictionary<ModelId, bool>? playedCards))
			{
				continue;
			}

			if (player.RunState is not RunState runState)
			{
				continue;
			}

			foreach ((ModelId cardId, bool upgraded) in playedCards)
			{
				CardModel canonical = ModelDb.GetById<CardModel>(cardId);
				CardModel reward = runState.CreateCard(canonical, player);
				if (upgraded)
				{
					reward.UpgradeInternal();
					reward.FinalizeUpgradeInternal();
				}

				room.AddExtraReward(player, new SpecialCardReward(reward, player));

				if (cardId == ModelDb.Card<Blitzkrieg>().Id)
				{
					offeredBlitzkriegReward = true;
				}
				else if (cardId == ModelDb.Card<GreatestPassOfCentralPlains>().Id)
				{
					offeredGreatestPassReward = true;
				}
			}
		}

		if (offeredBlitzkriegReward)
		{
			SquSfx.PlayDuringCombatEnd(SquSfx.BlitzkriegThreeHoursBreakJingzhouEvent);
		}

		if (offeredGreatestPassReward)
		{
			SquSfx.PlayDuringCombatEnd(SquSfx.GreatestPassOfCentralPlainsCombatRewardEvent);
		}
	}

	public static void ClearCombat() => PlayedCardsByPlayer.Clear();
}
