using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Prevents an auto-played Attack from consuming Vigor when Supreme General already grants it the
/// root card's spent Vigor bonus. Also exposes the shared cleanup needed after restoring Vigor.
/// </summary>
public static class AttackVigorResolution
{
	private static readonly Type? VigorInternalDataType =
		AccessTools.Inner(typeof(VigorPower), "Data");

	private static readonly MethodInfo? GetInternalDataMethod =
		VigorInternalDataType is null
			? null
			: AccessTools.Method(typeof(PowerModel), "GetInternalData", Type.EmptyTypes)
				?.MakeGenericMethod(VigorInternalDataType);

	private static readonly FieldInfo? CommandToModifyField =
		VigorInternalDataType is null
			? null
			: AccessTools.Field(VigorInternalDataType, "commandToModify");

	private static readonly FieldInfo? AmountWhenAttackStartedField =
		VigorInternalDataType is null
			? null
			: AccessTools.Field(VigorInternalDataType, "amountWhenAttackStarted");

	/// <summary>
	/// Clears vanilla Vigor's binding to the previous Attack after Vigor is restored, allowing the
	/// restored amount to empower the next Attack normally.
	/// </summary>
	public static void ClearVigorAttackBinding(VigorPower vigor)
	{
		if (!TryGetVigorData(vigor, out object? data))
		{
			return;
		}

		CommandToModifyField!.SetValue(data, null);
		AmountWhenAttackStartedField!.SetValue(data, 0);
	}

	private static bool TrySuppressInheritedVigorConsumption(
		VigorPower vigor,
		AttackCommand command)
	{
		if (command.ModelSource is not CardModel card
			|| !SupremeGeneralKeywordSystem.TryGetInheritedVigorBonus(
				card,
				command.CardPlay,
				out _)
			|| !TryGetVigorData(vigor, out object? data)
			|| CommandToModifyField!.GetValue(data) != command)
		{
			return false;
		}

		CommandToModifyField.SetValue(data, null);
		AmountWhenAttackStartedField!.SetValue(data, 0);
		return true;
	}

	private static bool TryGetVigorData(VigorPower vigor, out object? data)
	{
		data = null;
		if (GetInternalDataMethod is null
			|| CommandToModifyField is null
			|| AmountWhenAttackStartedField is null)
		{
			return false;
		}

		data = GetInternalDataMethod.Invoke(vigor, null);
		return data is not null;
	}

	[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.AfterAttack))]
	private static class VigorAfterAttackPatch
	{
		private static bool Prefix(VigorPower __instance, AttackCommand command) =>
			!TrySuppressInheritedVigorConsumption(__instance, command);
	}
}
