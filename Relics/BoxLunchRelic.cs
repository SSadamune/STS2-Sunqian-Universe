using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Cards;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "box_lunch")]
public class BoxLunchRelic : ScriptRelicTemplate
{
	private int _trackedTurn = int.MinValue;
	private int _switchesThisTurn;
	private int _pendingEnergyCost;
	private bool _selectionInProgress;
	private int _cooldown;
	private int _lastCommittedTurn = int.MinValue;
	private int _committedSwitches;

	public const int SleepTurns = 2;
	internal virtual int CooldownAfterActivation => SleepTurns;
	internal int CooldownTurnsRemaining => _cooldown;
	public override bool ShowCounter => DisplayAmount > 0;
	public override int DisplayAmount => !IsCanonical && CombatManager.Instance.IsInProgress && _cooldown > 0
		? _cooldown : -1;

	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<string> RegisteredKeywordIds =>
		[SquKeywords.ScriptId, SquKeywords.ScriptCollectionId];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/BoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/BoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/BoxLunchRelicBig.png");

	internal virtual int SwitchesPerTurn => 1;

	internal virtual int EnergyCostPerSwitch => 0;

	internal int RemainingSwitchesThisTurn
	{
		get
		{
			if (Owner.PlayerCombatState?.TurnNumber is not int turn)
			{
				return 0;
			}
			SyncTurn(turn);
			return _cooldown > 0 ? 0 : Math.Max(0, SwitchesPerTurn - _switchesThisTurn);
		}
	}

	internal bool CanReselectFromScriptCollection()
	{
		if (_selectionInProgress || _cooldown > 0
			|| Owner.PlayerCombatState?.TurnNumber is not int turn
			|| !CombatManager.Instance.IsPartOfPlayerTurn(Owner)
			|| ScriptSystem.ScriptCollectionPile.GetPile(Owner).Cards.Count == 0)
		{
			return false;
		}

		SyncTurn(turn);
		int availableEnergy = Math.Max(0, Owner.PlayerCombatState.Energy - _pendingEnergyCost);
		return _switchesThisTurn < SwitchesPerTurn
			&& availableEnergy >= EnergyCostPerSwitch;
	}

	internal async Task<bool> ReselectFromScriptCollectionAsync()
	{
		if (!CanReselectFromScriptCollection())
		{
			return false;
		}

		int turn = Owner.PlayerCombatState!.TurnNumber;
		SyncTurn(turn);
		_selectionInProgress = true;
		Status = RelicStatus.Active;
		Flash();
		try
		{
			IReadOnlyList<ScriptRecordCardTemplate>? selected =
				await ScriptSystem.ChooseActiveScriptRecordsAsync(
					new BlockingPlayerChoiceContext(),
					Owner,
					cancelable: true,
					requireManualSelection: true,
					prompt: ScriptSystem.CreateReselectionPrompt(RemainingSwitchesThisTurn),
					localOnly: true);
			if (selected is null)
			{
				return false;
			}

			int switchesUsed = _switchesThisTurn + 1;
			if (EnergyCostPerSwitch > 0)
			{
				ReserveEnergyCost();
			}

			bool requested = ScriptSystem.RequestBoxLunchReselection(
				Owner,
				selected,
				switchesUsed,
				EnergyCostPerSwitch);
			if (!requested)
			{
				ReleaseEnergyCost(EnergyCostPerSwitch);
				return false;
			}

			MarkSwitchesUsed(turn, switchesUsed);
			return true;
		}
		finally
		{
			_selectionInProgress = false;
			RefreshStatus();
		}
	}

	internal void MarkSwitchesUsed(int turnNumber, int switchesUsed)
	{
		SyncTurn(turnNumber);
		if (switchesUsed > _switchesThisTurn)
		{
			_switchesThisTurn = switchesUsed;
		}
	}

	internal void ReserveEnergyCost() => _pendingEnergyCost += EnergyCostPerSwitch;

	internal bool TryCommitReselection(int turnNumber, int switchesUsed)
	{
		if (_cooldown > 0 || (_lastCommittedTurn == turnNumber && switchesUsed <= _committedSwitches))
			return false;
		MarkSwitchesUsed(turnNumber, switchesUsed);
		_lastCommittedTurn = turnNumber;
		_committedSwitches = switchesUsed;
		_cooldown = CooldownAfterActivation;
		InvokeDisplayAmountChanged();
		RefreshStatus();
		return true;
	}

	public override Task AfterSideTurnStart(
		CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (side == Owner.Creature.Side && participants.Contains(Owner.Creature) && !Owner.Creature.IsDead)
		{
			SyncTurn(Owner.PlayerCombatState!.TurnNumber);
			if (_cooldown > 0)
			{
				_cooldown--;
				InvokeDisplayAmountChanged();
			}
			RefreshStatus();
		}
		return Task.CompletedTask;
	}

	private void RefreshStatus() => Status = _cooldown == 0 && RemainingSwitchesThisTurn > 0
		? RelicStatus.Active : RelicStatus.Normal;

	internal void ReleaseEnergyCost(int cost)
	{
		if (cost <= 0)
		{
			return;
		}

		_pendingEnergyCost = Math.Max(0, _pendingEnergyCost - cost);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_trackedTurn = int.MinValue;
		_switchesThisTurn = 0;
		_pendingEnergyCost = 0;
		_selectionInProgress = false;
		_cooldown = 0;
		_lastCommittedTurn = int.MinValue;
		_committedSwitches = 0;
		Status = RelicStatus.Normal;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	private void SyncTurn(int turn)
	{
		if (_trackedTurn == turn)
		{
			return;
		}

		_trackedTurn = turn;
		_switchesThisTurn = 0;
		_pendingEnergyCost = 0;
	}
}
