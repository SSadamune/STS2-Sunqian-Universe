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
/// 西凉野人剧本：持有者打出的攻击牌会将本次消耗的活力量永久加到该牌基础伤害上（参考 THRASH）。
/// </summary>
[RegisterPower]
public sealed class ScriptXiliangSavagePower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public PendingAttack? Pending;
	}

	private sealed class PendingAttack
	{
		public required AttackCommand Command { get; init; }

		public required CardModel Card { get; init; }

		public required int VigorBefore { get; init; }
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

		if (AttackCardDamageRetain.TryAddBaseDamage(pending.Card, consumed))
		{
			Flash();
		}

		return Task.CompletedTask;
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
