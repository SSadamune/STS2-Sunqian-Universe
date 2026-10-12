#nullable enable

using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Squ.Interop;

/// <summary>
/// Optional newsanguo model lookups used by Metamorphosis. All access is gated by
/// the public API interop so this assembly never takes a hard dependency on newsanguo.dll.
/// </summary>
internal static class NewsanguoMetamorphosisInterop
{
	private const string CaoWeiCharacterEntry = "NEWSANGUO_CHARACTER_CAO_WEI_CHARACTER";
	private const string ShuHanCharacterEntry = "NEWSANGUO_CHARACTER_SHU_HAN_CHARACTER";

	public const string AlwaysMineEntry = "NEWSANGUO_CARD_ALWAYS_MINE";
	public const string UnclesAndAuntsEntry = "NEWSANGUO_CARD_UNCLES_AND_AUNTS";
	public const string LetsDiscussEntry = "NEWSANGUO_CARD_LETS_DISCUSS";
	public const string RetireEntry = "NEWSANGUO_CARD_RETIRE";
	public const string BetterEachDayEntry = "NEWSANGUO_CARD_BETTER_EACH_DAY";
	public const string MaClanQuadBlastEntry = "NEWSANGUO_CARD_MA_CLAN_QUAD_BLAST";
	public const string MyThreeGeneralsEntry = "NEWSANGUO_CARD_MY_THREE_GENERALS";
	public const string HeavenAndEarthEntry = "NEWSANGUO_CARD_HEAVEN_AND_EARTH";
	public const string TenThousandTransparentHolesEntry =
		"NEWSANGUO_CARD_TEN_THOUSAND_TRANSPARENT_HOLES";
	public const string AutoPilotEntry = "NEWSANGUO_CARD_AUTO_PILOT";
	public const string ReleaseEntry = "NEWSANGUO_CARD_RELEASE";
	public const string WheresWineEntry = "NEWSANGUO_CARD_WHERES_WINE";

	private static readonly string[] CaoWeiCandidateEntries =
	[
		AlwaysMineEntry,
		UnclesAndAuntsEntry,
		LetsDiscussEntry,
		RetireEntry,
		BetterEachDayEntry,
		MaClanQuadBlastEntry,
		MyThreeGeneralsEntry,
	];

	private static readonly string[] ShuHanCandidateEntries =
	[
		HeavenAndEarthEntry,
		TenThousandTransparentHolesEntry,
		AutoPilotEntry,
		ReleaseEntry,
		MaClanQuadBlastEntry,
		WheresWineEntry,
	];

	public static IReadOnlyList<CardModel> GetCaoWeiCandidateCards()
	{
		if (!NewsanguoPublicApiInterop.IsReady)
		{
			return [];
		}

		return GetCandidateCards(CaoWeiCandidateEntries);
	}

	public static IReadOnlyList<CardModel> GetShuHanCandidateCards() =>
		NewsanguoPublicApiInterop.IsReady
			? GetCandidateCards(ShuHanCandidateEntries)
			: [];

	public static CardPoolModel? GetCaoWeiCardPool() =>
		GetCaoWeiCharacterOrNull()?.CardPool;

	public static CardPoolModel? GetShuHanCardPool() =>
		GetShuHanCharacterOrNull()?.CardPool;

	public static bool IsCaoWeiCardPool(CardPoolModel pool) =>
		GetCaoWeiCardPool() is { } caoWeiPool && caoWeiPool.Id == pool.Id;

	public static bool IsShuHanCardPool(CardPoolModel pool) =>
		GetShuHanCardPool() is { } shuHanPool && shuHanPool.Id == pool.Id;

	public static string? GetCaoWeiDisplayName() =>
		GetCaoWeiCharacterOrNull()?.Title.GetFormattedText();

	public static string? GetShuHanDisplayName() =>
		GetShuHanCharacterOrNull()?.Title.GetFormattedText();

	public static bool IsCard(CardModel card, string entry) =>
		NewsanguoPublicApiInterop.IsReady
		&& card.Id == new ModelId("CARD", entry);

	public static int GetDrunkenMight(Player? player) =>
		NewsanguoPublicApiInterop.IsReady
			? NewsanguoPublicApiInterop.GetDrunkenMight(player)
			: 0;

	private static CardModel? GetCardOrNull(string entry) =>
		NewsanguoPublicApiInterop.IsReady
			? ModelDb.GetByIdOrNull<CardModel>(new ModelId("CARD", entry))
			: null;

	private static CharacterModel? GetCaoWeiCharacterOrNull() =>
		GetCharacterOrNull(CaoWeiCharacterEntry);

	private static CharacterModel? GetShuHanCharacterOrNull() =>
		GetCharacterOrNull(ShuHanCharacterEntry);

	private static CharacterModel? GetCharacterOrNull(string entry) =>
		NewsanguoPublicApiInterop.IsReady
			? ModelDb.GetByIdOrNull<CharacterModel>(new ModelId("CHARACTER", entry))
			: null;

	private static IReadOnlyList<CardModel> GetCandidateCards(IEnumerable<string> entries) =>
		entries
			.Select(GetCardOrNull)
			.OfType<CardModel>()
			.ToList();
}
