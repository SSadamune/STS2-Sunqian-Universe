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
/// 「不熄」：持有期间，灼烧的熄灭判定不会移除灼烧；每次判定消耗 1 层。
/// </summary>
[RegisterPower]
public sealed class UnextinguishedPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/UnextinguishedPower.png",
		BigIconPath: "res://images/powers/UnextinguishedPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	/// <summary>
	/// If this creature has at least 1 stack, consume 1 stack and prevent Burning from extinguishing this turn.
	/// Extinguish chance still increases as if it did not extinguish.
	/// </summary>
	public static async Task<bool> TryPreventExtinguish(Creature owner)
	{
		if (owner.GetPower<UnextinguishedPower>() is not { Amount: > 0 } unextinguished)
		{
			return false;
		}

		unextinguished.Flash();
		await PowerCmd.TickDownDuration(unextinguished);
		return true;
	}
}
