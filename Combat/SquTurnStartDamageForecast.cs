using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Pure UI forecast for Poison and Burning damage sharing turn-start defensive resources.
/// This intentionally does not invoke HP-loss hooks or their effect callbacks.
/// </summary>
internal static class SquTurnStartDamageForecast
{
	internal readonly record struct Result(int PoisonDamage, int BurningDamage);

	private sealed class DefenseState(Creature target)
	{
		private decimal _hardenedShellRemaining =
			target.GetPower<HardenedShellPower>() is { Amount: > 0 } shell
				? shell.Amount
				: decimal.MaxValue;

		private int _slipperyRemaining =
			target.GetPower<SlipperyPower>() is { Amount: > 0 } slippery
				? slippery.Amount
				: 0;

		private readonly bool _hasHardenedShell =
			target.GetPower<HardenedShellPower>() is { Amount: > 0 };

		private readonly bool _hasIntangible =
			target.GetPower<IntangiblePower>() is { Amount: > 0 };

		private int _hpRemaining = target.CurrentHp;

		public bool IsAlive => _hpRemaining > 0;

		public int Apply(decimal damage)
		{
			damage = Math.Max(0m, damage);
			damage = Math.Min(damage, _hardenedShellRemaining);

			if (_hasIntangible || _slipperyRemaining > 0)
			{
				damage = Math.Min(damage, 1m);
			}

			int hpLost = Math.Min((int)Math.Min(damage, 999999999m), _hpRemaining);
			if (hpLost <= 0)
			{
				return 0;
			}

			_hpRemaining -= hpLost;
			if (_hasHardenedShell)
			{
				_hardenedShellRemaining = Math.Max(0m, _hardenedShellRemaining - hpLost);
			}

			if (_slipperyRemaining > 0)
			{
				_slipperyRemaining--;
			}

			return hpLost;
		}
	}

	public static Result Calculate(Creature target)
	{
		PoisonPower? poison = target.GetPower<PoisonPower>();
		BurningPower? burning = target.GetPower<BurningPower>();
		if (poison is null && burning is null)
		{
			return default;
		}

		var defense = new DefenseState(target);
		int poisonDamage = 0;
		int burningDamage = 0;

		// Combat hooks execute powers in this same list order. Simulating that order matters because
		// Hardened Shell and Slippery are shared/consumable defenses across both damage sources.
		foreach (PowerModel power in target.Powers)
		{
			if (!defense.IsAlive)
			{
				break;
			}

			if (ReferenceEquals(power, poison))
			{
				poisonDamage = CalculatePoisonDamage(target, poison!, defense);
			}
			else if (ReferenceEquals(power, burning) && burning!.Amount > 0)
			{
				burningDamage = defense.Apply(ModifyDamage(target, burning.Amount));
			}
		}

		return new Result(poisonDamage, burningDamage);
	}

	/// <summary>
	/// Preserves vanilla Poison's original forecast for mechanics such as Doom's kill threshold.
	/// </summary>
	public static int CalculateLegacyPoisonDamage(Creature target)
	{
		PoisonPower? poison = target.GetPower<PoisonPower>();
		if (poison is null)
		{
			return 0;
		}

		decimal total = 0m;
		int triggerCount = GetPoisonTriggerCount(target, poison);
		for (int i = 0; i < triggerCount; i++)
		{
			total += ModifyDamage(target, poison.Amount - i);
		}

		return (int)total;
	}

	private static int CalculatePoisonDamage(
		Creature target,
		PoisonPower poison,
		DefenseState defense)
	{
		int total = 0;
		int triggerCount = GetPoisonTriggerCount(target, poison);
		for (int i = 0; i < triggerCount && defense.IsAlive; i++)
		{
			total += defense.Apply(ModifyDamage(target, poison.Amount - i));
		}

		return total;
	}

	private static int GetPoisonTriggerCount(Creature target, PoisonPower poison)
	{
		int accelerantTriggers = target.CombatState?.GetOpponentsOf(target)
			.Where(static creature => creature.IsAlive)
			.Sum(static creature => creature.GetPowerAmount<AccelerantPower>()) ?? 0;
		return Math.Min(poison.Amount, 1 + accelerantTriggers);
	}

	private static decimal ModifyDamage(Creature target, decimal amount)
	{
		if (target.CombatState is not { } combatState)
		{
			return Math.Max(0m, amount);
		}

		return Hook.ModifyDamage(
			combatState.RunState,
			combatState,
			target,
			null,
			amount,
			ValueProp.Unblockable | ValueProp.Unpowered,
			null,
			null,
			ModifyDamageHookType.All,
			CardPreviewMode.None,
			out IEnumerable<AbstractModel> _);
	}
}
