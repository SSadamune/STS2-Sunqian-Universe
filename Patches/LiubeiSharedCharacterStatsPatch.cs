#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using Squ.Character;

namespace Squ.Patches;

/// <summary>
/// 刘备与孙乾是同一选角标签下的两个版本，因此共用进阶难度和角色统计。
/// 角色模型本身仍保持独立，以便各自拥有不同的卡池、初始卡组和初始遗物。
/// </summary>
[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.GetOrCreateCharacterStats))]
internal static class LiubeiSharedCharacterStatsPatch
{
	[HarmonyPrefix]
	private static void UseSunqianStatsForLiubei(ref ModelId characterId)
	{
		if (characterId == ModelDb.GetId<LiubeiCharacter>())
		{
			characterId = ModelDb.GetId<SunqianCharacter>();
		}
	}
}
