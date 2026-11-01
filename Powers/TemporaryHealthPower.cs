#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// CurrentHp includes these stacks; CurrentHp - Amount is real HP.
/// Gain with PowerCmd.Apply. Loss and decay use CreatureCmd.Damage so native
/// mitigation and HP-loss hooks run once, before stacks are consumed.
/// </summary>
[RegisterPower]
public sealed class TemporaryHealthPower : ModPowerTemplate
{
	private bool _changingStacks;
	private bool _limitNextLoss;
	public const int TurnStartLoss = 6;
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TemporaryHealthPower.png",
		BigIconPath: "res://images/powers/TemporaryHealthPowerBig.png");

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
		Creature? applier, CardModel? cardSource)
	{
		if (power != this || amount == 0m)
		{
			return;
		}

		if (amount > 0m)
		{
			await CreatureCmd.SetCurrentHp(Owner,
				(decimal)Owner.CurrentHp + Math.Min(Amount, amount));
		}
		else
		{
			// A requested stack decrease is also HP loss: restore the stacks first,
			// then let defenses determine the amount actually lost.
			SetAmount((int)Math.Min(999999999m, Amount - amount));
			_changingStacks = true;
			try
			{
				await LoseTemporaryHealth(choiceContext, -amount);
			}
			finally
			{
				_changingStacks = false;
			}
		}
	}

	public override async Task BeforeSideTurnStart(
		PlayerChoiceContext choiceContext, CombatSide side,
		IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (side == Owner.Side && participants.Contains(Owner) && Owner.IsAlive && Amount > 0)
		{
			await LoseTemporaryHealth(choiceContext, TurnStartLoss);
		}
	}

	/// <summary>Lose at most the remaining temporary HP, subject to native defenses.</summary>
	public async Task LoseTemporaryHealth(PlayerChoiceContext choiceContext, decimal amount)
	{
		if (Amount <= 0 || amount <= 0m || Owner.IsDead)
		{
			return;
		}
		_limitNextLoss = true;
		try
		{
			await CreatureCmd.Damage(choiceContext, Owner, Math.Min(amount, Amount),
				ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		}
		finally
		{
			_limitNextLoss = false;
		}
	}

	internal void LimitTemporaryLoss(ref decimal amount)
	{
		if (_limitNextLoss)
		{
			_limitNextLoss = false;
			amount = Math.Min(amount, Amount);
		}
	}

	// Called synchronously at the actual HP mutation, after native mitigation.
	// Do not use PowerCmd.ModifyAmount here: that would apply defenses twice.
	internal void Consume(int hpLost)
	{
		if (hpLost <= 0 || Amount <= 0)
		{
			return;
		}
		SetAmount(Math.Max(0, Amount - hpLost));
		if (Amount == 0 && !_changingStacks)
		{
			RemoveInternal();
		}
	}
}
