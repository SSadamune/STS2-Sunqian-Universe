using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Squ.Powers;
using Squ.Relics;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 先古之民把初始遗物换成丰盛盒饭时，选项悬停只保留升级后的遗物与能力，
/// 并把相对盒饭新增的词标成绿色。遗物栏和战斗内的正式描述保持原色。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "get_ExtraHoverTips")]
internal static class AbundantBoxLunchOrobasHoverPatch
{
	private const string PreviewSuffix = ".orobasHoverPreview";

	private static void Postfix(TouchOfOrobas __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (__instance.UpgradedRelic != ModelDb.Relic<AbundantBoxLunchRelic>().Id)
		{
			return;
		}

		string sentryId = ModelDb.Power<ScriptSentryPower>().Id.ToString();
		string viceGeneralId = ModelDb.Power<ScriptViceGeneralPower>().Id.ToString();
		List<IHoverTip> tips = __result.ToList();
		int upgradedIndex = tips.FindIndex(static tip =>
			tip is HoverTip hover && hover.CanonicalModel is AbundantBoxLunchRelic);
		if (upgradedIndex > 0)
		{
			tips.RemoveRange(0, upgradedIndex);
		}

		tips.RemoveAll(tip =>
			tip is HoverTip hover
			&& (hover.CanonicalModel is BoxLunchRelic || hover.Id == sentryId));

		for (int i = 0; i < tips.Count; i++)
		{
			if (tips[i] is not HoverTip hover)
			{
				continue;
			}

			if (hover.CanonicalModel is AbundantBoxLunchRelic)
			{
				tips[i] = CreateRelicPreview(hover);
			}
			else if (hover.Id == viceGeneralId)
			{
				tips[i] = CreatePowerPreview(hover, viceGeneralId);
			}
		}

		__result = tips;
	}

	private static HoverTip CreateRelicPreview(HoverTip original)
	{
		RelicModel relic = ModelDb.Relic<AbundantBoxLunchRelic>();
		LocString description = new("relics", relic.Id.Entry + PreviewSuffix);
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(relic));
		HoverTip preview = new(relic.Title, description.GetFormattedText(), original.Icon);
		if (original.CanonicalModel is not null)
		{
			preview.SetCanonicalModel(original.CanonicalModel);
		}

		preview.Id = original.Id;
		return preview;
	}

	private static HoverTip CreatePowerPreview(HoverTip original, string viceGeneralId)
	{
		PowerModel power = ModelDb.Power<ScriptViceGeneralPower>();
		LocString description = new("powers", power.Id.Entry + PreviewSuffix);
		HoverTip preview = new(power, description.GetFormattedText(), original.IsSmart)
		{
			Id = viceGeneralId,
		};
		return preview;
	}
}
