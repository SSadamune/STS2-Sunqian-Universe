using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;

#nullable enable

namespace Squ.Script;

public static class ScriptSystem
{
	internal static bool SuppressLiftNotification { get; set; }

	/// <summary>
	/// 本回合该玩家已记录的剧本失效次数。
	/// </summary>
	public static int GetScriptLiftsThisTurn(Player player) =>
		PlayerScriptLiftTracker.GetLiftsThisTurn(player);

	/// <summary>
	/// 移除角色身上所有剧本能力（例如事件强制结束剧本）。
	/// </summary>
	public static async Task InvalidateScriptsAsync(Creature creature)
	{
		foreach (ScriptPowerTemplate active in creature.Powers.OfType<ScriptPowerTemplate>().ToList())
		{
			await RemoveScriptPowerAsync(active);
		}
	}

	public static async Task RemoveScriptPowerAsync(ScriptPowerTemplate power, bool notifyLift = true)
	{
		SuppressLiftNotification = !notifyLift;
		await PowerCmd.Remove(power);
		SuppressLiftNotification = false;
	}

	internal static async Task NotifyScriptLiftedAsync(Creature creature, PlayerChoiceContext choiceContext)
	{
		Player? player = creature.Player;
		if (player is null)
		{
			return;
		}

		int liftsThisTurn = PlayerScriptLiftTracker.RecordScriptLift(player);
		ScriptLiftContext context = new()
		{
			ChoiceContext = choiceContext,
			Owner = creature,
			LiftsThisTurn = liftsThisTurn,
		};

		foreach (IScriptLiftHandler handler in EnumerateScriptLiftHandlers(player, creature))
		{
			await handler.OnScriptLiftAsync(context);
		}
	}

	private static IEnumerable<IScriptLiftHandler> EnumerateScriptLiftHandlers(Player player, Creature creature)
	{
		foreach (AbstractModel model in player.Relics)
		{
			if (model is IScriptLiftHandler handler)
			{
				yield return handler;
			}
		}

		foreach (PowerModel power in creature.Powers)
		{
			if (power is IScriptLiftHandler handler)
			{
				yield return handler;
			}
		}
	}
}
