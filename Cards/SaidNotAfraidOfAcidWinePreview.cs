using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Squ.Cards;

/// <summary>
/// 《说了不怕酸》悬停中展示的《酒》。与实际生成牌相同，但预先显示消耗。
/// 不进入任何可获取卡池。
/// </summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "said_not_afraid_of_acid_wine_preview")]
public sealed class SaidNotAfraidOfAcidWinePreview : Wine
{
	public override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];
}
