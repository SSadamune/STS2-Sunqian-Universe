#nullable enable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;
using STS2RitsuLib;

namespace Squ.Combat;

/// <summary>
/// 从每个玩家回合开始追踪其格挡技能与实际造成的伤害，使中途获得的倾巢而出也能读取完整快照。
/// </summary>
public static class AllInTurnTracker
{
	private sealed class TurnState
	{
		public decimal TotalDamage;
		public bool PlayedBlockSkill;
	}

	private static readonly Dictionary<ulong, TurnState> States = new();
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => States.Clear());
	}

	public static decimal GetTotalDamage(Player? player) =>
		player is not null && States.TryGetValue(player.NetId, out TurnState? state)
			? state.TotalDamage
			: 0m;

	public static bool PlayedBlockSkill(Player? player) =>
		player is not null
		&& States.TryGetValue(player.NetId, out TurnState? state)
		&& state.PlayedBlockSkill;

	public static void Reset(Player player)
	{
		States[player.NetId] = new TurnState();
		SyncPowerDescription(player);
	}

	private static TurnState GetState(Player player)
	{
		if (!States.TryGetValue(player.NetId, out TurnState? state))
		{
			state = new TurnState();
			States[player.NetId] = state;
		}

		return state;
	}

	private static void RecordCardPlayed(CardPlay cardPlay)
	{
		CardModel card = cardPlay.Card;
		if (cardPlay.PlayIndex != 0
			|| card.Type != CardType.Skill
			|| !card.GainsBlock
			|| card.Owner is not { } owner)
		{
			return;
		}

		GetState(owner).PlayedBlockSkill = true;
		SyncPowerDescription(owner);
	}

	private static void RecordDamage(Creature? dealer, DamageResult result, Creature target)
	{
		Player? player = dealer?.Player ?? dealer?.PetOwner;
		if (player is null || !target.IsEnemy || result.TotalDamage <= 0)
		{
			return;
		}

		GetState(player).TotalDamage += result.TotalDamage;
		SyncPowerDescription(player);
	}

	private static void SyncPowerDescription(Player player)
	{
		player.Creature.GetPower<AllInPower>()?.SyncTurnDamageVar();
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
	private static class BeforeCardPlayedPatch
	{
		private static void Prefix(CardPlay cardPlay) => RecordCardPlayed(cardPlay);
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageGiven))]
	private static class AfterDamageGivenPatch
	{
		private static void Prefix(Creature? dealer, DamageResult results, Creature target) =>
			RecordDamage(dealer, results, target);
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
	private static class AfterSideTurnStartPatch
	{
		private static void Prefix(CombatSide side, IReadOnlyList<Creature> participants)
		{
			foreach (Player player in participants
				.Where(creature => creature.Side == side)
				.Select(creature => creature.Player)
				.OfType<Player>())
			{
				Reset(player);
			}
		}
	}
}
