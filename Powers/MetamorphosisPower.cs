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
using Squ.Combat;
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
			|| CombatState?.CurrentSide != Owner.Side)
		{
			return;
		}

		int triggerCount = MarkAndCountTriggers(cardPlay.Card);
		if (triggerCount == 0)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(Metamorphosis.EnergyGain * triggerCount, player);
	}

	private int MarkAndCountTriggers(CardModel card)
	{
		Data data = GetInternalData<Data>();
		int triggerCount = 0;
		if (IsComplete
			&& CharacterCardPoolMembership.IsColorless(card)
			&& !data.TriggeredColorlessThisTurn)
		{
			data.TriggeredColorlessThisTurn = true;
			triggerCount++;
		}

		if (Owner.Player is not { } player)
		{
			return triggerCount;
		}

		foreach (CardPoolModel pool in MetamorphosisCharacterCatalog.GetSupportedPools(player))
		{
			if (data.RecordedPoolIds.Contains(pool.Id)
				&& CharacterCardPoolMembership.Contains(pool, card)
				&& data.TriggeredPoolIdsThisTurn.Add(pool.Id))
			{
				triggerCount++;
			}
		}

		return triggerCount;
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
			return MetamorphosisCharacterCatalog.GetSupportedPools(player)
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
			(Owner.Player is { } player
				? MetamorphosisCharacterCatalog.GetSupportedPools(player)
				: MetamorphosisCharacterCatalog.SupportedPools)
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

		IReadOnlyList<CardPoolModel> supportedPools =
			MetamorphosisCharacterCatalog.GetSupportedPools(player);
		foreach (CardModel playedCard in playedCards)
		{
			if (IsComplete && CharacterCardPoolMembership.IsColorless(playedCard))
			{
				data.TriggeredColorlessThisTurn = true;
			}

			foreach (CardPoolModel pool in supportedPools)
			{
				if (data.RecordedPoolIds.Contains(pool.Id)
					&& CharacterCardPoolMembership.Contains(pool, playedCard))
				{
					data.TriggeredPoolIdsThisTurn.Add(pool.Id);
				}
			}
		}
	}
}
