using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 阅后即焚：每回合打出的前 <see cref="Amount"/> 张带消耗的牌会给予能量与火种。
/// 层数叠加时增加可触发的消耗牌数量。
/// </summary>
[RegisterPower]
public sealed class BurnAfterReadingPower : ModPowerTemplate
{
	public const int EnergyGain = 1;
	public const decimal TinderStacks = 3m;
	public const int BaseTriggerCount = 1;
	public const int UpgradedTriggerCount = 2;

	private sealed class Data
	{
		public int ExhaustCardsPlayedThisTurn;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BurnAfterReadingPower.png",
		BigIconPath: "res://images/powers/BurnAfterReadingPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(EnergyGain),
		new PowerVar<TinderPower>(TinderStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.ForEnergy(this),
		HoverTipFactory.FromPower<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncExhaustCountFromHistory();
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner))
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().ExhaustCardsPlayedThisTurn = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| cardPlay.PlayIndex != 0
			|| !cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust)
			|| CombatState is not { } combatState
			|| combatState.CurrentSide != Owner.Side)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.ExhaustCardsPlayedThisTurn++;
		if (data.ExhaustCardsPlayedThisTurn > Amount)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, player);
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			Owner,
			DynamicVars[nameof(TinderPower)].BaseValue,
			Owner,
			cardPlay.Card);
	}

	private void SyncExhaustCountFromHistory()
	{
		if (CombatState is not { } combatState || Owner.Player is not { } player)
		{
			return;
		}

		GetInternalData<Data>().ExhaustCardsPlayedThisTurn =
			CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
				entry.HappenedThisTurn(combatState)
				&& entry.CardPlay.Card.Owner == player
				&& entry.CardPlay.PlayIndex == 0
				&& entry.CardPlay.Card.Keywords.Contains(CardKeyword.Exhaust));
	}
}
