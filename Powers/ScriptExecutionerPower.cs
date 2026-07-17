using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 刽子手剧本：持有者的攻击牌击杀敌人后，恢复该攻击消耗的活力；升级后同时恢复其消耗的能量。
/// </summary>
[RegisterPower]
public sealed class ScriptExecutionerPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public bool RestoreEnergy;

		public PendingAttack? Pending;

		public Dictionary<CardModel, AttackPlayTrack> ActivePlays { get; } = [];
	}

	private sealed class PendingAttack
	{
		public required AttackCommand Command { get; init; }

		public required CardModel Card { get; init; }

		public int VigorToRestore { get; init; }
	}

	private sealed class AttackPlayTrack
	{
		public int EnergySpent { get; init; }

		public bool EnergyRestored { get; set; }
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptExecutionerPower.png",
		BigIconPath: "res://images/powers/ScriptExecutionerPowerBig.png");

	protected override object InitInternalData() => new Data();

	protected override string SmartDescriptionLocKey =>
		GetInternalData<Data>().RestoreEnergy
			? base.Id.Entry + ".smartDescriptionUpgraded"
			: base.Id.Entry + ".smartDescription";

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		GetInternalData<Data>().RestoreEnergy = cardSource is ExecutionerScript { IsUpgraded: true };
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (Owner.IsDead || cardPlay.Card.Type != CardType.Attack || cardPlay.PlayIndex != 0)
		{
			return Task.CompletedTask;
		}

		if (cardPlay.Card.Owner.Creature != Owner)
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().ActivePlays[cardPlay.Card] = new AttackPlayTrack
		{
			EnergySpent = cardPlay.Resources.EnergySpent,
		};

		return Task.CompletedTask;
	}

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
			VigorToRestore = vigor.Amount,
		};

		return Task.CompletedTask;
	}

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		Data data = GetInternalData<Data>();
		PendingAttack? pending = data.Pending;
		if (pending is null || pending.Command != command)
		{
			return;
		}

		data.Pending = null;

		if (Owner.IsDead)
		{
			return;
		}

		bool killed = command.Results
			.SelectMany(results => results)
			.Any(result => result.WasTargetKilled);
		if (!killed)
		{
			return;
		}

		bool restored = false;

		if (pending.VigorToRestore > 0)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				pending.VigorToRestore,
				Owner,
				pending.Card);
			restored = true;
		}

		if (data.RestoreEnergy
			&& data.ActivePlays.TryGetValue(pending.Card, out AttackPlayTrack? track)
			&& !track.EnergyRestored
			&& track.EnergySpent > 0
			&& Owner.Player is Player player)
		{
			await PlayerCmd.GainEnergy(track.EnergySpent, player);
			track.EnergyRestored = true;
			restored = true;
		}

		if (restored)
		{
			Flash();
		}
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == Owner)
		{
			GetInternalData<Data>().ActivePlays.Remove(cardPlay.Card);
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
