using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>剧本：一人一城。覆甲获得双倍格挡，并在正常衰减之外额外减少1层。</summary>
[RegisterPower]
public sealed class ScriptOneManOneCityPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptOneManOneCityPower.png",
		BigIconPath: "res://images/powers/ScriptOneManOneCityPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>(),
	];

	public override async Task BeforeSideTurnEndEarly(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner))
		{
			return;
		}

		PlatingPower? plating = Owner.GetPower<PlatingPower>();
		if (plating is null || plating.Amount <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, plating.Amount, ValueProp.Unpowered, null);
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner)
			|| Owner.Player?.PlayerCombatState?.TurnNumber == 1)
		{
			return;
		}

		PlatingPower? plating = Owner.GetPower<PlatingPower>();
		if (plating is not null && plating.Amount > 0)
		{
			Flash();
			await PowerCmd.Decrement(plating);
		}
	}
}
