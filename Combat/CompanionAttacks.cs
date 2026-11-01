#nullable enable
using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Squ.Combat;

public static class CompanionAttacks
{
	// The public FromOsty builder rejects other monsters. Preserve the native card
	// attack pipeline and change only its dealer to the requested companion.
	private static readonly Action<AttackCommand, Creature> SetAttacker =
		AccessTools.MethodDelegate<Action<AttackCommand, Creature>>(
			AccessTools.PropertySetter(typeof(AttackCommand), nameof(AttackCommand.Attacker)));
	public static AttackCommand FromCompanion(this AttackCommand attack, Creature pet,
		CardModel card, CardPlay play)
	{
		attack.FromCard(card, play);
		SetAttacker(attack, pet);
		return attack.WithAttackerAnim("Attack", 0, pet);
	}
}

public sealed class CompanionDamageVar : DynamicVar
{
	private readonly string _companionId;
	public CompanionDamageVar(string id, decimal amount) : base("CompanionDamage", amount) => _companionId = id;
	public override void UpdateCardPreview(CardModel card, CardPreviewMode mode,
		Creature? target, bool runGlobalHooks)
	{
		PreviewValue = runGlobalHooks && card.Owner != null
			? Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target,
				CompanionSummons.GetCompanion(card.Owner, _companionId), BaseValue, ValueProp.Move,
				card, null, ModifyDamageHookType.All, mode, out _)
			: BaseValue;
	}
}
