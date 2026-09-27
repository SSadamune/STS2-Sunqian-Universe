using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Defines a reusable scope for Attack cards that resolve other Attack cards inside their own
/// resolution. Nested Attacks neither consume Vigor nor count against Keep Vigor while the scope
/// is active; the root Attack retains responsibility for both resources.
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

	private static readonly Dictionary<ulong, int> SuppressionDepthByPlayer = [];

	/// <summary>
	/// Suppresses Vigor and Keep Vigor consumption by Attack cards resolved inside this scope.
	/// Scopes can be nested, so future cards may compose this helper safely.
	/// </summary>
	public static IDisposable SuppressNestedAttackConsumption(Player player)
	{
		ulong playerId = player.NetId;
		SuppressionDepthByPlayer.TryGetValue(playerId, out int depth);
		SuppressionDepthByPlayer[playerId] = depth + 1;
		return new SuppressionScope(playerId);
	}

	public static bool IsNestedAttackConsumptionSuppressed(Player? player) =>
		player is not null
		&& SuppressionDepthByPlayer.TryGetValue(player.NetId, out int depth)
		&& depth > 0;

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

	private static bool TrySuppressVigorConsumption(VigorPower vigor, AttackCommand command)
	{
		if (!IsNestedAttackConsumptionSuppressed(vigor.Owner.Player)
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

	private sealed class SuppressionScope(ulong playerId) : IDisposable
	{
		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			if (!SuppressionDepthByPlayer.TryGetValue(playerId, out int depth) || depth <= 1)
			{
				SuppressionDepthByPlayer.Remove(playerId);
				return;
			}

			SuppressionDepthByPlayer[playerId] = depth - 1;
		}
	}

	[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.AfterAttack))]
	private static class VigorAfterAttackPatch
	{
		private static bool Prefix(VigorPower __instance, AttackCommand command) =>
			!TrySuppressVigorConsumption(__instance, command);
	}
}
