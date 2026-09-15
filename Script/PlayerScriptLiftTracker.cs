using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

#nullable enable

namespace Squ.Script;

/// <summary>
/// 按玩家记录当前回合内剧本失效次数；回合以 <see cref="PlayerCombatState.TurnNumber" /> 区分。
/// </summary>
internal static class PlayerScriptLiftTracker
{
	private sealed class TurnLiftState
	{
		public int TurnNumber;

		public int LiftCount;
	}

	private static readonly Dictionary<Player, TurnLiftState> States = new();

	/// <summary>
	/// 清除上一场战斗留下的回合计数。
	/// 不同战斗可能复用同一个玩家对象和相同的回合数，不能仅靠 <see cref="PlayerCombatState.TurnNumber"/> 区分。
	/// </summary>
	public static void ClearCombat()
	{
		States.Clear();
	}

	/// <summary>
	/// 记录一次剧本失效，返回本回合累计失效次数。
	/// </summary>
	public static int RecordScriptLift(Player player)
	{
		int turnNumber = player.PlayerCombatState?.TurnNumber ?? -1;

		if (!States.TryGetValue(player, out TurnLiftState? state) || state.TurnNumber != turnNumber)
		{
			States[player] = new TurnLiftState { TurnNumber = turnNumber, LiftCount = 1 };
			return 1;
		}

		state.LiftCount++;
		return state.LiftCount;
	}

	/// <summary>
	/// 本回合已记录的剧本失效次数；若尚未记录或回合已变，则为 0。
	/// </summary>
	public static int GetLiftsThisTurn(Player player)
	{
		int turnNumber = player.PlayerCombatState?.TurnNumber ?? -1;

		if (!States.TryGetValue(player, out TurnLiftState? state) || state.TurnNumber != turnNumber)
		{
			return 0;
		}

		return state.LiftCount;
	}
}
