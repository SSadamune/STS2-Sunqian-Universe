using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 升级预览右侧：飞火流星+ 费用由 1 变为 X 时，将左上角 X 标为与其它升级 diff 相同的绿色。
/// 仅作用于 <see cref="CardPreviewMode.Upgrade"/>，不影响战斗手牌。
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
internal static class FireNovaUpgradePreviewXCostColorPatch
{
	private static readonly FieldInfo EnergyLabelField =
		AccessTools.Field(typeof(NCard), "_energyLabel")
		?? throw new InvalidOperationException("NCard._energyLabel is missing.");

	private static void Postfix(NCard __instance, PileType pileType, CardPreviewMode previewMode)
	{
		if (previewMode != CardPreviewMode.Upgrade)
		{
			return;
		}

		CardModel? model = __instance.Model;
		if (model is not FireNova || model.EnergyCost is not { CostsX: true })
		{
			return;
		}

		MegaLabel energyLabel = (MegaLabel)EnergyLabelField.GetValue(__instance)!;
		((Control)energyLabel).AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.green);
		((Control)energyLabel).AddThemeColorOverride(
			ThemeConstants.Label.FontOutlineColor,
			StsColors.energyGreenOutline);
	}
}
