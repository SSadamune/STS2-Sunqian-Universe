using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// 《上方谷之叹》与“一将成名”的联动：威董卓和武诸葛获得的“好火啊…”
/// 同时提高其给予的火攻层数。
/// </summary>
internal static class YjcmGoodFireInterop
{
	public static bool IsModReady => YjcmPublicApiInterop.IsReady;

	public static bool Affects(Player? player) =>
		IsModReady && YjcmPublicApiInterop.IsYjcmCharacter(player?.Character);

	public static bool EnhancesHuoGong(Player? player, PowerModel power) =>
		Affects(player) && YjcmPublicApiInterop.IsHuoGongPower(power);

	public static IEnumerable<IHoverTip> HuoGongHoverTips(CardModel card)
	{
		if (ShowsInlineKeywords()
			&& YjcmPublicApiInterop.CreateHuoGongHoverTip() is { } huoGongTip)
		{
			yield return huoGongTip;
		}
	}

	public static IEnumerable<IHoverTip> HuoGongHoverTips(PowerModel power)
	{
		if (ShowsInlineKeywords()
			&& YjcmPublicApiInterop.CreateHuoGongHoverTip() is { } huoGongTip)
		{
			yield return huoGongTip;
		}
	}

	public static IEnumerable<IHoverTip> LibraryHoverTips()
	{
		if (PresentationMode != JointModPresentationMode.HoverOnly)
		{
			yield break;
		}

		yield return new HoverTip(
			SquCommonL10n.YjcmGoodFireHoverTitle(),
			SquCommonL10n.YjcmGoodFireLibraryHover());
	}

	public static void AddCombatCondition(LocString description, CardModel card) =>
		description.Add(
			"YjcmHuoGongText",
			ShowsInlineKeywords()
				? SquCommonL10n.YjcmGoodFireInlineText().GetFormattedText()
				: string.Empty);

	public static void AddCombatCondition(LocString description, PowerModel power) =>
		description.Add(
			"YjcmHuoGongText",
			ShowsInlineKeywords()
				? SquCommonL10n.YjcmGoodFireInlineText().GetFormattedText()
				: string.Empty);

	private static JointModPresentationMode PresentationMode =>
		JointModPresentation.Resolve(
			IsModReady,
			YjcmPublicApiInterop.IsYjcmCharacter);

	private static bool ShowsInlineKeywords() =>
		PresentationMode == JointModPresentationMode.InlineKeywords;
}
