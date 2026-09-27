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
/// Tracks each root Attack as one complete play. It consumes one stack even if no Vigor was
/// available, then restores exactly the Vigor spent by that Attack after its final play resolves.
/// Nested Attacks inside an <see cref="AttackVigorResolution"/> suppression scope do neither.
/// </summary>
[RegisterPower]
public sealed class KeepVigorPower : ModPowerTemplate
{
	private sealed class Data
	{
		public Dictionary<CardModel, AttackPlayTrack> ActivePlays { get; } = [];

		public List<CardModel> ActivePlayOrder { get; } = [];
	}

	private sealed class AttackPlayTrack
	{
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
			|| Amount <= 0m
			|| cardPlay.PlayIndex != 0
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.Card.Type != CardType.Attack
			|| ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(cardPlay.Card)
			|| AttackVigorResolution.IsNestedAttackConsumptionSuppressed(Owner.Player))
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		data.ActivePlays[cardPlay.Card] = new AttackPlayTrack();
		data.ActivePlayOrder.Remove(cardPlay.Card);
		data.ActivePlayOrder.Add(cardPlay.Card);
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
			|| amount >= 0m
			|| AttackVigorResolution.IsNestedAttackConsumptionSuppressed(Owner.Player))
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		for (int i = data.ActivePlayOrder.Count - 1; i >= 0; i--)
		{
			CardModel activeCard = data.ActivePlayOrder[i];
			if (data.ActivePlays.TryGetValue(activeCard, out AttackPlayTrack? track))
			{
				track.VigorSpent += -amount;
				break;
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
		if (!data.ActivePlays.Remove(cardPlay.Card, out AttackPlayTrack? track))
		{
			return;
		}

		data.ActivePlayOrder.Remove(cardPlay.Card);
		Flash();
		await PowerCmd.Decrement(this);

		if (Owner.IsDead || track.VigorSpent <= 0m)
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
}
