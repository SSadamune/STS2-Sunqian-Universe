using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 剧本：副将。带 <see cref="CardTag.Strike"/> 的牌额外造成 2 点伤害；
/// 带 <see cref="CardTag.Defend"/> 的牌额外获得 1 点格挡。
/// </summary>
[RegisterPower]
public sealed class ScriptViceGeneralPower : ScriptPowerTemplate
{
	public const decimal StrikeBonusDamage = 2m;
	public const decimal DefendBonusBlock = 1m;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptViceGeneralPower.png",
		BigIconPath: "res://images/powers/ScriptViceGeneralPowerBig.png");

	public override decimal ModifyDamageAdditive(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (Owner != dealer || card is null || !props.IsPoweredAttack())
		{
			return 0m;
		}

		if (!card.Tags.Contains(CardTag.Strike))
		{
			return 0m;
		}

		return StrikeBonusDamage;
	}

	public override decimal ModifyBlockAdditive(
		Creature target,
		decimal block,
		ValueProp props,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (target != Owner || cardSource is null || cardSource.Owner != Owner.Player)
		{
			return 0m;
		}

		if (!cardSource.Tags.Contains(CardTag.Defend))
		{
			return 0m;
		}

		return DefendBonusBlock;
	}
}
