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
/// 打出率 = PlayFromHandCount / HandEntryCount：
/// HandEntryCount = 进入手牌次数（抽牌、从其它区域移入、打出后回手如 Particle Wall）；
/// PlayFromHandCount = 从手牌进入打出区的次数（抽牌堆自动打出不计；重放不会再次从手牌出发）。
/// 当前仍留在手牌、尚未打出的停留计入分母、不计入分子。
/// </summary>
public static class CardDrawPlayRateTracker
{
	public const int DefaultWindowSize = 5;

	public const int MaxStoredCombats = 10;

	private const string SaveKey = "card_play_rate_v4";

	private const string CelebrateMourningEntrySuffix = "CELEBRATE_MOURNING";

	private static readonly PileType[] CelebrateMourningScanPiles =
	[
		PileType.Hand,
		PileType.Draw,
		PileType.Discard,
		PileType.Exhaust,
		PileType.Play,
	];

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
		RitsuLibFramework.SubscribeLifecycle<CardMovedBetweenPilesEvent>(OnCardMovedBetweenPiles);
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded);
		RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded);
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
		RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => Runtimes.Clear());
	}

	/// <summary>
	/// 从抽牌堆中选出打出率最高的至多 <paramref name="count"/> 张牌。
	/// 分母为 0 时视为打出率 0；率相同则优先从手牌打出次数更多者，再按获得顺序、身份键、抽牌堆位置。
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
				out int playFromHandCount,
				out int handEntryCount);
			CardModel identity = ResolveIdentityCard(card) ?? card;
			ranked.Add(new RankedDrawCard(
				card,
				playFromHandCount,
				handEntryCount,
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
				out int playFromHandCount,
				out int handEntryCount);
			ranked.Add((card, playFromHandCount, handEntryCount));
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

	/// <summary>
	/// 判断被消耗牌是否属于最高打出率档。消耗不再改写打出率，此方法等价于
	/// <see cref="IsAmongHighestPlayRateDeckCards"/>，保留给《闻丧贺喜》调用。
	/// </summary>
	public static bool WasAmongHighestPlayRateDeckCardsBeforeExhaust(
		Player player,
		CardModel exhaustedCard,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null) =>
		IsAmongHighestPlayRateDeckCards(
			player,
			exhaustedCard,
			windowSize,
			includeCurrentCombat,
			exclude);

	/// <summary>
	/// 判断 <paramref name="card"/> 是否与牌组中当前最高打出率档的任一牌共享身份键。
	/// 用身份键而非 <see cref="CardModel"/> 引用比较，避免 DeckVersion 与牌组实例不一致时误判。
	/// </summary>
	public static bool IsAmongHighestPlayRateDeckCards(
		Player player,
		CardModel card,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null)
	{
		CardModel? identity = ResolveIdentityCard(card);
		if (identity is null)
		{
			return false;
		}

		string targetKey = GetIdentityKey(identity);
		HashSet<CardModel> highest = GetHighestPlayRateDeckCards(
			player,
			windowSize,
			includeCurrentCombat,
			exclude);

		foreach (CardModel deckCard in highest)
		{
			CardModel deckIdentity = ResolveIdentityCard(deckCard) ?? deckCard;
			if (GetIdentityKey(deckIdentity) == targetKey)
			{
				return true;
			}
		}

		return false;
	}

	public static bool TryGetStats(
		Player player,
		CardModel card,
		int windowSize,
		bool includeCurrentCombat,
		out int playFromHandCount,
		out int handEntryCount)
	{
		playFromHandCount = 0;
		handEntryCount = 0;
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
			out playFromHandCount,
			out handEntryCount);
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

		builder.AppendLine("  formula: PlayFromHandCount/HandEntryCount");
		builder.Append("  playerNetId: ").AppendLine(player.NetId.ToString());
		builder.Append("  windowSize: ").AppendLine(windowSize.ToString());
		builder.Append("  includeCurrentCombat: ").AppendLine(includeCurrentCombat.ToString());
		builder.Append("  storedCombats: ").Append(runtime.Recent.Count)
			.Append('/').AppendLine(MaxStoredCombats.ToString());

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

	/// <summary>
	/// 临时诊断：任意牌进入消耗堆时记录打出率排名与「闻丧贺喜」位置（Debug Log Viewer）。
	/// </summary>
	public static void LogCardExhaustedDiagnostics(
		Player player,
		CardModel exhaustedCard,
		PileType previousPile,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = true)
	{
		var builder = new StringBuilder();
		builder.AppendLine("[CardDrawPlayRateTracker] card exhausted (Celebrate Mourning debug)");
		builder.Append("  exhaustedFrom: ").AppendLine(previousPile.ToString());
		builder.Append("  exhaustedCard: ").Append(FormatCardLabel(exhaustedCard));
		builder.Append(" objectHash=").AppendLine(exhaustedCard.GetHashCode().ToString());

		CardModel? resolvedIdentity = ResolveIdentityCard(exhaustedCard);
		if (resolvedIdentity is null)
		{
			builder.AppendLine("  resolveIdentity: FAILED (no DeckVersion and not in Deck pile)");
		}
		else
		{
			builder.Append("  resolveIdentity: ").Append(FormatCardLabel(resolvedIdentity))
				.Append(" [").Append(GetIdentityKey(resolvedIdentity)).AppendLine("]");
		}

		builder.Append("  deckVersion: ")
			.AppendLine(exhaustedCard.DeckVersion is null ? "null" : FormatCardLabel(exhaustedCard.DeckVersion));

		if (TryGetStats(
			    player,
			    exhaustedCard,
			    windowSize,
			    includeCurrentCombat,
			    out int playFromHandCount,
			    out int handEntryCount))
		{
			builder.Append("  exhaustedCardStats: playFromHandCount=").Append(playFromHandCount)
				.Append(", handEntryCount=").Append(handEntryCount)
				.Append(", rate=").AppendLine(FormatRate(playFromHandCount, handEntryCount));
		}
		else
		{
			builder.AppendLine("  exhaustedCardStats: (no tracked stats for this card)");
		}

		List<RankedDeckCard> rankedOthers = GetRankedDeckCards(
			player,
			windowSize,
			includeCurrentCombat,
			exclude: IsCelebrateMourningCard);
		AppendExhaustedPlayRateRank(builder, exhaustedCard, rankedOthers);

		builder.AppendLine("  highestOtherPlayRateDeckCards:");
		HashSet<CardModel> highestOthers = GetHighestPlayRateDeckCards(
			player,
			windowSize,
			includeCurrentCombat,
			exclude: IsCelebrateMourningCard);
		if (highestOthers.Count == 0)
		{
			builder.AppendLine("    (none)");
		}
		else
		{
			foreach (CardModel deckCard in highestOthers.OrderBy(card => card.Title, StringComparer.Ordinal))
			{
				builder.Append("    ").AppendLine(FormatCardLabel(deckCard));
			}
		}

		bool isAmongHighestOthers = IsAmongHighestPlayRateDeckCards(
			player,
			exhaustedCard,
			windowSize,
			includeCurrentCombat,
			exclude: IsCelebrateMourningCard);
		bool wasAmongHighestBeforeExhaust = WasAmongHighestPlayRateDeckCardsBeforeExhaust(
			player,
			exhaustedCard,
			windowSize,
			includeCurrentCombat,
			exclude: IsCelebrateMourningCard);
		builder.Append("  isAmongHighestOtherPlayRate: ").AppendLine(isAmongHighestOthers.ToString());
		builder.Append("  wasAmongHighestBeforeThisExhaust: ")
			.AppendLine(wasAmongHighestBeforeExhaust.ToString());
		builder.Append("  isCelebrateMourningCard: ").AppendLine(IsCelebrateMourningCard(exhaustedCard).ToString());

		builder.AppendLine("  deckPlayRateRanking (excluding Celebrate Mourning):");
		if (rankedOthers.Count == 0)
		{
			builder.AppendLine("    (empty deck)");
		}
		else
		{
			for (int i = 0; i < rankedOthers.Count; i++)
			{
				RankedDeckCard entry = rankedOthers[i];
				builder.Append("    #").Append(i + 1).Append(' ')
					.Append(FormatCardLabel(entry.Card))
					.Append(" [").Append(entry.IdentityKey).Append("] rate=")
					.Append(FormatRate(entry.PlayFromHandCount, entry.HandEntryCount))
					.AppendLine();
			}
		}

		builder.AppendLine("  celebrateMourningInstances:");
		AppendCelebrateMourningInstances(
			builder,
			player,
			exhaustedCard,
			windowSize,
			includeCurrentCombat,
			wasAmongHighestBeforeExhaust);

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
			    out int playFromHandCount,
			    out int handEntryCount))
		{
			builder.AppendLine(" -> no tracked stats");
			return;
		}

		builder.Append(" -> playFromHandCount=").Append(playFromHandCount)
			.Append(", handEntryCount=").Append(handEntryCount)
			.Append(", rate=").Append(FormatRate(playFromHandCount, handEntryCount))
			.AppendLine();
	}

	private static void OnCardMovedBetweenPiles(CardMovedBetweenPilesEvent evt)
	{
		CardModel card = evt.Card;
		Player? player = card.Owner;
		if (player is null)
		{
			return;
		}

		PileType previousPile = evt.PreviousPile;
		PileType? newPile = card.Pile?.Type;
		if (newPile == PileType.Exhaust && previousPile != PileType.Exhaust)
		{
			LogCardExhaustedDiagnostics(player, card, previousPile);
		}

		if (!TryGetStatsKey(card, out string statsKey))
		{
			return;
		}

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		if (newPile == PileType.Hand && previousPile != PileType.Hand)
		{
			runtime.Current.AddHandEntry(statsKey);
			return;
		}

		if (newPile == PileType.Play && previousPile == PileType.Hand)
		{
			runtime.Current.AddPlayFromHand(statsKey);
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
			GetOrCreateRuntime(player).Current = new CombatSnapshot();
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
			Persist(player);
		}
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
		out int playFromHandCount,
		out int handEntryCount)
	{
		playFromHandCount = 0;
		handEntryCount = 0;
		int take = Math.Max(0, windowSize);
		IEnumerable<CombatSnapshot> finished = runtime.Recent.Count <= take
			? runtime.Recent
			: runtime.Recent.Skip(runtime.Recent.Count - take);

		foreach (CombatSnapshot snapshot in finished)
		{
			if (snapshot.Cards.TryGetValue(statsKey, out CardCombatStats? stats))
			{
				playFromHandCount += stats.PlayFromHandCount;
				handEntryCount += stats.HandEntryCount;
			}
		}

		if (includeCurrentCombat && runtime.Current.Cards.TryGetValue(statsKey, out CardCombatStats? current))
		{
			playFromHandCount += current.PlayFromHandCount;
			handEntryCount += current.HandEntryCount;
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
		int rateCmp = CompareRate(left.PlayFromHandCount, left.Denominator, right.PlayFromHandCount, right.Denominator);
		if (rateCmp != 0)
		{
			return -rateCmp;
		}

		int playCmp = left.PlayFromHandCount.CompareTo(right.PlayFromHandCount);
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
				.Append(" playFromHandCount=").Append(stats.PlayFromHandCount)
				.Append(", handEntryCount=").Append(stats.HandEntryCount)
				.Append(", rate=").Append(FormatRate(stats.PlayFromHandCount, stats.HandEntryCount))
				.AppendLine();
		}
	}

	private static string FormatCardLabel(CardModel card)
	{
		CardModel identity = card.DeckVersion ?? card;
		return $"{identity.Title} ({identity.Id.Entry})";
	}

	private static string FormatRate(int playFromHandCount, int handEntryCount) =>
		handEntryCount <= 0
			? "n/a"
			: $"{playFromHandCount}/{handEntryCount} ({(100f * playFromHandCount / handEntryCount):0.##}%)";

	private static bool IsCelebrateMourningCard(CardModel card) =>
		card.Id.Entry.EndsWith(CelebrateMourningEntrySuffix, StringComparison.Ordinal)
		|| card.DeckVersion?.Id.Entry.EndsWith(CelebrateMourningEntrySuffix, StringComparison.Ordinal) == true;

	private static List<RankedDeckCard> GetRankedDeckCards(
		Player player,
		int windowSize,
		bool includeCurrentCombat,
		Func<CardModel, bool>? exclude = null)
	{
		List<RankedDeckCard> ranked = [];
		foreach (CardModel deckCard in PileType.Deck.GetPile(player).Cards)
		{
			if (exclude?.Invoke(deckCard) == true)
			{
				continue;
			}

			TryGetStats(
				player,
				deckCard,
				windowSize,
				includeCurrentCombat,
				out int playFromHandCount,
				out int handEntryCount);
			CardModel identity = ResolveIdentityCard(deckCard) ?? deckCard;
			ranked.Add(new RankedDeckCard(
				deckCard,
				playFromHandCount,
				handEntryCount,
				GetIdentityKey(identity)));
		}

		ranked.Sort(CompareRankedDeckCards);
		return ranked;
	}

	private static void AppendExhaustedPlayRateRank(
		StringBuilder builder,
		CardModel exhaustedCard,
		IReadOnlyList<RankedDeckCard> rankedOthers)
	{
		if (!TryGetExhaustedIdentityKey(exhaustedCard, out string exhaustedKey))
		{
			builder.AppendLine("  exhaustedPlayRateRankAmongOthers: (identity unresolved)");
			return;
		}

		int rank = -1;
		for (int i = 0; i < rankedOthers.Count; i++)
		{
			if (rankedOthers[i].IdentityKey == exhaustedKey)
			{
				rank = i + 1;
				break;
			}
		}

		if (rank < 0)
		{
			builder.AppendLine(
				"  exhaustedPlayRateRankAmongOthers: (not found in deck ranking — likely not a deck card identity)");
			return;
		}

		RankedDeckCard entry = rankedOthers[rank - 1];
		builder.Append("  exhaustedPlayRateRankAmongOthers: #").Append(rank)
			.Append('/').Append(rankedOthers.Count)
			.Append(" rate=").Append(FormatRate(entry.PlayFromHandCount, entry.HandEntryCount))
			.AppendLine();
	}

	private static void AppendCelebrateMourningInstances(
		StringBuilder builder,
		Player player,
		CardModel exhaustedCard,
		int windowSize,
		bool includeCurrentCombat,
		bool wasAmongHighestBeforeExhaust)
	{
		bool found = false;
		foreach (PileType pileType in CelebrateMourningScanPiles)
		{
			IReadOnlyList<CardModel> cards = pileType.GetPile(player).Cards;
			for (int index = 0; index < cards.Count; index++)
			{
				CardModel card = cards[index];
				if (!IsCelebrateMourningCard(card))
				{
					continue;
				}

				found = true;
				bool canListen = CanCelebrateMourningListenForReturnTrigger(card.Pile?.Type);
				bool ownerMatches = exhaustedCard.Owner == card.Owner;
				bool exhaustedIsSelf = IsCelebrateMourningCard(exhaustedCard);
				bool wouldTriggerReturn = canListen
				                          && ownerMatches
				                          && !exhaustedIsSelf
				                          && wasAmongHighestBeforeExhaust;

				builder.Append("    ").Append(pileType).Append('[').Append(index).Append("]: ")
					.Append(FormatCardLabel(card))
					.Append(" objectHash=").Append(card.GetHashCode())
					.Append(", canListen=").Append(canListen)
					.Append(", ownerMatches=").Append(ownerMatches)
					.Append(", wouldTriggerReturn=").Append(wouldTriggerReturn);

				if (!wouldTriggerReturn)
				{
					builder.Append(" (");
					if (!canListen)
					{
						builder.Append("pile not listened; ");
					}

					if (!ownerMatches)
					{
						builder.Append("owner mismatch; ");
					}

					if (exhaustedIsSelf)
					{
						builder.Append("exhausted card is Celebrate Mourning; ");
					}

					if (!wasAmongHighestBeforeExhaust)
					{
						builder.Append("exhausted card not among highest other play rate before this exhaust; ");
					}

					builder.Append(')');
				}

				builder.AppendLine();
			}
		}

		if (!found)
		{
			builder.AppendLine("    (none in Hand/Draw/Discard/Exhaust/Play)");
		}
	}

	private static bool TryGetExhaustedIdentityKey(CardModel card, out string identityKey)
	{
		CardModel? identity = ResolveIdentityCard(card);
		if (identity is null)
		{
			identityKey = string.Empty;
			return false;
		}

		identityKey = GetIdentityKey(identity);
		return true;
	}

	private static bool CanCelebrateMourningListenForReturnTrigger(PileType? pileType) =>
		pileType is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust;

	private static int CompareRankedDeckCards(RankedDeckCard left, RankedDeckCard right)
	{
		int rateCmp = CompareRate(left.PlayFromHandCount, left.HandEntryCount, right.PlayFromHandCount, right.HandEntryCount);
		if (rateCmp != 0)
		{
			return -rateCmp;
		}

		int playCmp = left.PlayFromHandCount.CompareTo(right.PlayFromHandCount);
		if (playCmp != 0)
		{
			return -playCmp;
		}

		return string.CompareOrdinal(left.IdentityKey, right.IdentityKey);
	}

	private readonly record struct RankedDeckCard(
		CardModel Card,
		int PlayFromHandCount,
		int HandEntryCount,
		string IdentityKey);

	private readonly record struct RankedDrawCard(
		CardModel Card,
		int PlayFromHandCount,
		int Denominator,
		int Floor,
		string Entry,
		string IdentityKey,
		int DrawIndex);

	private sealed class PlayerRuntime
	{
		public bool SavedStateLoaded;

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

		public void AddPlayFromHand(string statsKey) => GetOrCreate(statsKey).PlayFromHandCount++;

		public void AddHandEntry(string statsKey) => GetOrCreate(statsKey).HandEntryCount++;

		public CombatSnapshot Clone()
		{
			var clone = new CombatSnapshot();
			foreach ((string key, CardCombatStats stats) in Cards)
			{
				clone.Cards[key] = new CardCombatStats
				{
					PlayFromHandCount = stats.PlayFromHandCount,
					HandEntryCount = stats.HandEntryCount,
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
		/// <summary>从手牌进入打出区的次数。</summary>
		public int PlayFromHandCount { get; set; }

		/// <summary>进入手牌的次数。</summary>
		public int HandEntryCount { get; set; }
	}
}
