using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptSentryPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptSentryPower.png",
		BigIconPath: "res://images/powers/ScriptSentryPowerBig.png");

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

		return 1m;
	}
}
