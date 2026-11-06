#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// Hidden per-combat life-loss tracker used by《复仇打击》. It records actual
/// CurrentHp decreases, including temporary health and non-damage HP changes,
/// and snapshots how many player-turn ends had completed at each loss.
/// </summary>
[RegisterPower]
public sealed class RevengeStrikeTrackerPower : ModPowerTemplate
{
	private readonly record struct Loss(int Amount, int CompletedPlayerTurns);

	private sealed class Data
	{
		public List<Loss> Losses { get; } = [];

		public int CompletedPlayerTurns { get; set; }
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.None;

	protected override bool IsVisibleInternal => false;

	public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

	protected override object InitInternalData() => new Data();

	public static void Initialize() =>
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
		{
			if (evt.CombatState == null)
			{
				return;
			}

			foreach (Player player in evt.RunState.Players)
			{
				EnsureTracking(player.Creature);
			}
		});

	internal static void EnsureTracking(Creature owner)
	{
		if (owner.GetPower<RevengeStrikeTrackerPower>() != null)
		{
			return;
		}

		ModelDb.Power<RevengeStrikeTrackerPower>()
			.ToMutable()
			.ApplyInternal(owner, 1, silent: true);
	}

	public static int GetLostHp(Player player, int turns) =>
		GetLostHp(player.Creature, turns);

	internal static int GetLostHp(Creature owner, int turns)
	{
		RevengeStrikeTrackerPower? power = owner.GetPower<RevengeStrikeTrackerPower>();
		if (power == null)
		{
			return 0;
		}

		Data data = power.GetInternalData<Data>();
		int completed = Math.Max(0, turns);
		int threshold = completed <= 1
			? data.CompletedPlayerTurns
			: Math.Max(0, data.CompletedPlayerTurns - (completed - 1));
		return data.Losses
			.Where(loss => loss.CompletedPlayerTurns >= threshold)
			.Sum(loss => loss.Amount);
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature == Owner && delta < 0m)
		{
			Data data = GetInternalData<Data>();
			data.Losses.Add(new Loss((int)(-delta), data.CompletedPlayerTurns));
		}

		return Task.CompletedTask;
	}

	public override Task AfterSideTurnEndLate(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side == Owner.Side && !Owner.IsDead && participants.Contains(Owner))
		{
			GetInternalData<Data>().CompletedPlayerTurns++;
		}

		return Task.CompletedTask;
	}
}
