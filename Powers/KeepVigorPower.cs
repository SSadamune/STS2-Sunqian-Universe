using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization;
using Squ.Cards;
using Squ.Combat;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 每张攻击牌单独占一层计数。同一张牌既消耗活力又消耗酒力，也只扣一层，
/// 并把两种资源一起补回。上将军让同一持有者自动打出的子牌不另占一层。
/// </summary>
[RegisterPower]
public class KeepVigorPower : ModPowerTemplate
{
	private sealed class Data
	{
		public List<AttackPlayTrack> ActiveAttacks { get; } = [];
	}

	private sealed class AttackPlayTrack
	{
		public required CardModel Card { get; init; }

		public required bool ReservesStack { get; init; }

		public decimal VigorSpent { get; set; }

		public decimal DrunkenMightSpent { get; set; }
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/KeepVigorPower.png",
		BigIconPath: "res://images/powers/KeepVigorPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		..NewsanguoDrunkenMightEffect.HoverTips(),
	];

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			NewsanguoDrunkenMightEffect.AddOrText(description);
			return description;
		}
	}

	protected override object InitInternalData() => new Data();

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| cardPlay.PlayIndex != 0
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.Card.Type != CardType.Attack)
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		bool suppress = ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(cardPlay.Card)
			|| SupremeGeneralKeywordSystem.ShouldSuppressSameOwnerChildAttackResources(
				cardPlay.Card,
				cardPlay);
		data.ActiveAttacks.Add(new AttackPlayTrack
		{
			Card = cardPlay.Card,
			ReservesStack = !suppress && CountReservedStacks(data) < Amount,
		});
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (Owner.IsDead
			|| !NewsanguoDrunkenMightEffect.IsSpentResource(power, Owner)
			|| amount >= 0m)
		{
			return Task.CompletedTask;
		}

		AttackPlayTrack? active = FindTrackForSpend(GetInternalData<Data>(), cardSource);
		if (active is { ReservesStack: true })
		{
			if (power is VigorPower)
			{
				active.VigorSpent += -amount;
			}
			else
			{
				active.DrunkenMightSpent += -amount;
			}
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayedLate(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.PlayIndex != cardPlay.PlayCount - 1)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		int trackIndex = FindLastTrackIndex(data, cardPlay.Card);
		if (trackIndex < 0)
		{
			return;
		}

		AttackPlayTrack track = data.ActiveAttacks[trackIndex];
		data.ActiveAttacks.RemoveAt(trackIndex);
		if (!track.ReservesStack || (track.VigorSpent <= 0m && track.DrunkenMightSpent <= 0m))
		{
			return;
		}

		Flash();
		await PowerCmd.Decrement(this);

		if (Owner.IsDead)
		{
			return;
		}

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
		}

		if (track.DrunkenMightSpent > 0m)
		{
			await NewsanguoPublicApiInterop.ApplyDrunkenMight(
				choiceContext,
				Owner,
				track.DrunkenMightSpent,
				Owner,
				cardPlay.Card);
		}
	}

	private static int CountReservedStacks(Data data)
	{
		int count = 0;
		foreach (AttackPlayTrack track in data.ActiveAttacks)
		{
			if (track.ReservesStack)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// 消耗记在打出这张牌的记录上。活力和酒力因此共享同一次计数。
	/// </summary>
	private static AttackPlayTrack? FindTrackForSpend(Data data, CardModel? cardSource)
	{
		if (cardSource is not null)
		{
			for (int i = data.ActiveAttacks.Count - 1; i >= 0; i--)
			{
				if (ReferenceEquals(data.ActiveAttacks[i].Card, cardSource))
				{
					return data.ActiveAttacks[i];
				}
			}
		}

		return data.ActiveAttacks.Count > 0 ? data.ActiveAttacks[^1] : null;
	}

	private static int FindLastTrackIndex(Data data, CardModel card)
	{
		for (int i = data.ActiveAttacks.Count - 1; i >= 0; i--)
		{
			if (ReferenceEquals(data.ActiveAttacks[i].Card, card))
			{
				return i;
			}
		}

		return -1;
	}
}
