using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「燃料充足」：本回合内给予的灼烧层数加成，与剧本能力无关。层数可叠加。
/// </summary>
[RegisterPower]
public sealed class FuelAbundantPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/FuelAbundantPower.png",
		BigIconPath: "res://images/powers/FuelAbundantPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override decimal ModifyPowerAmountGivenAdditive(
		PowerModel power,
		Creature giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource)
	{
		if (giver != Owner || power is not BurningPower || Amount <= 0)
		{
			return 0m;
		}

		return Amount;
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side == CombatSide.Player && participants.Contains(Owner))
		{
			await PowerCmd.Remove(this);
		}
	}
}
