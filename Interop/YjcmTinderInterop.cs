using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// Optional Yjcm extension for Tinder. Its gameplay effect only requires the
/// public API, while its combat UI is shown only when the party contains a
/// Yjcm character.
/// </summary>
internal static class YjcmTinderInterop
{
	public static bool EnhancesHuoGong(PowerModel power) =>
		YjcmPublicApiInterop.IsReady
		&& YjcmPublicApiInterop.IsHuoGongPower(power);

	public static IEnumerable<IHoverTip> HuoGongHoverTips(PowerModel power)
	{
		JointModPresentationMode mode = PresentationMode;
		if (mode == JointModPresentationMode.HoverOnly)
		{
			yield return new HoverTip(
				SquCommonL10n.YjcmGoodFireHoverTitle(),
				SquCommonL10n.YjcmTinderLibraryHover());
			yield break;
		}

		if (mode == JointModPresentationMode.InlineKeywords
			&& YjcmPublicApiInterop.CreateHuoGongHoverTip() is { } huoGongTip)
		{
			yield return huoGongTip;
		}
	}

	public static void AddCombatDescriptionArgs(LocString description, PowerModel power) =>
		description.Add(
			"YjcmTinderHuoGongText",
			PresentationMode == JointModPresentationMode.InlineKeywords
				? SquCommonL10n.YjcmTinderInlineText().GetFormattedText()
				: string.Empty);

	private static JointModPresentationMode PresentationMode =>
		JointModPresentation.Resolve(
			YjcmPublicApiInterop.IsReady,
			YjcmPublicApiInterop.IsYjcmCharacter);

}
