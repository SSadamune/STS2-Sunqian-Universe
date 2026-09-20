using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 西凉野人剧本：持有者的牌会永久保留本次活力带来的数值加成。
/// 攻击伤害在攻击结束后立刻写回；格挡、灼烧等在本次打出中才读取的加成
/// 延迟到整张牌结算后写回，避免当前这次打出被重复计算。
/// </summary>
[RegisterPower]
public sealed class ScriptXiliangSavagePower : ScriptPowerTemplate
{
	private const string BlockVarName = "Block";
	private static readonly string BurningVarName = nameof(BurningPower);

	private sealed class Data
	{
		public PendingAttack? Pending;

		public Dictionary<CardModel, PendingCardBonuses> PendingCardBonuses { get; } = [];
	}

	private sealed class PendingAttack
	{
		public required AttackCommand Command { get; init; }

		public required CardModel Card { get; init; }

		public required int VigorBefore { get; init; }
	}

	private sealed class PendingCardBonuses
	{
		public decimal Block;

		public decimal Burning;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptXiliangSavagePower.png",
		BigIconPath: "res://images/powers/ScriptXiliangSavagePowerBig.png");

	protected override object InitInternalData() => new Data();

	public override Task BeforeAttack(AttackCommand command)
	{
		if (Owner.IsDead || !TryGetQualifyingAttackCard(command, out CardModel? card))
		{
			return Task.CompletedTask;
		}

		VigorPower? vigor = Owner.GetPower<VigorPower>();
		if (vigor is not { Amount: > 0 } || !command.DamageProps.IsPoweredAttack())
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().Pending = new PendingAttack
		{
			Command = command,
			Card = card,
			VigorBefore = vigor.Amount,
		};

		return Task.CompletedTask;
	}

	public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		Data data = GetInternalData<Data>();
		PendingAttack? pending = data.Pending;
		if (pending is null || pending.Command != command)
		{
			return Task.CompletedTask;
		}

		data.Pending = null;

		// Do not measure consumed vigor by reading stacks after the attack:
		// Hook.AfterAttack listener order is not guaranteed. If this power runs
		// before VigorPower, the difference is still 0 even though vigor will be spent.
		// Vigor always removes its full pre-attack amount on powered attacks, so
		// retain that snapshot (mirrors VigorPower.amountWhenAttackStarted).
		int consumed = pending.VigorBefore;
		if (consumed <= 0)
		{
			return Task.CompletedTask;
		}

		bool retained = CardValueRetain.TryAddBaseDamage(pending.Card, consumed);
		if (pending.Card.DynamicVars.ContainsKey(BurningVarName))
		{
			QueueCardBonus(pending.Card, burning: consumed);
			retained = true;
		}

		if (retained)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power is not VigorPower
			|| power.Owner != Owner
			|| amount >= 0m
			|| cardSource is null
			|| cardSource.Owner.Creature != Owner
			|| !cardSource.DynamicVars.ContainsKey(BlockVarName))
		{
			return Task.CompletedTask;
		}

		QueueCardBonus(cardSource, block: -amount);
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.PlayIndex != cardPlay.PlayCount - 1
			|| !GetInternalData<Data>().PendingCardBonuses.Remove(
				cardPlay.Card,
				out PendingCardBonuses? bonuses))
		{
			return Task.CompletedTask;
		}

		bool retained = false;
		if (bonuses.Block > 0m)
		{
			retained |= CardValueRetain.TryAddBaseValue(
				cardPlay.Card,
				BlockVarName,
				bonuses.Block);
		}

		if (bonuses.Burning > 0m)
		{
			retained |= CardValueRetain.TryAddBaseValue(
				cardPlay.Card,
				BurningVarName,
				bonuses.Burning);
		}

		if (retained)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private void QueueCardBonus(
		CardModel card,
		decimal block = 0m,
		decimal burning = 0m)
	{
		Data data = GetInternalData<Data>();
		if (!data.PendingCardBonuses.TryGetValue(card, out PendingCardBonuses? bonuses))
		{
			bonuses = new PendingCardBonuses();
			data.PendingCardBonuses.Add(card, bonuses);
		}

		bonuses.Block += block;
		bonuses.Burning += burning;
	}

	private bool TryGetQualifyingAttackCard(AttackCommand command, out CardModel card)
	{
		card = null!;
		if (command.Attacker != Owner)
		{
			return false;
		}

		if (command.ModelSource is not CardModel cardSource || cardSource.Type != CardType.Attack)
		{
			return false;
		}

		if (cardSource.Owner.Creature != Owner)
		{
			return false;
		}

		card = cardSource;
		return true;
	}
}
