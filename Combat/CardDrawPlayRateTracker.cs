using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 按牌组卡牌实例跟踪「抽到次数 / Play 阶段打出次数」，并保留最近若干场已结束战斗的快照。
/// 打出率 = Plays / Draws（仅 Draws &gt; 0 时有定义）；仅 <see cref="PlayerTurnPhase.Play"/> 阶段的打出计入分子。
/// </summary>
public static class CardDrawPlayRateTracker
{
	public const int DefaultWindowSize = 5;

	public const int MaxStoredCombats = 10;

	private const string SaveKey = "card_draw_play_rate";

	private static readonly PlayerRunSavedData<PlayerSaveState> SavedData =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).RegisterPerPlayer(
			SaveKey,
			() => new PlayerSaveState(),
			new RunSavedDataOptions
			{
				WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
			});

	private static readonly Dictionary<ulong, PlayerRuntime> Runtimes = new();

	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CardDrawnEvent>(OnCardDrawn);
		RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(OnCardPlayed);
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded);
		RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded);
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
		RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => Runtimes.Clear());
	}

	/// <summary>
	/// 从抽牌堆中选出过去 <paramref name="windowSize"/> 场战斗打出率最高的至多 <paramref name="count"/> 张牌。
	/// 无抽牌记录或 draws=0 的牌（含本场临时牌）视为打出率 0、打出次数 0，与有记录的 0% 牌同等参与。
	/// 打出率相同则优先打出次数更多者；仍相同则优先更早获得的牌（入组楼层，再实例 ID / 抽牌堆次序）。
	/// </summary>
	public static List<CardModel> SelectHighestPlayRateFromDrawPile(
		Player player,
		int count,
		int windowSize = DefaultWindowSize,
		bool includeCurrentCombat = false)
	{
		if (count <= 0)
		{
			return [];
		}

		EnsureDeckInstanceIds(player);

		List<(CardModel Card, float Rate, int Plays, int Floor, int InstanceId, int DrawIndex)> ranked = [];
		int drawIndex = 0;
		foreach (CardModel card in PileType.Draw.GetPile(player).Cards)
		{
			float rate = 0f;
			int plays = 0;
			if (TryGetStats(player, card, windowSize, includeCurrentCombat, out int draws, out int trackedPlays)
				&& draws > 0)
			{
				rate = (float)trackedPlays / draws;
				plays = trackedPlays;
			}

			CardModel identity = ResolveIdentityCard(card) ?? card;
			int floor = identity.FloorAddedToDeck ?? int.MaxValue;
			int instanceId = TryResolveInstanceId(player, card, assignIfMissing: false, out int resolvedId)
				? resolvedId
				: int.MaxValue;

			ranked.Add((card, rate, plays, floor, instanceId, drawIndex));
			drawIndex++;
		}

		return ranked
			.OrderByDescending(entry => entry.Rate)
			.ThenByDescending(entry => entry.Plays)
			.ThenBy(entry => entry.Floor)
			.ThenBy(entry => entry.InstanceId)
			.ThenBy(entry => entry.DrawIndex)
			.Take(count)
			.Select(entry => entry.Card)
			.ToList();
	}

	public static bool TryGetStats(
		Player player,
		CardModel card,
		int windowSize,
		bool includeCurrentCombat,
		out int draws,
		out int plays)
	{
		draws = 0;
		plays = 0;
		if (!TryResolveInstanceId(player, card, assignIfMissing: false, out int instanceId))
		{
			return false;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		AggregateWindow(runtime, instanceId, windowSize, includeCurrentCombat, out draws, out plays);
		return true;
	}

	/// <summary>
	/// 将当前追踪数据写入 RitsuLib / 游戏 logger（可在 Debug Log Viewer 中查看）。
	/// </summary>
	public static void LogCurrentState(
		Player player,
		int windowSize = DefaultWindowSize,
		bool includeCurrentCombat = false,
		IReadOnlyList<CardModel>? selectedCards = null,
		string? reason = null)
	{
		EnsureDeckInstanceIds(player);
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		var builder = new StringBuilder();
		builder.AppendLine("[CardDrawPlayRateTracker] snapshot");
		if (!string.IsNullOrWhiteSpace(reason))
		{
			builder.Append("  reason: ").AppendLine(reason);
		}

		builder.Append("  playerNetId: ").AppendLine(player.NetId.ToString());
		builder.Append("  windowSize: ").AppendLine(windowSize.ToString());
		builder.Append("  includeCurrentCombat: ").AppendLine(includeCurrentCombat.ToString());
		builder.Append("  storedCombats: ").Append(runtime.Recent.Count)
			.Append('/').AppendLine(MaxStoredCombats.ToString());
		builder.Append("  nextInstanceId: ").AppendLine(runtime.NextInstanceId.ToString());
		builder.Append("  deckMappingsRestored: ").AppendLine(runtime.DeckMappingsRestored.ToString());

		AppendSnapshotSection(builder, "deckInstanceMap", runtime.CardToInstanceId
			.OrderBy(pair => pair.Value)
			.Select(pair => $"    #{pair.Value}: {FormatCardLabel(pair.Key)} [{GetMatchKey(pair.Key)}]"));

		int finishedInWindow = Math.Min(windowSize, runtime.Recent.Count);
		int firstFinishedIndex = runtime.Recent.Count - finishedInWindow;
		for (int i = 0; i < runtime.Recent.Count; i++)
		{
			bool inWindow = i >= firstFinishedIndex;
			builder.Append("  finishedCombat[").Append(i).Append(inWindow ? ", inWindow" : ", outOfWindow")
				.AppendLine("]:");
			AppendCombatSnapshot(builder, runtime.Recent[i], "    ");
		}

		builder.AppendLine("  currentCombat:");
		AppendCombatSnapshot(builder, runtime.Current, "    ");

		builder.AppendLine("  drawPileRates:");
		foreach (CardModel card in PileType.Draw.GetPile(player).Cards)
		{
			if (!TryGetStats(player, card, windowSize, includeCurrentCombat, out int draws, out int plays))
			{
				builder.Append("    ").Append(FormatCardLabel(card))
					.AppendLine(" -> no tracked stats");
				continue;
			}

			builder.Append("    ").Append(FormatCardLabel(card))
				.Append(" -> draws=").Append(draws)
				.Append(", plays=").Append(plays)
				.Append(", rate=").Append(FormatRate(draws, plays))
				.AppendLine();
		}

		if (selectedCards is { Count: > 0 })
		{
			builder.AppendLine("  selectedThisPlay:");
			foreach (CardModel card in selectedCards)
			{
				builder.Append("    ").Append(FormatCardLabel(card));
				if (TryGetStats(player, card, windowSize, includeCurrentCombat, out int draws, out int plays))
				{
					builder.Append(" -> draws=").Append(draws)
						.Append(", plays=").Append(plays)
						.Append(", rate=").Append(FormatRate(draws, plays));
				}

				builder.AppendLine();
			}
		}

		SquMod.Logger.Info(builder.ToString());
	}

	private static void OnCardDrawn(CardDrawnEvent evt)
	{
		CardModel card = evt.Card;
		Player? player = card.Owner;
		if (player is null || !TryResolveInstanceId(player, card, assignIfMissing: true, out int instanceId))
		{
			return;
		}

		GetOrCreateRuntime(player).Current.AddDraw(instanceId);
	}

	private static void OnCardPlayed(CardPlayedEvent evt)
	{
		CardPlay cardPlay = evt.CardPlay;
		if (cardPlay.PlayIndex != 0)
		{
			return;
		}

		CardModel card = cardPlay.Card;
		Player? player = card.Owner;
		if (player is null || player.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
		{
			return;
		}

		if (!TryResolveInstanceId(player, card, assignIfMissing: true, out int instanceId))
		{
			return;
		}

		GetOrCreateRuntime(player).Current.AddPlay(instanceId);
	}

	private static void OnCombatStarting(CombatStartingEvent evt)
	{
		if (evt.RunState is not RunState runState)
		{
			return;
		}

		foreach (Player player in runState.Players)
		{
			LoadSavedState(player);
			EnsureDeckInstanceIds(player);
			GetOrCreateRuntime(player).Current = new CombatSnapshot();
			Persist(player);
		}
	}

	private static void OnCombatEnded(CombatEndedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			PlayerRuntime runtime = GetOrCreateRuntime(player);
			if (runtime.Current.Cards.Count > 0)
			{
				runtime.Recent.Add(runtime.Current);
				while (runtime.Recent.Count > MaxStoredCombats)
				{
					runtime.Recent.RemoveAt(0);
				}
			}

			runtime.Current = new CombatSnapshot();
			EnsureDeckInstanceIds(player);
			Persist(player);
		}
	}

	private static void OnRunLoaded(RunLoadedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			LoadSavedState(player, force: true);
			EnsureDeckInstanceIds(player);
		}
	}

	private static void OnRunStarted(RunStartedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			LoadSavedState(player, force: true);
			EnsureDeckInstanceIds(player);
		}
	}

	private static void LoadSavedState(Player player, bool force = false)
	{
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		if (runtime.SavedStateLoaded && !force)
		{
			return;
		}

		if (player.RunState is not RunState)
		{
			return;
		}

		PlayerSaveState saved = SavedData.Get(player);
		runtime.NextInstanceId = Math.Max(1, saved.NextInstanceId);
		runtime.Recent = CloneCombatList(saved.RecentCombats);
		runtime.Current = new CombatSnapshot();
		runtime.CardToInstanceId.Clear();
		runtime.DeckMappingsRestored = false;
		runtime.SavedStateLoaded = true;
	}

	private static void EnsureDeckInstanceIds(Player player)
	{
		IReadOnlyList<CardModel> deckCards = PileType.Deck.GetPile(player).Cards;
		if (deckCards.Count == 0)
		{
			return;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		LoadSavedState(player);

		if (!runtime.DeckMappingsRestored)
		{
			RestoreDeckInstanceMappings(player, runtime, deckCards);
		}

		foreach (CardModel card in deckCards)
		{
			GetOrAssignInstanceId(runtime, card);
		}
	}

	private static void RestoreDeckInstanceMappings(
		Player player,
		PlayerRuntime runtime,
		IReadOnlyList<CardModel> deckCards)
	{
		PlayerSaveState saved = SavedData.Get(player);
		List<DeckInstanceBinding> savedBindings = saved.DeckInstances.Count > 0
			? saved.DeckInstances
			: BuildLegacyBindings(saved.DeckInstanceIds, deckCards);

		runtime.CardToInstanceId.Clear();
		int matched = 0;

		// 牌组顺序即物理牌顺序；读档后对象引用会变，但顺序与上次 Persist 时一致。
		if (savedBindings.Count == deckCards.Count)
		{
			for (int i = 0; i < deckCards.Count; i++)
			{
				DeckInstanceBinding binding = savedBindings[i];
				runtime.CardToInstanceId[deckCards[i]] = binding.InstanceId;
				runtime.NextInstanceId = Math.Max(runtime.NextInstanceId, binding.InstanceId + 1);
				matched++;
			}
		}
		else
		{
			// 牌组数量变化时的兜底：按 cardId + 入组楼层 FIFO 匹配（不含升级/附魔）。
			List<DeckInstanceBinding> unusedBindings = savedBindings.Select(binding => binding.Clone()).ToList();
			foreach (CardModel card in deckCards)
			{
				string matchKey = GetMatchKey(card);
				int index = unusedBindings.FindIndex(binding => binding.MatchKey == matchKey);
				if (index < 0)
				{
					continue;
				}

				DeckInstanceBinding binding = unusedBindings[index];
				unusedBindings.RemoveAt(index);
				runtime.CardToInstanceId[card] = binding.InstanceId;
				runtime.NextInstanceId = Math.Max(runtime.NextInstanceId, binding.InstanceId + 1);
				matched++;
			}

			if (unusedBindings.Count > 0)
			{
				SquMod.Logger.Info(
					$"[CardDrawPlayRateTracker] deck mapping fallback left {unusedBindings.Count} unused bindings");
			}
		}

		runtime.DeckMappingsRestored = true;

		if (matched < deckCards.Count)
		{
			SquMod.Logger.Info(
				$"[CardDrawPlayRateTracker] deck mapping partial after restore: matched {matched}/{deckCards.Count}, "
				+ $"savedBindings={savedBindings.Count}, nextInstanceId={runtime.NextInstanceId}");
		}
	}

	private static List<DeckInstanceBinding> BuildLegacyBindings(
		IReadOnlyList<int> legacyIds,
		IReadOnlyList<CardModel> deckCards)
	{
		if (legacyIds.Count != deckCards.Count)
		{
			return [];
		}

		var bindings = new List<DeckInstanceBinding>(deckCards.Count);
		for (int i = 0; i < deckCards.Count; i++)
		{
			int instanceId = legacyIds[i];
			if (instanceId <= 0)
			{
				continue;
			}

			bindings.Add(CreateBinding(deckCards[i], instanceId));
		}

		return bindings;
	}

	private static bool TryResolveInstanceId(
		Player player,
		CardModel card,
		bool assignIfMissing,
		out int instanceId)
	{
		CardModel? identity = ResolveIdentityCard(card);
		if (identity is null)
		{
			instanceId = 0;
			return false;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		EnsureDeckInstanceIds(player);

		if (runtime.CardToInstanceId.TryGetValue(identity, out instanceId))
		{
			return true;
		}

		if (!assignIfMissing || identity.Pile?.Type != PileType.Deck)
		{
			instanceId = 0;
			return false;
		}

		instanceId = GetOrAssignInstanceId(runtime, identity);
		return true;
	}

	private static CardModel? ResolveIdentityCard(CardModel card)
	{
		if (card.DeckVersion != null)
		{
			return card.DeckVersion;
		}

		return card.Pile?.Type == PileType.Deck ? card : null;
	}

	private static int GetOrAssignInstanceId(PlayerRuntime runtime, CardModel deckCard)
	{
		if (runtime.CardToInstanceId.TryGetValue(deckCard, out int existing))
		{
			return existing;
		}

		int id = runtime.NextInstanceId++;
		runtime.CardToInstanceId[deckCard] = id;
		return id;
	}

	private static void AggregateWindow(
		PlayerRuntime runtime,
		int instanceId,
		int windowSize,
		bool includeCurrentCombat,
		out int draws,
		out int plays)
	{
		draws = 0;
		plays = 0;
		int take = Math.Max(0, windowSize);
		IEnumerable<CombatSnapshot> finished = runtime.Recent.Count <= take
			? runtime.Recent
			: runtime.Recent.Skip(runtime.Recent.Count - take);

		string key = instanceId.ToString();
		foreach (CombatSnapshot snapshot in finished)
		{
			if (snapshot.Cards.TryGetValue(key, out CardCombatStats? stats))
			{
				draws += stats.Draws;
				plays += stats.Plays;
			}
		}

		if (includeCurrentCombat && runtime.Current.Cards.TryGetValue(key, out CardCombatStats? current))
		{
			draws += current.Draws;
			plays += current.Plays;
		}
	}

	private static PlayerRuntime GetOrCreateRuntime(Player player)
	{
		if (Runtimes.TryGetValue(player.NetId, out PlayerRuntime? runtime))
		{
			return runtime;
		}

		runtime = new PlayerRuntime();
		Runtimes[player.NetId] = runtime;
		return runtime;
	}

	private static void Persist(Player player)
	{
		if (player.RunState is not RunState)
		{
			return;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		SavedData.Modify(player, saved =>
		{
			saved.NextInstanceId = runtime.NextInstanceId;
			saved.RecentCombats = CloneCombatList(runtime.Recent);
			saved.DeckInstances = PileType.Deck.GetPile(player).Cards
				.Select(card => CreateBinding(card, GetOrAssignInstanceId(runtime, card)))
				.ToList();
			saved.DeckInstanceIds = [];
		});
	}

	private static DeckInstanceBinding CreateBinding(CardModel card, int instanceId)
	{
		return new DeckInstanceBinding
		{
			InstanceId = instanceId,
			MatchKey = GetMatchKey(card),
		};
	}

	/// <summary>
	/// 仅用于牌组数量变化时的兜底匹配；正常读档按牌组索引恢复。
	/// 不含升级/附魔，同一张物理牌在变身后 Id 会变，需重新分配实例。
	/// </summary>
	private static string GetMatchKey(CardModel card)
	{
		string floor = card.FloorAddedToDeck?.ToString() ?? "none";
		return $"{card.Id.Entry}|f{floor}";
	}

	private static List<CombatSnapshot> CloneCombatList(IEnumerable<CombatSnapshot>? source) =>
		source?.Select(snapshot => snapshot.Clone()).ToList() ?? [];

	private static void AppendSnapshotSection(StringBuilder builder, string title, IEnumerable<string> lines)
	{
		builder.Append("  ").AppendLine(title + ":");
		foreach (string line in lines)
		{
			builder.AppendLine(line);
		}
	}

	private static void AppendCombatSnapshot(StringBuilder builder, CombatSnapshot snapshot, string indent)
	{
		if (snapshot.Cards.Count == 0)
		{
			builder.Append(indent).AppendLine("(empty)");
			return;
		}

		foreach ((string instanceId, CardCombatStats stats) in snapshot.Cards
			         .OrderBy(pair => int.Parse(pair.Key)))
		{
			builder.Append(indent).Append("#").Append(instanceId)
				.Append(" draws=").Append(stats.Draws)
				.Append(", plays=").Append(stats.Plays)
				.Append(", rate=").Append(FormatRate(stats.Draws, stats.Plays))
				.AppendLine();
		}
	}

	private static string FormatCardLabel(CardModel card)
	{
		CardModel identity = card.DeckVersion ?? card;
		return $"{identity.Title} ({identity.Id.Entry})";
	}

	private static string FormatRate(int draws, int plays) =>
		draws <= 0 ? "n/a" : $"{plays}/{draws} ({(100f * plays / draws):0.##}%)";

	private sealed class PlayerRuntime
	{
		public int NextInstanceId = 1;

		public bool SavedStateLoaded;

		public bool DeckMappingsRestored;

		public Dictionary<CardModel, int> CardToInstanceId { get; } = new();

		public List<CombatSnapshot> Recent { get; set; } = [];

		public CombatSnapshot Current { get; set; } = new();
	}

	public sealed class PlayerSaveState
	{
		public int NextInstanceId { get; set; } = 1;

		/// <summary>旧版按牌组顺序存的实例 ID，仅用于迁移。</summary>
		public List<int> DeckInstanceIds { get; set; } = [];

		public List<DeckInstanceBinding> DeckInstances { get; set; } = [];

		public List<CombatSnapshot> RecentCombats { get; set; } = [];
	}

	public sealed class DeckInstanceBinding
	{
		public int InstanceId { get; set; }

		/// <summary>兜底匹配用；正常读档按牌组索引恢复，不依赖此字段。</summary>
		public string MatchKey { get; set; } = string.Empty;

		public DeckInstanceBinding Clone() => new()
		{
			InstanceId = InstanceId,
			MatchKey = MatchKey,
		};
	}

	public sealed class CombatSnapshot
	{
		public Dictionary<string, CardCombatStats> Cards { get; set; } = new();

		public void AddDraw(int instanceId) => GetOrCreate(instanceId).Draws++;

		public void AddPlay(int instanceId) => GetOrCreate(instanceId).Plays++;

		public CombatSnapshot Clone()
		{
			var clone = new CombatSnapshot();
			foreach ((string key, CardCombatStats stats) in Cards)
			{
				clone.Cards[key] = new CardCombatStats
				{
					Draws = stats.Draws,
					Plays = stats.Plays,
				};
			}

			return clone;
		}

		private CardCombatStats GetOrCreate(int instanceId)
		{
			string key = instanceId.ToString();
			if (!Cards.TryGetValue(key, out CardCombatStats? stats))
			{
				stats = new CardCombatStats();
				Cards[key] = stats;
			}

			return stats;
		}
	}

	public sealed class CardCombatStats
	{
		public int Draws { get; set; }

		public int Plays { get; set; }
	}
}
