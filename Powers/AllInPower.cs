#nullable enable

using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// All In: when a manually played card spends the owner's last energy, damage and Burning caused
/// during that card's complete resolution are increased by this power's percentage.
/// </summary>
[RegisterPower]
public sealed class AllInPower : ModPowerTemplate
{
	public const decimal BaseBonusPercent = 30m;
	public const decimal UpgradedBonusPercent = 50m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/AllInPower.png",
		BigIconPath: "res://images/powers/AllInPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (dealer != Owner || !ShouldApplyTo(card))
		{
			return 1m;
		}

		return 1m + GetBonusPercent(card) / 100m;
	}

	public override decimal ModifyPowerAmountGivenMultiplicative(
		PowerModel power,
		Creature giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource)
	{
		if (giver != Owner || power is not BurningPower || !ShouldApplyTo(cardSource))
		{
			return 1m;
		}

		return 1m + GetBonusPercent(cardSource) / 100m;
	}

	private bool ShouldApplyTo(CardModel? card)
	{
		if (AllInResolutionTracker.IsUpdatingCardPreview)
		{
			if (card?.Pile?.Type == PileType.Play
				&& card.Owner?.Creature == Owner
				&& AllInResolutionTracker.TryGetActiveBonus(Owner.Player, out _))
			{
				return true;
			}

			return AllInResolutionTracker.WillConsumeLastEnergy(card);
		}

		return AllInResolutionTracker.TryGetActiveBonus(Owner.Player, out _);
	}

	private decimal GetBonusPercent(CardModel? card) =>
		AllInResolutionTracker.TryGetActiveBonus(Owner.Player, out decimal bonusPercent)
		&& (!AllInResolutionTracker.IsUpdatingCardPreview || card?.Pile?.Type == PileType.Play)
			? bonusPercent
			: Amount;
}
