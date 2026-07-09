using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;

namespace Squ.Patches;

/// <summary>
/// 能力悬停经 <see cref="PowerModel.HoverTips"/> 构建文案；在读取前刷新受敏捷影响的格挡预览。
/// </summary>
[HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
internal static class CombatBlockPreviewPatch
{
	private static void Prefix(PowerModel __instance)
	{
		if (__instance is BuddyTangxiaohuPower buddy)
		{
			buddy.RefreshBlockPreviewForHover();
		}
	}
}
