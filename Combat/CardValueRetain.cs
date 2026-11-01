using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Permanently increases a card's printed combat values, mirroring
/// <see cref="MegaCrit.Sts2.Core.Models.Cards.Thrash"/> for damage.
/// </summary>
public static class CardValueRetain
{
	private static readonly FieldInfo? AttackDamagePerHitField =
		AccessTools.Field(typeof(AttackCommand), "_damagePerHit");

	private static readonly FieldInfo? AttackCalculatedDamageVarField =
		AccessTools.Field(typeof(AttackCommand), "_calculatedDamageVar");

	public static bool TryAddBaseDamage(CardModel card, decimal amount)
	{
		if (amount <= 0)
		{
			return false;
		}

		if (card.DynamicVars.ContainsKey("Damage"))
		{
			card.DynamicVars.Damage.BaseValue += amount;
			return true;
		}

		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			card.DynamicVars.CalculatedDamage.BaseValue += amount;
			return true;
		}

		if (card.DynamicVars.ContainsKey("OstyDamage"))
		{
			card.DynamicVars.OstyDamage.BaseValue += amount;
			return true;
		}

		Squ.SquMod.Logger?.Warn(
			$"Could not retain {amount} damage on {card.Id.Entry}: no recognized damage dynamic var.");
		return false;
	}

	public static bool TryAddBaseValue(CardModel card, string dynamicVarName, decimal amount)
	{
		if (amount <= 0m || !card.DynamicVars.ContainsKey(dynamicVarName))
		{
			return false;
		}

		card.DynamicVars[dynamicVarName].BaseValue += amount;
		return true;
	}

	/// <summary>
	/// Finds the damage variable that supplied an attack command. Later commands may already carry
	/// a snapshotted additive bonus in their raw amount, so callers provide those possible offsets.
	/// </summary>
	public static string? ResolveDamageVarName(
		CardModel card,
		AttackCommand command,
		params decimal[] possibleAdditiveOffsets)
	{
		List<KeyValuePair<string, DynamicVar>> damageVars = card.DynamicVars
			.Where(pair => pair.Value is DamageVar or CalculatedDamageVar)
			.ToList();
		if (damageVars.Count == 0)
		{
			return null;
		}

		if (damageVars.Count == 1)
		{
			return damageVars[0].Key;
		}

		if (AttackCalculatedDamageVarField?.GetValue(command) is CalculatedDamageVar calculated)
		{
			foreach (KeyValuePair<string, DynamicVar> pair in damageVars)
			{
				if (ReferenceEquals(pair.Value, calculated))
				{
					return pair.Key;
				}
			}
		}

		if (TryGetRawAttackDamage(command, out decimal rawDamage))
		{
			IEnumerable<decimal> offsets = possibleAdditiveOffsets
				.Append(0m)
				.Where(offset => offset >= 0m)
				.Distinct();
			foreach (decimal offset in offsets)
			{
				string? match = null;
				foreach (KeyValuePair<string, DynamicVar> pair in damageVars)
				{
					if (pair.Value.BaseValue != rawDamage - offset)
					{
						continue;
					}
					if (match is not null)
					{
						match = null;
						break; // Ambiguous offset: try the next one, preserving its priority.
					}
					match = pair.Key;
				}
				if (match is not null)
				{
					return match;
				}
			}
		}

		return damageVars.FirstOrDefault(pair => pair.Key == "Damage").Key
			?? damageVars[0].Key;
	}

	public static bool TryGetRawAttackDamage(AttackCommand command, out decimal damage)
	{
		if (AttackDamagePerHitField?.GetValue(command) is decimal rawDamage)
		{
			damage = rawDamage;
			return true;
		}

		damage = 0m;
		return false;
	}
}
