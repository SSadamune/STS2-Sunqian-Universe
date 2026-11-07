#nullable enable
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace Squ.RunData;

/// <summary>Per-player, signed run resource; powers are only its combat representation.</summary>
public static class RighteousnessRunData
{
	public const int CombatCarryoverLimit = 10;
	private static readonly PlayerRunSavedData<State> Saved =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).RegisterPerPlayer(
			"righteousness", () => new State(),
			new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

	public static void Initialize()
	{
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
		{
			foreach (Player player in evt.RunState.Players)
			{
				RestoreForCombat(player);
			}
		});
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
		{
			foreach (Player player in evt.RunState.Players)
			{
				// Carryover is bookkeeping, not a power expenditure. Do not fire
				// amount-change hooks or add discarded stacks to the spent total.
				if (Get(player) > CombatCarryoverLimit)
				{
					Set(player, CombatCarryoverLimit);
					player.Creature.GetPower<RighteousnessPower>()?.SetAmount(CombatCarryoverLimit);
				}
			}
		});
	}

	public static int Get(Player player) => Saved.Get(player).Amount;
	public static int GetSpentAmount(Player player) => Saved.Get(player).SpentAmount;
	internal static void RecordSpent(Player player, long amount) => Saved.Modify(player, state =>
		state.SpentAmount = (int)System.Math.Min(int.MaxValue, (long)state.SpentAmount + System.Math.Max(0, amount)));

	internal static void Set(Player player, int amount) =>
		Saved.Modify(player, state => state.Amount = amount);

	public static void RestoreForCombat(Player player)
	{
		int amount = Get(player);
		if (amount == 0 || player.Creature.GetPower<RighteousnessPower>() != null)
		{
			return;
		}
		// This restores already-owned stacks, rather than granting them again.
		ModelDb.Power<RighteousnessPower>().ToMutable().ApplyInternal(player.Creature, amount, silent: true);
	}

	private sealed class State
	{
		public int Amount { get; set; }
		public int SpentAmount { get; set; }
	}
}
