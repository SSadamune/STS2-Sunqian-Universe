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
	private static readonly PlayerRunSavedData<State> Saved =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).RegisterPerPlayer(
			"righteousness", () => new State(),
			new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

	public static void Initialize() =>
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
		{
			foreach (Player player in evt.RunState.Players)
			{
				RestoreForCombat(player);
			}
		});

	public static int Get(Player player) => Saved.Get(player).Amount;
    public static int GetLostAmount(Player player) => Saved.Get(player).LostAmount;
    internal static void RecordLoss(Player player, long amount) => Saved.Modify(player, state =>
        state.LostAmount = (int)System.Math.Min(int.MaxValue, (long)state.LostAmount + System.Math.Max(0, amount)));

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
        public int LostAmount { get; set; }
	}
}
