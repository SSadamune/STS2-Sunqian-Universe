using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Squ.Cards;

/// <summary>
/// 《酒桌文化》悬停中展示的《酒》。与实际给予的牌相同，为升级过的酒并预先显示执迷。
/// 不进入任何可获取卡池。
/// </summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "drinking_table_culture_wine_preview")]
public sealed class DrinkingTableCultureWinePreview : Wine
{
	public override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Enthralled,
	];
}
