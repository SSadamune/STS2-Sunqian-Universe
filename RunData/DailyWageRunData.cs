using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ.Relics;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

#nullable enable

namespace Squ.RunData;

/// <summary>
/// 《日结工资》本局累计发放的金币。按玩家分开存档。
/// </summary>
public static class DailyWageRunData
{
	public const string GoldEarnedVarName = "GoldEarned";

	private const string SaveKey = "daily_wage_gold";

	private static readonly PlayerRunSavedData<State> Saved =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).RegisterPerPlayer(
			SaveKey,
			() => new State(),
			new RunSavedDataOptions
			{
				WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
			});

	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(SyncAll);
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(SyncAll);
	}

	public static int AddGold(Player player, decimal amount)
	{
		int gained = (int)amount;
		int total = 0;
		Saved.Modify(player, data =>
		{
			data.GoldEarned += gained;
			total = data.GoldEarned;
		});
		return total;
	}

	public static int GetGoldEarned(Player player) => Saved.Get(player).GoldEarned;

	public static void SyncRelic(DailyWageRelic relic)
	{
		if (!relic.IsMutable || relic.Owner is null)
		{
			return;
		}

		relic.DynamicVars[GoldEarnedVarName].BaseValue = GetGoldEarned(relic.Owner);
	}

	private static void SyncAll(RunLoadedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			SyncPlayer(player);
		}
	}

	private static void SyncAll(RunStartedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			SyncPlayer(player);
		}
	}

	private static void SyncPlayer(Player player)
	{
		foreach (RelicModel relic in player.Relics)
		{
			if (relic is DailyWageRelic dailyWage)
			{
				SyncRelic(dailyWage);
			}
		}
	}

	private sealed class State
	{
		public int GoldEarned { get; set; }
	}
}
