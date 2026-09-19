using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 长厚似伪：带 <see cref="CardTag.Defend"/> 的牌额外获得格挡，数额为
/// <see cref="PowerModel.Amount"/> 乘以自身负面状态种类数。
/// </summary>
[RegisterPower]
public sealed class TooKindToBeTruePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TooKindToBeTruePower.png",
		BigIconPath: "res://images/powers/TooKindToBeTruePowerBig.png");

	public override decimal ModifyBlockAdditive(
		Creature target,
		decimal block,
		ValueProp props,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (Owner != target || !props.IsPoweredCardOrMonsterMoveBlock())
		{
			return 0m;
		}

		if (cardSource != null && !cardSource.Tags.Contains(CardTag.Defend))
		{
			return 0m;
		}

		int debuffTypes = CountDistinctDebuffs(Owner);
		if (debuffTypes <= 0)
		{
			return 0m;
		}

		return Amount * debuffTypes;
	}

	public override Task AfterModifyingBlockAmount(decimal modifiedBlock, CardModel? cardSource, CardPlay? cardPlay)
	{
		Flash();
		return Task.CompletedTask;
	}

	private static int CountDistinctDebuffs(Creature creature) =>
		creature.Powers
			.Where(power => power.Amount != 0m && power.GetTypeForAmount(power.Amount) == PowerType.Debuff)
			.Select(power => power.Id)
			.Distinct()
			.Count();
}
