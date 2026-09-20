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
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 好哥们龙套帝：每回合第 <see cref="TriggerCardCount"/> 次打出牌后抽 <see cref="Amount"/> 张牌。
/// 层数叠加时增加抽牌数。
/// </summary>
[RegisterPower]
public sealed class BuddyLongtaodiPower : ModPowerTemplate
{
	public const int TriggerCardCount = 3;

	private sealed class Data
	{
		public int CardsPlayedThisTurn;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BuddyLongtaodiPower.png",
		BigIconPath: "res://images/powers/BuddyLongtaodiPowerBig.png");

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncPlayCountFromHistory();
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

		GetInternalData<Data>().CardsPlayedThisTurn = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| cardPlay.PlayIndex != 0
			|| CombatState is not { } combatState
			|| combatState.CurrentSide != Owner.Side)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.CardsPlayedThisTurn++;
		if (data.CardsPlayedThisTurn != TriggerCardCount)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(choiceContext, Amount, player);
	}

	private void SyncPlayCountFromHistory()
	{
		if (CombatState is not { } combatState || Owner.Player is not { } player)
		{
			return;
		}

		GetInternalData<Data>().CardsPlayedThisTurn =
			CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
				entry.HappenedThisTurn(combatState)
				&& entry.CardPlay.Card.Owner == player
				&& entry.CardPlay.PlayIndex == 0);
	}
}
