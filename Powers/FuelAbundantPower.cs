using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「燃料充足」：下一次给予的灼烧层数加成，可跨回合保留，生效一次后移除。层数可叠加。
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

	public override async Task AfterModifyingPowerAmountGiven(PowerModel power)
	{
		if (power is BurningPower)
		{
			await PowerCmd.Remove(this);
		}
	}
}
