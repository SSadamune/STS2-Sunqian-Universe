#nullable enable

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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class MetamorphosisPower : ModPowerTemplate
{
	public const string CharacterNamesVarName = "CharacterNames";

	private sealed class Data
	{
		public HashSet<ModelId> RecordedPoolIds { get; } = [];
		public HashSet<ModelId> TriggeredPoolIdsThisTurn { get; } = [];
		public bool TriggeredColorlessThisTurn;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/MetamorphosisPowerIcon.png",
		BigIconPath: "res://images/powers/MetamorphosisPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(CharacterNamesVarName, string.Empty),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
	];

	protected override string SmartDescriptionLocKey =>
		IsComplete
			? Id.Entry + ".smartDescriptionComplete"
			: base.SmartDescriptionLocKey;

	protected override object InitInternalData() => new Data();

	public bool HasRecorded(CardPoolModel pool) =>
		GetInternalData<Data>().RecordedPoolIds.Contains(pool.Id);

	public bool Record(CardPoolModel pool)
	{
		if (!MetamorphosisCharacterCatalog.IsSupported(pool)
			|| !GetInternalData<Data>().RecordedPoolIds.Add(pool.Id))
		{
			return false;
		}

		RefreshCharacterNames();
		return true;
	}

	public void SyncAmountAndTurnHistory(CardModel? currentlyResolvingCard)
	{
		SetAmount(GetInternalData<Data>().RecordedPoolIds.Count);
		SyncPlayedPoolsFromHistory(currentlyResolvingCard);
		InvokeDisplayAmountChanged();
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncAmountAndTurnHistory(cardSource);
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

		Data data = GetInternalData<Data>();
		data.TriggeredPoolIdsThisTurn.Clear();
		data.TriggeredColorlessThisTurn = false;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| CombatState?.CurrentSide != Owner.Side
			|| !TryMarkTriggered(cardPlay.Card))
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(Metamorphosis.EnergyGain, player);
	}

	private bool TryMarkTriggered(CardModel card)
	{
		Data data = GetInternalData<Data>();
		if (card.Pool.IsColorless)
		{
			if (!IsComplete || data.TriggeredColorlessThisTurn)
			{
				return false;
			}

			data.TriggeredColorlessThisTurn = true;
			return true;
		}

		return data.RecordedPoolIds.Contains(card.Pool.Id)
			&& data.TriggeredPoolIdsThisTurn.Add(card.Pool.Id);
	}

	private bool IsComplete
	{
		get
		{
			if (!IsMutable || Owner.Player is not { } player)
			{
				return false;
			}

			ModelId currentPoolId = player.Character.CardPool.Id;
			return MetamorphosisCharacterCatalog.SupportedPools
				.Where(pool => pool.Id != currentPoolId)
				.All(pool => HasRecorded(pool));
		}
	}

	private void RefreshCharacterNames()
	{
		Data data = GetInternalData<Data>();
		string separator = new LocString(
			"powers",
			Id.Entry + ".characterSeparator").GetFormattedText() ?? "、";
		((StringVar)DynamicVars[CharacterNamesVarName]).StringValue = string.Join(
			separator,
			MetamorphosisCharacterCatalog.SupportedPools
				.Where(pool => data.RecordedPoolIds.Contains(pool.Id))
				.Select(MetamorphosisCharacterCatalog.GetDisplayName));
	}

	private void SyncPlayedPoolsFromHistory(CardModel? currentlyResolvingCard)
	{
		if (CombatState is not { } combatState || Owner.Player is not { } player)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.TriggeredPoolIdsThisTurn.Clear();
		data.TriggeredColorlessThisTurn = false;

		IEnumerable<CardModel> playedCards = CombatManager.Instance.History.CardPlaysFinished
			.Where(entry => entry.HappenedThisTurn(combatState)
				&& entry.CardPlay.Card.Owner == player)
			.Select(entry => entry.CardPlay.Card);
		if (currentlyResolvingCard?.Owner == player)
		{
			playedCards = playedCards.Append(currentlyResolvingCard);
		}

		foreach (CardModel playedCard in playedCards)
		{
			if (playedCard.Pool.IsColorless)
			{
				data.TriggeredColorlessThisTurn |= IsComplete;
			}
			else if (data.RecordedPoolIds.Contains(playedCard.Pool.Id))
			{
				data.TriggeredPoolIdsThisTurn.Add(playedCard.Pool.Id);
			}
		}
	}
}
