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

		public AttackResourceSpendTransaction? ResourceSpend { get; init; }
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
		bool reservesStack = !suppress && CountReservedStacks(data) < Amount;
		data.ActiveAttacks.Add(new AttackPlayTrack
		{
			Card = cardPlay.Card,
			ReservesStack = reservesStack,
			ResourceSpend = reservesStack
				? new AttackResourceSpendTransaction(Owner, cardPlay.Card)
				: null,
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
		if (Owner.IsDead || amount >= 0m)
		{
			return Task.CompletedTask;
		}

		foreach (AttackPlayTrack active in GetInternalData<Data>().ActiveAttacks)
		{
			active.ResourceSpend?.Record(power, amount, cardSource);
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
		if (!track.ReservesStack || track.ResourceSpend is not { SpentAny: true } resourceSpend)
		{
			return;
		}

		Flash();
		await PowerCmd.Decrement(this);

		if (Owner.IsDead)
		{
			return;
		}

		// Each refund effect restores its own recorded debit, independently of other refunds.
		decimal vigorToRestore = resourceSpend.VigorSpent;
		if (vigorToRestore > 0m)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner,
				vigorToRestore,
				Owner,
				cardPlay.Card);

			if (Owner.GetPower<VigorPower>() is { } restoredVigor)
			{
				AttackVigorResolution.ClearVigorAttackBinding(restoredVigor);
			}
		}

		decimal drunkenMightToRestore = resourceSpend.GetRestorableDrunkenMight();
		if (drunkenMightToRestore > 0m)
		{
			await NewsanguoPublicApiInterop.ApplyDrunkenMight(
				choiceContext,
				Owner,
				drunkenMightToRestore,
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
