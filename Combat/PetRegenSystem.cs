#nullable enable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Squ.Combat;

/// <summary>
/// 让宠物身上的原版再生在其主人回合结束时按原版流程结算。
/// </summary>
[HarmonyPatch(typeof(RegenPower), nameof(RegenPower.BeforeSideTurnEndEarly))]
public static class PetRegenSystem
{
	private static void Prefix(
		RegenPower __instance,
		[HarmonyArgument("participants")] ref IEnumerable<Creature> participants)
	{
		Creature pet = __instance.Owner;
		if (pet.PetOwner is not { } petOwner
			|| participants.Contains(pet)
			|| !participants.Contains(petOwner.Creature))
		{
			return;
		}

		participants = participants.Append(pet);
	}
}
