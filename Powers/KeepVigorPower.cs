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
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// Tracks every Attack as its own play. A stack is spent only when that Attack actually consumes
/// Vigor. Same-owner child Attacks inheriting Vigor from Supreme General neither consume a stack
/// nor absorb another restoration.
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
	];

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
			|| power is not VigorPower
			|| power.Owner != Owner
			|| amount >= 0m)
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		if (data.ActiveAttacks.Count > 0)
		{
			AttackPlayTrack active = data.ActiveAttacks[^1];
			if (active.ReservesStack)
			{
				active.VigorSpent += -amount;
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
		if (!track.ReservesStack || track.VigorSpent <= 0m)
		{
			return;
		}

		Flash();
		await PowerCmd.Decrement(this);

		if (Owner.IsDead)
		{
			return;
		}

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
