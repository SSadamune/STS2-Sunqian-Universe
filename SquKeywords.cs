using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;

#nullable enable

namespace Squ;

/// <summary>
/// Mod 悬停关键词「剧本」的 id 常量（注册见 SquMod.ModLoaded）。
/// </summary>
public static class SquKeywords
{
	public static readonly string ScriptId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "script");

	public static readonly CardKeyword Script = ScriptId.GetModCardKeyword();

	public static readonly string DoomKillThresholdId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "doom_kill_threshold");

	public static readonly CardKeyword DoomKillThreshold = DoomKillThresholdId.GetModCardKeyword();

	public static readonly string StackableScriptId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "stackable_script");

	public static readonly CardKeyword StackableScript = StackableScriptId.GetModCardKeyword();

	public static readonly string MultiTargetId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "multi_target");

	public static readonly CardKeyword MultiTarget = MultiTargetId.GetModCardKeyword();

	public static readonly string EnvironmentalId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "environmental");

	public static readonly CardKeyword Environmental = EnvironmentalId.GetModCardKeyword();

	public static readonly string ScryId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "scry");

	public static readonly CardKeyword Scry = ScryId.GetModCardKeyword();

	public static readonly string FitId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "fit");

	public static readonly CardKeyword Fit = FitId.GetModCardKeyword();

	public static readonly string CountsAsPlayedId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "counts_as_played");

	public static readonly CardKeyword CountsAsPlayed = CountsAsPlayedId.GetModCardKeyword();

	public static readonly string EunuchMessageId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "eunuch_message");

	public static readonly CardKeyword EunuchMessage = EunuchMessageId.GetModCardKeyword();

	public static readonly string WarFeedsWarId = ModContentRegistry
		.GetQualifiedKeywordId(SquMod.ModId, "war_feeds_war");

	public static readonly CardKeyword WarFeedsWar = WarFeedsWarId.GetModCardKeyword();

	public static IHoverTip CreateWarFeedsWarHoverTip(CardModel card)
	{
		LocString title = ModKeywordRegistry.GetTitle(WarFeedsWarId);
		LocString description = ModKeywordRegistry.GetDescription(WarFeedsWarId);
		description.Add("CardName", card.Title);
		return new HoverTip(title, description, icon: null);
	}

	/// <summary>名字/关键词中带有[gold]预见[/gold]的牌。</summary>
	public static bool IsScry(this CardModel card) => card.Keywords.Contains(Scry);

	public static bool HasEunuchMessage(this CardModel card) => card.Keywords.Contains(EunuchMessage);
}
