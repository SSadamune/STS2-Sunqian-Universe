using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

#nullable enable

namespace Squ.RunData;

/// <summary>
/// Records the additional healing a player killed by Self Decapitation Ascension should receive
/// after the vanilla pre-combat-end revival pass restores 1 HP.
/// </summary>
public static class SelfDecapitationReviveData
{
	private const string SaveKey = "self_decapitation_revive";

	private static readonly PlayerRunSavedData<State> Saved =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).RegisterPerPlayer(
			SaveKey,
			() => new State(),
			new RunSavedDataOptions
			{
				WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
			});

	public static void Initialize()
	{
		// Forces registration before a run is loaded.
	}

	public static void Schedule(Player player, int healthLost)
	{
		RunState runState = GetRunState(player);
		Saved.Set(runState, player.NetId, new State
		{
			HealthLost = healthLost,
		});
	}

	public static bool TryGet(Player player, out int healthLost)
	{
		RunState runState = GetRunState(player);
		if (Saved.TryGet(runState, player.NetId, out State state)
			&& state.HealthLost > 0)
		{
			healthLost = state.HealthLost;
			return true;
		}

		healthLost = 0;
		return false;
	}

	public static void Clear(Player player)
	{
		RunState runState = GetRunState(player);
		Saved.Remove(runState, player.NetId);
	}

	private static RunState GetRunState(Player player) =>
		player.RunState as RunState
		?? throw new System.InvalidOperationException(
			"Self Decapitation Ascension requires a concrete RunState.");

	public sealed class State
	{
		public int HealthLost { get; set; }
	}
}
