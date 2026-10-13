#nullable enable
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Models.Capabilities;

namespace Squ.Combat;

/// <summary>为夜袭乌巢影响下的打击牌提供通用动态描述与灼烧预览。</summary>
public sealed class NightRaidWuchaoStrikeCapability : CardCapability, ICardDescriptionContributor, ICardHoverTipContributor
{
	public const string BurningVarName = "NightRaidBurning";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		ModCardVars.ComputedPowerAmountGiven<BurningPower>(
			BurningVarName,
			0m,
			static (card, _) => NightRaidWuchaoStrikeSystem.GetBaseBurning(card)),
	];

	public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
	{
		if (context.PileType is not (PileType.Hand or PileType.Play)
			|| NightRaidWuchaoStrikeSystem.GetBaseBurning(context.Card) <= 0m)
		{
			yield break;
		}

		yield return new CardDescriptionFragment(
			new LocString("cards", "SUNQIAN_UNIVERSE_NIGHT_RAID_WUCHAO_STRIKE.descriptionFragment"),
			CardDescriptionFragmentPlacement.AfterBase);
	}

	public IEnumerable<IHoverTip> GetHoverTips(CardModel card)
	{
		if (card.Pile?.Type is PileType.Hand or PileType.Play
			&& NightRaidWuchaoStrikeSystem.GetBaseBurning(card) > 0m)
		{
			yield return HoverTipFactory.FromPower<BurningPower>();
		}
	}
}
