using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ.Cards;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 闪电战单次打出结算窗口：同场战斗、同一玩家至多将一张未强化闪电战加入牌组一次。
/// </summary>
public static class BlitzkriegResolutionTracker
{
	private static readonly HashSet<ulong> ActiveMonitoring = [];

	private static readonly HashSet<ulong> GrantedDeckLoot = [];

	public static bool TryBeginMonitoring(Player player)
	{
		if (GrantedDeckLoot.Contains(player.NetId))
		{
			return false;
		}

		return ActiveMonitoring.Add(player.NetId);
	}

	public static void EndMonitoring(Player player)
	{
		ActiveMonitoring.Remove(player.NetId);
	}

	public static bool IsMonitoring(Player player) => ActiveMonitoring.Contains(player.NetId);

	public static async Task TryGrantDeckLootAsync(
		PlayerChoiceContext choiceContext,
		Player player)
	{
		if (!IsMonitoring(player) || !GrantedDeckLoot.Add(player.NetId))
		{
			return;
		}

		ActiveMonitoring.Remove(player.NetId);

		if (player.RunState is not RunState runState)
		{
			return;
		}

		CardModel blitzkrieg = runState.CreateCard<Blitzkrieg>(player);
		await CardPileCmd.Add(blitzkrieg, PileType.Deck);
	}

	public static void ClearCombat()
	{
		ActiveMonitoring.Clear();
		GrantedDeckLoot.Clear();
	}
}
