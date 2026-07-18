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
/// 刽子手剧本：持有者的攻击牌本次打出（含仁义双股剑等原地重放带来的所有额外结算）
/// 期间只要击杀过至少一个敌人，在这次打出彻底结束后恢复一次其消耗的活力；
/// 升级后同时恢复一次其消耗的能量。
/// 无论中途杀死了几个敌人、经历了几次 <see cref="CardPlay.PlayIndex"/>，都只在
/// 最后一次结算完成后统一恢复一次——“打出时记录，打出后恢复”，而不是每次击杀各自恢复。
/// 活力恢复仍要求消耗活力的那次攻击吃到活力加成（<see cref="ValueProp.IsPoweredAttack"/>），
/// 否则会出现“环境伤害杀敌后白送一份活力”的问题；能量恢复不受此限制。
/// </summary>
[RegisterPower]
public sealed class ScriptExecutionerPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public bool RestoreEnergy;

		public PendingAttack? Pending;

		/// <summary>
		/// 按 <see cref="CardModel"/> 引用为键，避免同一时刻嵌套打出的其它攻击牌
		/// （例如闪电战对抽牌堆里其它牌的 AutoPlay）互相覆盖对方的记录。
		/// </summary>
		public Dictionary<CardModel, AttackPlayTrack> ActivePlays { get; } = [];
	}

	private sealed class PendingAttack
	{
		public required AttackCommand Command { get; init; }

		public required CardModel Card { get; init; }
	}

	private sealed class AttackPlayTrack
	{
		public required int EnergySpent { get; init; }

		/// <summary>本次打出期间，第一次吃到活力加成的攻击所消耗的活力层数；未消耗则为 null。</summary>
		public int? VigorSpent { get; set; }

		/// <summary>本次打出期间是否至少击杀过一个敌人（不关心具体次数）。</summary>
		public bool AnyKill { get; set; }
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

	/// <summary>
	/// 只在整次打出的第一次结算（<see cref="CardPlay.PlayIndex"/> == 0）时记录，
	/// 后续因重放而追加的结算不会重置已经累积的 <see cref="AttackPlayTrack"/>。
	/// </summary>
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

	/// <summary>
	/// 只用来在真正发生击杀时把结果记到对应的 <see cref="AttackPlayTrack"/> 上，
	/// 不在此处恢复任何资源——恢复统一延后到 <see cref="AfterCardPlayedLate"/>。
	/// </summary>
	public override Task BeforeAttack(AttackCommand command)
	{
		if (Owner.IsDead || !TryGetQualifyingAttackCard(command, out CardModel? card))
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		data.Pending = new PendingAttack
		{
			Command = command,
			Card = card,
		};

		if (data.ActivePlays.TryGetValue(card, out AttackPlayTrack? track)
			&& track.VigorSpent is null
			&& command.DamageProps.IsPoweredAttack()
			&& Owner.GetPower<VigorPower>() is { Amount: > 0 } vigor)
		{
			// 只在本次打出期间第一次吃到活力加成时快照：活力本身会在这次攻击后自我清零，
			// 之后的攻击即使还在同一次打出内，也不会再有活力可消耗。
			track.VigorSpent = vigor.Amount;
		}

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

		if (Owner.IsDead)
		{
			return Task.CompletedTask;
		}

		bool killed = command.Results
			.SelectMany(results => results)
			.Any(result => result.WasTargetKilled);
		if (killed && data.ActivePlays.TryGetValue(pending.Card, out AttackPlayTrack? track))
		{
			track.AnyKill = true;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 只在整次打出的最后一次结算（<see cref="CardPlay.PlayIndex"/> == <see cref="CardPlay.PlayCount"/> - 1）
	/// 之后才真正恢复资源；恢复与否、恢复多少都取自累积下来的 <see cref="AttackPlayTrack"/>，
	/// 与本次打出期间具体击杀了几次无关。
	/// </summary>
	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != Owner || cardPlay.PlayIndex != cardPlay.PlayCount - 1)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (!data.ActivePlays.Remove(cardPlay.Card, out AttackPlayTrack? track) || !track.AnyKill)
		{
			return;
		}

		bool restored = false;

		if (track.VigorSpent is int vigorSpent && vigorSpent > 0)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				vigorSpent,
				Owner,
				cardPlay.Card);
			restored = true;
		}

		if (data.RestoreEnergy && track.EnergySpent > 0 && Owner.Player is Player player)
		{
			await PlayerCmd.GainEnergy(track.EnergySpent, player);
			restored = true;
		}

		if (restored)
		{
			Flash();
		}
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
