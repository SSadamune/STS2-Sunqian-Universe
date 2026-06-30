using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「燃料充足」：下一张带 <see cref="SquCardTags.Burning"/> 的牌给予的灼烧额外加层，可跨回合保留。
/// 该牌一次打出内的多次给予灼烧均有效，于 <see cref="AfterCardPlayedLate"/> 后移除。
/// </summary>
[RegisterPower]
public sealed class FuelAbundantPower : ModPowerTemplate
{
	private sealed class Data
	{
		public CardModel? BoundCardSource;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/FuelAbundantPower.png",
		BigIconPath: "res://images/powers/FuelAbundantPowerBig.png");

	protected override object InitInternalData() => new Data();

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
		if (giver != Owner || power is not BurningPower || Amount <= 0
			|| cardSource == null || !SquCardTags.AppliesBurning(cardSource))
		{
			return 0m;
		}

		Data data = GetInternalData<Data>();
		if (data.BoundCardSource != null && data.BoundCardSource != cardSource)
		{
			return 0m;
		}

		data.BoundCardSource ??= cardSource;
		return Amount;
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!SquCardTags.AppliesBurning(cardPlay.Card))
		{
			return;
		}

		GetInternalData<Data>().BoundCardSource = null;
		await PowerCmd.Remove(this);
	}
}
