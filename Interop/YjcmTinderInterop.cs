using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// Optional Yjcm extension for Tinder. Its gameplay effect and combat UI are
/// enabled whenever the public API is available.
/// </summary>
internal static class YjcmTinderInterop
{
	public static bool EnhancesHuoGong(PowerModel power) =>
		YjcmPublicApiInterop.IsReady
		&& YjcmPublicApiInterop.IsHuoGongPower(power);

	public static IEnumerable<IHoverTip> HuoGongHoverTips()
	{
		if (YjcmPublicApiInterop.IsReady
			&& YjcmPublicApiInterop.CreateHuoGongHoverTip() is { } huoGongTip)
		{
			yield return huoGongTip;
		}
	}

	public static void AddCombatDescriptionArgs(LocString description) =>
		description.Add(
			"YjcmTinderHuoGongText",
			YjcmPublicApiInterop.IsReady
				? SquCommonL10n.YjcmTinderInlineText().GetFormattedText()
				: string.Empty);

}
