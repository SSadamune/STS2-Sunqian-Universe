using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 吕布的篮球：每 3 个回合获得活力。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "lubu_basketball")]
public sealed class LubuBasketballRelic : ModRelicTemplate
{
	private const string TurnsKey = "Turns";

	private bool _isActivating;

	private int _turnsSeen;

	public override RelicRarity Rarity => RelicRarity.Common;

	public override bool ShowCounter => true;

	public override int DisplayAmount
	{
		get
		{
			if (!IsActivating)
			{
				return TurnsSeen;
			}

			return DynamicVars[TurnsKey].IntValue;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(TurnsKey, 3m),
		new PowerVar<VigorPower>(4m),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/LubuBasketballRelic.png",
		IconOutlinePath: "res://images/relics/LubuBasketballRelicOutline.png",
		BigIconPath: "res://images/relics/LubuBasketballRelicBig.png");

	private bool IsActivating
	{
		get => _isActivating;
		set
		{
			AssertMutable();
			_isActivating = value;
			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty]
	public int TurnsSeen
	{
		get => _turnsSeen;
		set
		{
			AssertMutable();
			_turnsSeen = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature))
		{
			return;
		}

		int threshold = DynamicVars[TurnsKey].IntValue;
		TurnsSeen = (TurnsSeen + 1) % threshold;
		Status = TurnsSeen == threshold - 1 ? RelicStatus.Active : RelicStatus.Normal;
		if (TurnsSeen != 0)
		{
			return;
		}

		_ = TaskHelper.RunSafely(DoActivateVisuals());
		await PowerCmd.Apply<VigorPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			null);
	}

	private async Task DoActivateVisuals()
	{
		IsActivating = true;
		Flash();
		await Cmd.Wait(1f);
		IsActivating = false;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}
}
