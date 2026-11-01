using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Powers;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 《剧本：小黄门》激活期间，手牌中的虚无视为保留。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_ShouldRetainThisTurn")]
internal static class EunuchMessageRetainPatch
{
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (!__result && ScriptEunuchPower.TreatsEtherealAsRetain(__instance))
		{
			__result = true;
		}
	}
}

/// <summary>
/// 《剧本：小黄门》只在规则层把虚无视为保留；卡面仍应显示真实的“虚无”，
/// 而不应因为 <see cref="CardModel.ShouldRetainThisTurn"/> 被改写而额外显示“保留”。
/// 卡牌本来就具有的保留不在此处移除。
/// </summary>
[HarmonyPatch]
internal static class EunuchVirtualRetainDescriptionPatch
{
	private static MethodBase TargetMethod() =>
		AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(method =>
			method.Name == "GetDescriptionForPile"
			&& method.GetParameters().Length == 3);

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (string.IsNullOrEmpty(__result)
			|| __instance.Keywords.Contains(CardKeyword.Retain)
			|| !ScriptEunuchPower.TreatsEtherealAsRetain(__instance))
		{
			return;
		}

		HoverTip retainTip = (HoverTip)HoverTipFactory.FromKeyword(CardKeyword.Retain);
		string marker = $"[gold]{retainTip.Title}[/gold]";
		List<string> lines = __result.Split('\n').ToList();
		int virtualRetainLine = lines.FindIndex(line =>
			line.StartsWith(marker, StringComparison.Ordinal));
		if (virtualRetainLine >= 0)
		{
			lines.RemoveAt(virtualRetainLine);
			__result = string.Join('\n', lines);
		}
	}
}

/// <summary>
/// 小黄门赋予的虚无持续到该牌实际开始结算；普通移区不会清除它。
/// 只移除带内部标记的虚无，避免误删卡牌原本具有的虚无。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class EunuchMessageClearWhenPlayedPatch
{
	private static void Prefix(CardModel __instance)
	{
		ScriptEunuchPower.ClearTemporaryEtherealIfPresent(__instance);
	}
}

/// <summary>
/// 带有独立回合末效果的牌（例如《灼伤》）会走官方的特殊移区流程，绕开普通虚无与保留判断。
/// 保留其回合末效果，但暂时拿掉虚无，结算后将牌放回手中，再恢复虚无；随后官方手牌清理
/// 仍会把它计入 retainedCards，因此“被保留时”钩子也会正常触发。
/// </summary>
[HarmonyPatch(typeof(CombatManager), "ResolveTurnEndCardEffects")]
internal static class EunuchEtherealTurnEndEffectPatch
{
	private static void Prefix(CardModel card, out bool __state)
	{
		__state = card.HasTurnEndInHandEffect
			&& card.Keywords.Contains(CardKeyword.Ethereal)
			&& card.Owner?.Creature.GetPower<ScriptEunuchPower>() is not null;
		if (__state)
		{
			CardCmd.RemoveKeyword(card, CardKeyword.Ethereal);
		}
	}

	private static void Postfix(
		CardModel card,
		bool __state,
		ref Task<CardPileAddResult?> __result)
	{
		if (__state)
		{
			__result = RestoreEtherealAndReturnToHandAsync(__result, card);
		}
	}

	private static async Task<CardPileAddResult?> RestoreEtherealAndReturnToHandAsync(
		Task<CardPileAddResult?> resolution,
		CardModel card)
	{
		try
		{
			await resolution;
		}
		finally
		{
			if (!card.Keywords.Contains(CardKeyword.Ethereal))
			{
				CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
			}
		}

		if (card.Pile?.Type == PileType.Hand)
		{
			return null;
		}

		return await CardPileCmd.Add(
			card,
			PileType.Hand,
			CardPilePosition.Bottom,
			clonedBy: null,
			skipVisuals: true);
	}
}

/// <summary>
/// 隐藏内部标记的提示；虚无被视为保留时，展示保留提示而非误导性的虚无/消耗提示。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
[HarmonyPriority(int.MinValue)]
internal static class EunuchMessageRetainHoverTipPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		List<IHoverTip> tips = __result.ToList();
		if (__instance.HasEunuchMessage())
		{
			IHoverTip genericMessageTip = HoverTipFactory.FromKeyword(SquKeywords.EunuchMessage);
			tips.RemoveAll(tip => tip.Id == genericMessageTip.Id);
			tips.Add(SquKeywords.CreateEunuchMessageHoverTip(__instance));
		}

		if (ScriptEunuchPower.TreatsEtherealAsRetain(__instance))
		{
			tips.Remove(HoverTipFactory.FromKeyword(CardKeyword.Ethereal));
			tips.Remove(HoverTipFactory.FromKeyword(CardKeyword.Exhaust));
			IHoverTip retainTip = HoverTipFactory.FromKeyword(CardKeyword.Retain);
			if (!tips.Contains(retainTip))
			{
				tips.Add(retainTip);
			}
		}

		__result = tips;
	}
}
