using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Squ.Cards;

/// <summary>
/// 夜袭乌巢剧本悬停提示中展示的星夜打击；与实际生成牌相同，但预先显示消耗。
/// 不进入任何可获取卡池。
/// </summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "night_raid_wuchao_script_starry_night_strike_preview")]
public sealed class NightRaidWuchaoScriptStarryNightStrikePreview : StarryNightStrike
{
	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];
}
