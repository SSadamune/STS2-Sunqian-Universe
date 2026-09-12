using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「好火啊…」：你给予的灼烧层数按 <see cref="Amount"/>% 增加。
/// 图标层数显示为 100、150、200、250…；灼烧层数修改顺序由原版
/// <c>Hook.ModifyPowerAmountGiven</c> 保证：先加算，再乘算。
/// </summary>
[RegisterPower]
public sealed class GoodFirePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GoodFirePower.png",
		BigIconPath: "res://images/powers/GoodFirePowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override decimal ModifyPowerAmountGivenMultiplicative(
		PowerModel power,
		Creature giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource)
	{
		if (giver != Owner || power is not BurningPower || Amount <= 0)
		{
			return 1m;
		}

		return 1m + (Amount / 100m);
	}
}
