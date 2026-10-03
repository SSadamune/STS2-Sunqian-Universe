using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
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
/// 刀斧手剧本：持有者的攻击牌本次打出（含仁义双股剑等原地重放带来的所有额外结算）
/// 期间只要自身或嵌套打出的攻击牌击杀过至少一个敌人，在这次打出彻底结束后恢复一次
/// 整条结算链实际消耗的活力与最外层攻击牌消耗的能量。
/// 无论中途杀死了几个敌人、经历了几次 <see cref="CardPlay.PlayIndex"/>，都只在
/// 最后一次结算完成后统一恢复一次——“打出时记录，打出后恢复”，而不是每次击杀各自恢复。
/// 活力恢复仍要求消耗活力的那次攻击吃到活力加成（<see cref="ValueProp.IsPoweredAttack"/>），
/// 否则会出现“环境伤害杀敌后白送一份活力”的问题；能量恢复不受此限制。
/// </summary>
[RegisterPower]
public sealed class ScriptHeadsmanPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public bool RestoreEnergy;

		/// <summary>只为整条嵌套结算链最外层的攻击牌建立记录。</summary>
		public Dictionary<CardModel, AttackPlayTrack> ActivePlays { get; } = [];
	}

	private sealed class AttackPlayTrack
	{
		public required int EnergySpent { get; init; }

		/// <summary>整条结算链中由攻击实际消耗的活力总量。</summary>
		public decimal VigorSpent { get; set; }

		/// <summary>本次打出期间是否至少击杀过一个敌人（不关心具体次数）。</summary>
		public bool AnyKill { get; set; }
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptHeadsmanPower.png",
		BigIconPath: "res://images/powers/ScriptHeadsmanPowerBig.png");

	protected override object InitInternalData() => new Data();

	protected override string SmartDescriptionLocKey =>
		base.Id.Entry + ".smartDescription";

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		GetInternalData<Data>().RestoreEnergy = true;
		return Task.CompletedTask;
	}

	/// <summary>
	/// 只在最外层攻击牌整次打出的第一次结算（<see cref="CardPlay.PlayIndex"/> == 0）时记录，
	/// 后续因重放而追加的结算不会重置已经累积的 <see cref="AttackPlayTrack"/>。
	/// </summary>
	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| !CardResolutionTracker.IsOutermostCardPlay(cardPlay)
			|| cardPlay.Card.Type != CardType.Attack
			|| cardPlay.PlayIndex != 0)
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
	/// 嵌套攻击造成的击杀统一记到最外层攻击牌，资源仍延后到
	/// <see cref="AfterCardPlayedLate"/> 恢复。
	/// </summary>
	public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (Owner.IsDead
			|| !TryGetQualifyingAttackCard(command, out _)
			|| !TryGetRootTrack(out AttackPlayTrack? track))
		{
			return Task.CompletedTask;
		}

		bool killed = command.Results
			.SelectMany(results => results)
			.Any(result => result.WasTargetKilled);
		if (killed)
		{
			track.AnyKill = true;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 依据实际的负向层数变化记录消耗，避免把“上将军”给予的模拟活力加成
	/// 或被抑制的嵌套攻击误判为再次消耗活力。
	/// </summary>
	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (Owner.IsDead
			|| power is not VigorPower
			|| power.Owner != Owner
			|| amount >= 0m
			|| !TryGetRootTrack(out AttackPlayTrack? track))
		{
			return Task.CompletedTask;
		}

		track.VigorSpent += -amount;
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

		if (track.VigorSpent > 0m)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				track.VigorSpent,
				Owner,
				cardPlay.Card);
			if (Owner.GetPower<VigorPower>() is { } restoredVigor)
			{
				AttackVigorResolution.ClearVigorAttackBinding(restoredVigor);
			}
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

	private bool TryGetRootTrack(out AttackPlayTrack track)
	{
		track = null!;
		return CardResolutionTracker.TryGetOutermostCard(Owner.Player, out CardModel rootCard)
			&& rootCard.Type == CardType.Attack
			&& rootCard.Owner.Creature == Owner
			&& GetInternalData<Data>().ActivePlays.TryGetValue(rootCard, out track!);
	}
}
