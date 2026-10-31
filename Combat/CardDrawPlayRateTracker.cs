using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

	private static readonly PileType[] CelebrateMourningHookPiles =
	[
		PileType.Hand,
		PileType.Draw,
		PileType.Discard,
		PileType.Exhaust,
		PileType.Play,
	];

	private static readonly PileType[] CelebrateMourningCandidatePiles =
	[
		PileType.Hand,
		PileType.Draw,
		PileType.Discard,
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
	/// 「家人」的打出率视为 100%；与实际打出率 100% 的牌并列时，家人优先。
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
				card.HasFamily(),
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
	/// 「家人」按 100% 参与比较，因此与实际打出率 100% 的牌并列最高。
	/// 分母为 0 时视为打出率 0；可通过 <paramref name="exclude"/> 排除若干牌。
	/// </summary>
	public static HashSet<CardModel> GetHighestPlayRateDeckCards(
		Player player,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null) =>
		GetHighestPlayRateCards(
			player,
			PileType.Deck.GetPile(player).Cards,
			windowSize,
			includeCurrentCombat,
			exclude);

	/// <summary>
	/// 从任意候选集合中返回有效打出率最高的全部卡牌（并列全收）。
	/// 原始历史统计保持不变；「家人」仅在这一规则层被视为 100%。
	/// </summary>
	public static HashSet<CardModel> GetHighestPlayRateCards(
		Player player,
		IEnumerable<CardModel> candidates,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null)
	{
		List<(CardModel Card, int PlayCount, int Denominator)> ranked = [];
		var statsCache = new Dictionary<string, (int PlayCount, int Denominator)>(StringComparer.Ordinal);
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		foreach (CardModel card in candidates)
		{
			if (exclude?.Invoke(card) == true)
			{
				continue;
			}

			(int playFromHandCount, int handEntryCount) = GetCachedStats(
				runtime,
				card,
				windowSize,
				includeCurrentCombat,
				statsCache);
			ranked.Add((card, playFromHandCount, handEntryCount));
		}

		if (ranked.Count == 0)
		{
			return [];
		}

		(CardModel Card, int PlayCount, int Denominator) best = ranked[0];
		foreach ((CardModel Card, int PlayCount, int Denominator) entry in ranked)
		{
			if (CompareEffectiveRate(
					entry.Card.HasFamily(),
					entry.PlayCount,
					entry.Denominator,
					best.Card.HasFamily(),
					best.PlayCount,
					best.Denominator) > 0)
			{
				best = entry;
			}
		}

		return ranked
			.Where(entry => CompareEffectiveRate(
				entry.Card.HasFamily(),
				entry.PlayCount,
				entry.Denominator,
				best.Card.HasFamily(),
				best.PlayCount,
				best.Denominator) == 0)
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
		List<CardModel> candidates = PileType.Deck.GetPile(player).Cards.ToList();
		if ((card.HasFamily() || IsCelebrateMourningCard(card))
			&& ResolveIdentityCard(card) is null)
		{
			candidates.Add(card);
		}

		return IsAmongHighestPlayRateCards(
			player,
			card,
			candidates,
			windowSize,
			includeCurrentCombat,
			exclude);
	}

	/// <summary>
	/// 判断卡牌是否属于给定候选集合的有效打出率最高档。
	/// 对牌组卡牌按稳定身份匹配，对战斗生成牌按实例匹配。
	/// </summary>
	public static bool IsAmongHighestPlayRateCards(
		Player player,
		CardModel card,
		IEnumerable<CardModel> candidates,
		int windowSize = MaxStoredCombats,
		bool includeCurrentCombat = false,
		Func<CardModel, bool>? exclude = null)
	{
		HashSet<CardModel> highest = GetHighestPlayRateCards(
			player,
			candidates,
			windowSize,
			includeCurrentCombat,
			exclude);
		if (highest.Contains(card))
		{
			return true;
		}

		CardModel? identity = ResolveIdentityCard(card);
		if (identity is null)
		{
			return false;
		}

		string targetKey = GetIdentityKey(identity);

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

	/// <summary>
	/// Returns the live Hand/Draw/Discard candidates used by Celebrate Mourning's
	/// hover preview.
	/// </summary>
	public static List<CardModel> GetCelebrateMourningPlayRateCandidates(Player player) =>
		CaptureCelebrateMourningCandidates(player);

	/// <summary>
	/// Returns the result captured when the card entered Exhaust. The compact result
	/// is shared by every Celebrate Mourning copy, so asynchronous exhaust hooks cannot
	/// change the comparison and multiple copies do not repeat the full scan.
	/// </summary>
	public static bool WasAmongHighestCelebrateMourningCandidatesAtExhaust(
		Player player,
		CardModel exhaustedCard)
	{
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		if (runtime.CelebrateMourningExhaustResults.TryGetValue(exhaustedCard, out bool result))
		{
			return result;
		}

		// Defensive fallback for cards exhausted before lifecycle initialization.
		return IsHighestCelebrateMourningCandidate(player, exhaustedCard);
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

		PlayerRuntime runtime = GetOrCreateRuntime(player);
		if (IsCelebrateMourningCard(card))
		{
			runtime.CelebrateMourningCards.Add(card);
		}

		PileType previousPile = evt.PreviousPile;
		PileType? newPile = card.Pile?.Type;
		if (newPile == PileType.Exhaust
			&& previousPile != PileType.Exhaust
			&& HasCelebrateMourningHookListener(runtime))
		{
			runtime.CelebrateMourningExhaustResults[card] =
				IsHighestCelebrateMourningCandidate(player, card);
		}

		if (!TryGetStatsKey(card, out string statsKey))
		{
			return;
		}

		if (newPile == PileType.Hand && previousPile != PileType.Hand)
		{
			runtime.Current.AddHandEntry(statsKey);
			return;
		}

		if (newPile == PileType.Play
			&& previousPile == PileType.Hand
			&& !CardResolutionTracker.IsBeingAutoPlayed(card))
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
			PlayerRuntime runtime = GetOrCreateRuntime(player);
			runtime.Current = new CombatSnapshot();
			runtime.CelebrateMourningExhaustResults.Clear();
			runtime.CelebrateMourningCards.Clear();
			CaptureCelebrateMourningCards(player, runtime);
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
			runtime.CelebrateMourningExhaustResults.Clear();
			runtime.CelebrateMourningCards.Clear();
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
		runtime.Recent = CloneCombatList(saved.RecentCombats.TakeLast(MaxStoredCombats));
		runtime.Current = new CombatSnapshot();
		runtime.CelebrateMourningExhaustResults.Clear();
		runtime.CelebrateMourningCards.Clear();
		if (player.PlayerCombatState is not null)
		{
			CaptureCelebrateMourningCards(player, runtime);
		}
		runtime.SavedStateLoaded = true;
	}

	private static List<CardModel> CaptureCelebrateMourningCandidates(Player player)
	{
		List<CardModel> candidates = [];
		foreach (PileType pileType in CelebrateMourningCandidatePiles)
		{
			foreach (CardModel card in pileType.GetPile(player).Cards)
			{
				candidates.Add(card);
			}
		}

		return candidates;
	}

	private static bool IsHighestCelebrateMourningCandidate(Player player, CardModel exhaustedCard)
	{
		PlayerRuntime runtime = GetOrCreateRuntime(player);
		var statsCache = new Dictionary<string, (int PlayCount, int Denominator)>(StringComparer.Ordinal);
		(int exhaustedPlays, int exhaustedEntries) = GetCachedStats(
			runtime,
			exhaustedCard,
			MaxStoredCombats,
			includeCurrentCombat: true,
			statsCache);

		foreach (PileType pileType in CelebrateMourningCandidatePiles)
		{
			foreach (CardModel candidate in pileType.GetPile(player).Cards)
			{
				if (ReferenceEquals(candidate, exhaustedCard))
				{
					continue;
				}

				(int candidatePlays, int candidateEntries) = GetCachedStats(
					runtime,
					candidate,
					MaxStoredCombats,
					includeCurrentCombat: true,
					statsCache);
				if (CompareEffectiveRate(
						candidate.HasFamily(),
						candidatePlays,
						candidateEntries,
						exhaustedCard.HasFamily(),
						exhaustedPlays,
						exhaustedEntries) > 0)
				{
					return false;
				}
			}
		}

		return true;
	}

	private static void CaptureCelebrateMourningCards(Player player, PlayerRuntime runtime)
	{
		foreach (PileType pileType in CelebrateMourningHookPiles)
		{
			foreach (CardModel card in pileType.GetPile(player).Cards)
			{
				if (IsCelebrateMourningCard(card))
				{
					runtime.CelebrateMourningCards.Add(card);
				}
			}
		}
	}

	private static bool HasCelebrateMourningHookListener(PlayerRuntime runtime)
	{
		runtime.CelebrateMourningCards.RemoveWhere(card => card.HasBeenRemovedFromState);
		return runtime.CelebrateMourningCards.Count > 0;
	}

	private static (int PlayCount, int Denominator) GetCachedStats(
		PlayerRuntime runtime,
		CardModel card,
		int windowSize,
		bool includeCurrentCombat,
		Dictionary<string, (int PlayCount, int Denominator)> statsCache)
	{
		if (!TryGetStatsKey(card, out string statsKey))
		{
			return (0, 0);
		}

		if (statsCache.TryGetValue(statsKey, out var stats))
		{
			return stats;
		}

		AggregateWindow(
			runtime,
			statsKey,
			windowSize,
			includeCurrentCombat,
			out int playCount,
			out int denominator);
		stats = (playCount, denominator);
		statsCache[statsKey] = stats;
		return stats;
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
			saved.RecentCombats = CloneCombatList(runtime.Recent.TakeLast(MaxStoredCombats));
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

	private static int CompareEffectiveRate(
		bool isFamilyA,
		int playA,
		int denominatorA,
		bool isFamilyB,
		int playB,
		int denominatorB) =>
		CompareRate(
			isFamilyA ? 1 : playA,
			isFamilyA ? 1 : denominatorA,
			isFamilyB ? 1 : playB,
			isFamilyB ? 1 : denominatorB);

	private static int CompareRankedDrawCards(RankedDrawCard left, RankedDrawCard right)
	{
		int rateCmp = CompareEffectiveRate(
			left.IsFamily,
			left.PlayFromHandCount,
			left.Denominator,
			right.IsFamily,
			right.PlayFromHandCount,
			right.Denominator);
		if (rateCmp != 0)
		{
			return -rateCmp;
		}

		int familyCmp = left.IsFamily.CompareTo(right.IsFamily);
		if (familyCmp != 0)
		{
			return -familyCmp;
		}

		if (left.IsFamily)
		{
			return CompareRankedDrawCardTieBreakers(left, right);
		}

		int playCmp = left.PlayFromHandCount.CompareTo(right.PlayFromHandCount);
		if (playCmp != 0)
		{
			return -playCmp;
		}

		return CompareRankedDrawCardTieBreakers(left, right);
	}

	private static int CompareRankedDrawCardTieBreakers(RankedDrawCard left, RankedDrawCard right)
	{
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

	private readonly record struct RankedDrawCard(
		CardModel Card,
		bool IsFamily,
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

		public Dictionary<CardModel, bool> CelebrateMourningExhaustResults { get; } =
			new(ReferenceEqualityComparer.Instance);

		public HashSet<CardModel> CelebrateMourningCards { get; } =
			new(ReferenceEqualityComparer.Instance);
	}

	[JsonConverter(typeof(PlayerSaveStateJsonConverter))]
	public sealed class PlayerSaveState
	{
		public List<CombatSnapshot> RecentCombats { get; set; } = [];
	}

	/// <summary>
	/// Stores each stable card identity only once and transposes its per-combat
	/// play/entry counts into a compact integer array. The reader also accepts the
	/// original v4 object graph, so an existing run keeps all of its history and is
	/// rewritten compactly after the next completed combat.
	/// </summary>
	public sealed class PlayerSaveStateJsonConverter : JsonConverter<PlayerSaveState>
	{
		private const string CombatCountProperty = "n";

		private const string HistoriesProperty = "h";

		private const string LegacyRecentCombatsProperty = "RecentCombats";

		public override PlayerSaveState Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			using JsonDocument document = JsonDocument.ParseValue(ref reader);
			JsonElement root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return new PlayerSaveState();
			}

			if (root.TryGetProperty(HistoriesProperty, out JsonElement historiesElement))
			{
				return ReadCompact(root, historiesElement);
			}

			if (root.TryGetProperty(LegacyRecentCombatsProperty, out JsonElement legacyElement))
			{
				List<CombatSnapshot>? legacy = JsonSerializer.Deserialize<List<CombatSnapshot>>(
					legacyElement.GetRawText(),
					options);
				return new PlayerSaveState
				{
					RecentCombats = CloneCombatList((legacy ?? []).TakeLast(MaxStoredCombats)),
				};
			}

			return new PlayerSaveState();
		}

		public override void Write(
			Utf8JsonWriter writer,
			PlayerSaveState value,
			JsonSerializerOptions options)
		{
			List<CombatSnapshot> recent = value.RecentCombats
				.TakeLast(MaxStoredCombats)
				.ToList();
			string[] keys = recent
				.SelectMany(snapshot => snapshot.Cards.Keys)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(key => key, StringComparer.Ordinal)
				.ToArray();

			writer.WriteStartObject();
			writer.WriteNumber(CombatCountProperty, recent.Count);
			writer.WritePropertyName(HistoriesProperty);
			writer.WriteStartObject();
			foreach (string key in keys)
			{
				writer.WritePropertyName(key);
				writer.WriteStartArray();
				foreach (CombatSnapshot snapshot in recent)
				{
					if (snapshot.Cards.TryGetValue(key, out CardCombatStats? stats))
					{
						writer.WriteNumberValue(stats.PlayFromHandCount);
						writer.WriteNumberValue(stats.HandEntryCount);
					}
					else
					{
						writer.WriteNumberValue(0);
						writer.WriteNumberValue(0);
					}
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
			writer.WriteEndObject();
		}

		private static PlayerSaveState ReadCompact(JsonElement root, JsonElement historiesElement)
		{
			if (historiesElement.ValueKind != JsonValueKind.Object)
			{
				return new PlayerSaveState();
			}

			int serializedCombatCount = root.TryGetProperty(CombatCountProperty, out JsonElement countElement)
				&& countElement.TryGetInt32(out int parsedCount)
					? Math.Max(0, parsedCount)
					: 0;
			int retainedCombatCount = Math.Min(serializedCombatCount, MaxStoredCombats);
			int skippedCombatCount = serializedCombatCount - retainedCombatCount;
			var recent = Enumerable.Range(0, retainedCombatCount)
				.Select(_ => new CombatSnapshot())
				.ToList();

			foreach (JsonProperty history in historiesElement.EnumerateObject())
			{
				if (history.Value.ValueKind != JsonValueKind.Array)
				{
					continue;
				}

				int[] counts = history.Value
					.EnumerateArray()
					.Select(element => element.TryGetInt32(out int count) ? count : 0)
					.ToArray();
				for (int retainedIndex = 0; retainedIndex < retainedCombatCount; retainedIndex++)
				{
					int serializedIndex = skippedCombatCount + retainedIndex;
					int countIndex = serializedIndex * 2;
					int plays = countIndex < counts.Length ? counts[countIndex] : 0;
					int entries = countIndex + 1 < counts.Length ? counts[countIndex + 1] : 0;
					if (plays == 0 && entries == 0)
					{
						continue;
					}

					recent[retainedIndex].Cards[history.Name] = new CardCombatStats
					{
						PlayFromHandCount = plays,
						HandEntryCount = entries,
					};
				}
			}

			return new PlayerSaveState { RecentCombats = recent };
		}
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
