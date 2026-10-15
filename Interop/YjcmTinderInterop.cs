using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
		if (ShowsCombatUi(power)
			&& YjcmPublicApiInterop.CreateHuoGongHoverTip() is { } huoGongTip)
		{
			yield return huoGongTip;
		}
	}

	public static void AddCombatDescriptionArgs(LocString description, PowerModel power) =>
		description.Add(
			"YjcmTinderHuoGongText",
			ShowsCombatUi(power)
				? SquCommonL10n.YjcmTinderInlineText().GetFormattedText()
				: string.Empty);

	private static bool ShowsCombatUi(PowerModel power)
	{
		if (!YjcmPublicApiInterop.IsReady
			|| CombatManager.Instance is not { IsInProgress: true }
			|| MutableOwner(power) is not { CombatState: { } combatState })
		{
			return false;
		}

		return combatState.Players.Any(player =>
			YjcmPublicApiInterop.IsYjcmCharacter(player.Character));
	}

	private static Creature? MutableOwner(PowerModel power) =>
		power.IsMutable ? power.Owner : null;
}
