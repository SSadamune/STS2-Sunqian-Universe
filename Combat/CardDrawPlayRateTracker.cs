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
/// 按卡牌稳定身份跟踪打出率，并保留最近若干场已结束战斗的快照。
/// 身份由 Entry、升级次数、获得楼层、附魔决定，host/client 对同一张逻辑牌看到同一套数字。
/// 打出率 = PlayCount / (PlayWithoutDiscardOrExhaustCount + ExhaustEntryCount + DiscardEntryCount)：
/// PlayCount = 打出次数（含自动打出/任意阶段；重放仅计 PlayIndex==0）；
/// PlayWithoutDiscardOrExhaustCount = 打出后未进入消耗堆或弃牌堆的次数；
/// ExhaustEntryCount = 进入消耗堆次数；
/// DiscardEntryCount = 进入弃牌堆次数。
/// </summary>
public static class CardDrawPlayRateTracker
{
	public const int DefaultWindowSize = 5;

	public const int MaxStoredCombats = 10;

	private const string SaveKey = "card_play_rate_v3";

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
		RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(OnCardPlayed);
		RitsuLibFramework.SubscribeLifecycle<CardMovedBetweenPilesEvent>(OnCardMovedBetweenPiles);
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded);
		RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded);
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
		RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => Runtimes.Clear());
	}

	/// <summary>
	/// 从抽牌堆中选出打出率最高的至多 <paramref name="count"/> 张牌。
	/// 分母为 0 时视为打出率 0；率相同则优先打出次数更多者，再按获得顺序、身份键、抽牌堆位置。
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

		List<RankedDrawCard> ranked = [];
		int drawIndex = 0;
		foreach (CardModel card in PileType.Draw.GetPile(player).Cards)
		{
			TryGetStats(
				player,
				card,
				windowSize,
				includeCurrentCombat,
				out int playCount,
				out int playWithoutDiscardOrExhaustCount,
				out int exhaustEntryCount,
				out int discardEntryCount);
			int denominator = playWithoutDiscardOrExhaustCount + exhaustEntryCount + discardEntryCount;
			CardModel identity = ResolveIdentityCard(card) ?? card;
			ranked.Add(new RankedDrawCard(
				card,
				playCount,
				denominator,
				identity.FloorAddedToDeck ?? int.MaxValue,
				identity.Id.Entry,
				GetIdentityKey(identity),
				drawIndex));
			drawIndex++;
		}

		ranked.Sort(CompareRankedDrawCards);
		int take = Math.Min(count, ranked.Count);
		var selected = new List<CardModel>(take);
		for (int i = 0; i < take; i++)
		{
			selected.Add(ranked[i].Card);
		}

		return selected;
	}

	/// <summary>
	/// 返回牌组中打出率最高的全部卡牌（并列全收）。
	/// 分母为 0 时视为打出率 0；可通过 <paramref name="exclude"/> 排除若干牌。
	/// </summary>
	public static HashSet<CardModel> GetHighestPlayRateDeckCards(
		Player player,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null)
	{
		List<(CardModel Card, int PlayCount, int Denominator)> ranked = [];
		foreach (CardModel card in PileType.Deck.GetPile(player).Cards)
		{
			if (exclude?.Invoke(card) == true)
			{
				continue;
			}

			TryGetStats(
				player,
				card,
				windowSize,
				includeCurrentCombat,
				out int playCount,
				out int playWithoutDiscardOrExhaustCount,
				out int exhaustEntryCount,
				out int discardEntryCount);
			int denominator = playWithoutDiscardOrExhaustCount + exhaustEntryCount + discardEntryCount;
			ranked.Add((card, playCount, denominator));
		}

		if (ranked.Count == 0)
		{
			return [];
		}

		(CardModel Card, int PlayCount, int Denominator) best = ranked[0];
		foreach ((CardModel Card, int PlayCount, int Denominator) entry in ranked)
		{
			if (CompareRate(entry.PlayCount, entry.Denominator, best.PlayCount, best.Denominator) > 0)
			{
				best = entry;
			}
		}

		return ranked
			.Where(entry => CompareRate(entry.PlayCount, entry.Denominator, best.PlayCount, best.Denominator) == 0)
			.Select(entry => entry.Card)
			.ToHashSet();
	}

	public static bool TryGetStats(
		Player player,
		CardModel card,
		int windowSize,
		bool includeCurrentCombat,
		out int playCount,
		out int playWithoutDiscardOrExhaustCount,
		out int exhaustEntryCount,
		out int discardEntryCount)
	{
		playCount = 0;
		playWithoutDiscardOrExhaustCount = 0;
		exhaustEntryCount = 0;
		discardEntryCount = 0;
		if (!TryGetStatsKey(card, out string statsKey))
		{
			return false;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		AggregateWindow(
			runtime,
			statsKey,
			windowSize,
			includeCurrentCombat,
			out playCount,
			out playWithoutDiscardOrExhaustCount,
			out exhaustEntryCount,
			out discardEntryCount);
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
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		var builder = new StringBuilder();
		builder.AppendLine("[CardDrawPlayRateTracker] snapshot");
		if (!string.IsNullOrWhiteSpace(reason))
		{
			builder.Append("  reason: ").AppendLine(reason);
		}

		builder.AppendLine(
			"  formula: PlayCount/(PlayWithoutDiscardOrExhaustCount+ExhaustEntryCount+DiscardEntryCount)");
		builder.Append("  playerNetId: ").AppendLine(player.NetId.ToString());
		builder.Append("  windowSize: ").AppendLine(windowSize.ToString());
		builder.Append("  includeCurrentCombat: ").AppendLine(includeCurrentCombat.ToString());
		builder.Append("  storedCombats: ").Append(runtime.Recent.Count)
			.Append('/').AppendLine(MaxStoredCombats.ToString());
		builder.Append("  pendingPlayOutcomes: ").AppendLine(runtime.PendingPlayOutcomes.Count.ToString());

		AppendSnapshotSection(builder, "deckIdentityKeys", PileType.Deck.GetPile(player).Cards
			.Select(card => $"    {GetIdentityKey(card)}: {FormatCardLabel(card)}")
			.OrderBy(line => line, StringComparer.Ordinal));

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
			AppendCardRateLine(builder, "    ", player, card, windowSize, includeCurrentCombat);
		}

		if (selectedCards is { Count: > 0 })
		{
			builder.AppendLine("  selectedThisPlay:");
			foreach (CardModel card in selectedCards)
			{
				AppendCardRateLine(builder, "    ", player, card, windowSize, includeCurrentCombat);
			}
		}

		SquMod.Logger.Info(builder.ToString());
	}

	private static void AppendCardRateLine(
		StringBuilder builder,
		string indent,
		Player player,
		CardModel card,
		int windowSize,
		bool includeCurrentCombat)
	{
		builder.Append(indent).Append(FormatCardLabel(card));
		CardModel identity = ResolveIdentityCard(card) ?? card;
		builder.Append(" [").Append(GetIdentityKey(identity)).Append(']');
		if (!TryGetStats(
			    player,
			    card,
			    windowSize,
			    includeCurrentCombat,
			    out int playCount,
			    out int playWithoutDiscardOrExhaustCount,
			    out int exhaustEntryCount,
			    out int discardEntryCount))
		{
			builder.AppendLine(" -> no tracked stats");
			return;
		}

		builder.Append(" -> playCount=").Append(playCount)
			.Append(", playWithoutDiscardOrExhaustCount=").Append(playWithoutDiscardOrExhaustCount)
			.Append(", exhaustEntryCount=").Append(exhaustEntryCount)
			.Append(", discardEntryCount=").Append(discardEntryCount)
			.Append(", rate=").Append(FormatRate(
				playCount,
				playWithoutDiscardOrExhaustCount,
				exhaustEntryCount,
				discardEntryCount))
			.AppendLine();
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
		if (player is null || !TryGetStatsKey(card, out string statsKey))
		{
			return;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		runtime.Current.AddPlayCount(statsKey);
		runtime.PendingPlayOutcomes[statsKey] = runtime.PendingPlayOutcomes.GetValueOrDefault(statsKey) + 1;
	}

	private static void OnCardMovedBetweenPiles(CardMovedBetweenPilesEvent evt)
	{
		CardModel card = evt.Card;
		Player? player = card.Owner;
		if (player is null || !TryGetStatsKey(card, out string statsKey))
		{
			return;
		}

		PileType previousPile = evt.PreviousPile;
		PileType? newPile = card.Pile?.Type;
		PlayerRuntime runtime = GetOrCreateRuntime(player);

		if (newPile == PileType.Exhaust && previousPile != PileType.Exhaust)
		{
			runtime.Current.AddExhaustEntry(statsKey);
			DecrementPending(runtime, statsKey);
			return;
		}

		if (newPile == PileType.Discard && previousPile != PileType.Discard)
		{
			runtime.Current.AddDiscardEntry(statsKey);
			DecrementPending(runtime, statsKey);
			return;
		}

		// 打出后离开 Play 且未进消耗/弃牌（能力牌离场、回手等）。
		if (previousPile == PileType.Play && DecrementPending(runtime, statsKey))
		{
			runtime.Current.AddPlayWithoutDiscardOrExhaust(statsKey);
		}
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
			PlayerRuntime runtime = GetOrCreateRuntime(player);
			runtime.Current = new CombatSnapshot();
			runtime.PendingPlayOutcomes.Clear();
		}
	}

	private static void OnCombatEnded(CombatEndedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			PlayerRuntime runtime = GetOrCreateRuntime(player);
			FlushPendingPlayOutcomes(runtime);
			if (runtime.Current.Cards.Count > 0)
			{
				runtime.Recent.Add(runtime.Current);
				while (runtime.Recent.Count > MaxStoredCombats)
				{
					runtime.Recent.RemoveAt(0);
				}
			}

			runtime.Current = new CombatSnapshot();
			Persist(player);
		}
	}

	private static void FlushPendingPlayOutcomes(PlayerRuntime runtime)
	{
		foreach ((string statsKey, int pending) in runtime.PendingPlayOutcomes.ToList())
		{
			for (int i = 0; i < pending; i++)
			{
				runtime.Current.AddPlayWithoutDiscardOrExhaust(statsKey);
			}
		}

		runtime.PendingPlayOutcomes.Clear();
	}

	private static void OnRunLoaded(RunLoadedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			LoadSavedState(player, force: true);
		}
	}

	private static void OnRunStarted(RunStartedEvent evt)
	{
		foreach (Player player in evt.RunState.Players)
		{
			LoadSavedState(player, force: true);
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
		runtime.Recent = CloneCombatList(saved.RecentCombats);
		runtime.Current = new CombatSnapshot();
		runtime.PendingPlayOutcomes.Clear();
		runtime.SavedStateLoaded = true;
	}

	private static bool TryGetStatsKey(CardModel card, out string statsKey)
	{
		CardModel? identity = ResolveIdentityCard(card);
		if (identity is null)
		{
			statsKey = string.Empty;
			return false;
		}

		statsKey = GetIdentityKey(identity);
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

	/// <summary>
	/// 可从复制状态推导的稳定统计键。同名、同升级、同楼层、同附魔的牌共享打出率。
	/// </summary>
	private static string GetIdentityKey(CardModel card)
	{
		string floor = card.FloorAddedToDeck?.ToString() ?? "none";
		string enchantment = card.Enchantment == null
			? "none"
			: $"{card.Enchantment.Id.Entry}:{card.Enchantment.Amount}";
		return $"{card.Id.Entry}|u{card.CurrentUpgradeLevel}|f{floor}|e{enchantment}";
	}

	private static void AggregateWindow(
		PlayerRuntime runtime,
		string statsKey,
		int windowSize,
		bool includeCurrentCombat,
		out int playCount,
		out int playWithoutDiscardOrExhaustCount,
		out int exhaustEntryCount,
		out int discardEntryCount)
	{
		playCount = 0;
		playWithoutDiscardOrExhaustCount = 0;
		exhaustEntryCount = 0;
		discardEntryCount = 0;
		int take = Math.Max(0, windowSize);
		IEnumerable<CombatSnapshot> finished = runtime.Recent.Count <= take
			? runtime.Recent
			: runtime.Recent.Skip(runtime.Recent.Count - take);

		foreach (CombatSnapshot snapshot in finished)
		{
			if (snapshot.Cards.TryGetValue(statsKey, out CardCombatStats? stats))
			{
				playCount += stats.PlayCount;
				playWithoutDiscardOrExhaustCount += stats.PlayWithoutDiscardOrExhaustCount;
				exhaustEntryCount += stats.ExhaustEntryCount;
				discardEntryCount += stats.DiscardEntryCount;
			}
		}

		if (includeCurrentCombat && runtime.Current.Cards.TryGetValue(statsKey, out CardCombatStats? current))
		{
			playCount += current.PlayCount;
			playWithoutDiscardOrExhaustCount += current.PlayWithoutDiscardOrExhaustCount;
			exhaustEntryCount += current.ExhaustEntryCount;
			discardEntryCount += current.DiscardEntryCount;
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
			saved.RecentCombats = CloneCombatList(runtime.Recent);
		});
	}

	private static bool DecrementPending(PlayerRuntime runtime, string statsKey)
	{
		if (!runtime.PendingPlayOutcomes.TryGetValue(statsKey, out int pending) || pending <= 0)
		{
			return false;
		}

		if (pending == 1)
		{
			runtime.PendingPlayOutcomes.Remove(statsKey);
		}
		else
		{
			runtime.PendingPlayOutcomes[statsKey] = pending - 1;
		}

		return true;
	}

	/// <summary>
	/// 交叉相乘比较打出率，避免 float 非确定性。分母为 0 时视为 0。
	/// </summary>
	private static int CompareRate(int playA, int denominatorA, int playB, int denominatorB)
	{
		long left = (long)(denominatorA > 0 ? playA : 0) * (denominatorB > 0 ? denominatorB : 1);
		long right = (long)(denominatorB > 0 ? playB : 0) * (denominatorA > 0 ? denominatorA : 1);
		return left.CompareTo(right);
	}

	private static int CompareRankedDrawCards(RankedDrawCard left, RankedDrawCard right)
	{
		int rateCmp = CompareRate(left.PlayCount, left.Denominator, right.PlayCount, right.Denominator);
		if (rateCmp != 0)
		{
			return -rateCmp;
		}

		int playCmp = left.PlayCount.CompareTo(right.PlayCount);
		if (playCmp != 0)
		{
			return -playCmp;
		}

		int floorCmp = left.Floor.CompareTo(right.Floor);
		if (floorCmp != 0)
		{
			return floorCmp;
		}

		int entryCmp = string.CompareOrdinal(left.Entry, right.Entry);
		if (entryCmp != 0)
		{
			return entryCmp;
		}

		int keyCmp = string.CompareOrdinal(left.IdentityKey, right.IdentityKey);
		if (keyCmp != 0)
		{
			return keyCmp;
		}

		return left.DrawIndex.CompareTo(right.DrawIndex);
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

		foreach ((string statsKey, CardCombatStats stats) in snapshot.Cards
			         .OrderBy(pair => pair.Key, StringComparer.Ordinal))
		{
			builder.Append(indent).Append(statsKey)
				.Append(" playCount=").Append(stats.PlayCount)
				.Append(", playWithoutDiscardOrExhaustCount=").Append(stats.PlayWithoutDiscardOrExhaustCount)
				.Append(", exhaustEntryCount=").Append(stats.ExhaustEntryCount)
				.Append(", discardEntryCount=").Append(stats.DiscardEntryCount)
				.Append(", rate=").Append(FormatRate(
					stats.PlayCount,
					stats.PlayWithoutDiscardOrExhaustCount,
					stats.ExhaustEntryCount,
					stats.DiscardEntryCount))
				.AppendLine();
		}
	}

	private static string FormatCardLabel(CardModel card)
	{
		CardModel identity = card.DeckVersion ?? card;
		return $"{identity.Title} ({identity.Id.Entry})";
	}

	private static string FormatRate(
		int playCount,
		int playWithoutDiscardOrExhaustCount,
		int exhaustEntryCount,
		int discardEntryCount)
	{
		int denominator = playWithoutDiscardOrExhaustCount + exhaustEntryCount + discardEntryCount;
		return denominator <= 0
			? "n/a"
			: $"{playCount}/{denominator} ({(100f * playCount / denominator):0.##}%)";
	}

	private readonly record struct RankedDrawCard(
		CardModel Card,
		int PlayCount,
		int Denominator,
		int Floor,
		string Entry,
		string IdentityKey,
		int DrawIndex);

	private sealed class PlayerRuntime
	{
		public bool SavedStateLoaded;

		/// <summary>
		/// 已计入 PlayCount、等待判定是否记入 PlayWithoutDiscardOrExhaustCount 的身份键及其次数。
		/// </summary>
		public Dictionary<string, int> PendingPlayOutcomes { get; } = new(StringComparer.Ordinal);

		public List<CombatSnapshot> Recent { get; set; } = [];

		public CombatSnapshot Current { get; set; } = new();
	}

	public sealed class PlayerSaveState
	{
		public List<CombatSnapshot> RecentCombats { get; set; } = [];
	}

	public sealed class CombatSnapshot
	{
		public Dictionary<string, CardCombatStats> Cards { get; set; } = new(StringComparer.Ordinal);

		public void AddPlayCount(string statsKey) => GetOrCreate(statsKey).PlayCount++;

		public void AddPlayWithoutDiscardOrExhaust(string statsKey) =>
			GetOrCreate(statsKey).PlayWithoutDiscardOrExhaustCount++;

		public void AddExhaustEntry(string statsKey) => GetOrCreate(statsKey).ExhaustEntryCount++;

		public void AddDiscardEntry(string statsKey) => GetOrCreate(statsKey).DiscardEntryCount++;

		public CombatSnapshot Clone()
		{
			var clone = new CombatSnapshot();
			foreach ((string key, CardCombatStats stats) in Cards)
			{
				clone.Cards[key] = new CardCombatStats
				{
					PlayCount = stats.PlayCount,
					PlayWithoutDiscardOrExhaustCount = stats.PlayWithoutDiscardOrExhaustCount,
					ExhaustEntryCount = stats.ExhaustEntryCount,
					DiscardEntryCount = stats.DiscardEntryCount,
				};
			}

			return clone;
		}

		private CardCombatStats GetOrCreate(string statsKey)
		{
			if (!Cards.TryGetValue(statsKey, out CardCombatStats? stats))
			{
				stats = new CardCombatStats();
				Cards[statsKey] = stats;
			}

			return stats;
		}
	}

	public sealed class CardCombatStats
	{
		/// <summary>打出次数（含自动打出；重放仅计第一次）。</summary>
		public int PlayCount { get; set; }

		/// <summary>打出后未进入消耗堆或弃牌堆的次数。</summary>
		public int PlayWithoutDiscardOrExhaustCount { get; set; }

		/// <summary>进入消耗堆的次数。</summary>
		public int ExhaustEntryCount { get; set; }

		/// <summary>进入弃牌堆的次数。</summary>
		public int DiscardEntryCount { get; set; }
	}
}
