#nullable enable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using Squ.Cards;
using Squ.Character;
using STS2RitsuLib.Data;

namespace Squ.Settings;

/// <summary>
/// 控制本模组注册的中立卡牌是否进入当前上下文的中立牌池候选。
/// 模型本身始终保留注册，以保证存档与按 ID 直接生成卡牌的兼容性。
/// </summary>
public static class NeutralCardRegistrationPolicy
{
	public static bool ShouldInclude()
	{
		NeutralContentPermissionMode mode = ModDataStore.For(SquMod.ModId)
			.Get<SquSettings>(SquSettings.DataKey)
			.NeutralCardRegistration;

		return mode switch
		{
			NeutralContentPermissionMode.Allow => true,
			NeutralContentPermissionMode.WhenModCharacterPresent => HasModCharacter(),
			_ => false,
		};
	}

	private static bool HasModCharacter()
	{
		IRunState? runState = RunManager.Instance.DebugOnlyGetState();
		return runState?.Players.Any(player => player.Character is SunqianCharacter) == true;
	}

	private static bool IsAffectedCard(CardModel card) =>
		card is TheGrievingPrevail or TheArrogantFall;

	[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
	private static class GetUnlockedCardsPatch
	{
		private static void Postfix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
		{
			if (__instance is ColorlessCardPool && !ShouldInclude())
			{
				__result = __result.Where(card => !IsAffectedCard(card));
			}
		}
	}
}
