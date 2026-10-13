using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// 《乱世害了你》不计入致死性的本回合攻击打出次数，避免占用「第一张攻击」而不吃到加成。
/// </summary>
[HarmonyPatch(typeof(LethalityPower), nameof(LethalityPower.ModifyDamageMultiplicative))]
internal static class ChaosHarmedYouLethalityPatch
{
	private static bool Prefix(
		LethalityPower __instance,
		ValueProp props,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!props.IsPoweredAttack() || cardSource == null || cardSource.Owner.Creature != __instance.Owner)
		{
			__result = 1m;
			return false;
		}

		CardPile? pile = cardSource.Pile;
		if (pile is { Type: PileType.Play } && cardSource.CurrentPlayIndex > 0)
		{
			__result = 1m;
			return false;
		}

		int attacksThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
			entry.HappenedThisTurn(__instance.CombatState)
			&& entry.CardPlay.Card.Type == CardType.Attack
			&& entry.CardPlay.Card is not ChaosHarmedYou
			&& entry.CardPlay.Player == __instance.Owner.Player);
		int currentPlayOffset = pile is { Type: PileType.Play } ? 1 : 0;
		if (attacksThisTurn > currentPlayOffset)
		{
			__result = 1m;
			return false;
		}

		__result = 1m + (decimal)__instance.Amount / 100m;
		return false;
	}
}
