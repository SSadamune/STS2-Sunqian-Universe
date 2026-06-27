using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本：一人一城。原版覆甲回合结束触发后，额外损失 1 层并再获得等量于当前层数的格挡。
/// </summary>
[RegisterPower]
public sealed class ScriptYiRenYiChengPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptYiRenYiChengPower.png",
		BigIconPath: "res://images/powers/ScriptYiRenYiChengPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>(),
	];

	/// <summary>
	/// 晚于 <see cref="PlatingPower.BeforeSideTurnEndEarly"/>，在原版覆甲获得格挡之后再结算剧本。
	/// </summary>
	public override async Task BeforeSideTurnEnd(
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

		await PowerCmd.Decrement(plating);
		if (plating.Amount <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, plating.Amount, ValueProp.Unpowered, null);
	}
}
