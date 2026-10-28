using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
			return Math.Max(0, SwitchesPerTurn - _switchesThisTurn);
		}
	}

	internal bool CanReselectFromScriptCollection()
	{
		if (_selectionInProgress
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
					prompt: ScriptSystem.CreateReselectionPrompt(RemainingSwitchesThisTurn));
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
			Status = RelicStatus.Normal;
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
		Status = RelicStatus.Normal;
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
