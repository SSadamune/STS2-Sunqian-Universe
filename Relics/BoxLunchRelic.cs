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
public sealed class BoxLunchRelic : ScriptRelicTemplate
{
	private int _lastReselectionTurn = int.MinValue;
	private bool _selectionInProgress;

	public override RelicRarity Rarity => RelicRarity.Starter;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/BoxLunchRelic.png",
		IconOutlinePath: "res://images/relics/BoxLunchRelicOutline.png",
		BigIconPath: "res://images/relics/BoxLunchRelicBig.png");

	internal bool CanReselectFromScriptCollection()
	{
		int? turn = Owner.PlayerCombatState?.TurnNumber;
		return !_selectionInProgress
			&& turn.HasValue
			&& _lastReselectionTurn != turn.Value
			&& CombatManager.Instance.IsPartOfPlayerTurn(Owner)
			&& ScriptSystem.ScriptCollectionPile.GetPile(Owner).Cards.Count > 0;
	}

	internal async Task<bool> ReselectFromScriptCollectionAsync()
	{
		if (!CanReselectFromScriptCollection())
		{
			return false;
		}

		int turn = Owner.PlayerCombatState!.TurnNumber;
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
				requireManualSelection: true);
			bool requested = selected is not null
				&& ScriptSystem.RequestBoxLunchReselection(Owner, selected);
			if (requested)
			{
				_lastReselectionTurn = turn;
			}

			return requested;
		}
		finally
		{
			_selectionInProgress = false;
			Status = RelicStatus.Normal;
		}
	}

	internal void MarkReselectionUsed(int turnNumber) =>
		_lastReselectionTurn = turnNumber;

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_lastReselectionTurn = int.MinValue;
		_selectionInProgress = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}
}
