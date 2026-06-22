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
		BigIconPath: "res://images/powers/ScriptSentryPower.png");

	public override decimal ModifyDamageAdditive(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card)
	{
		if (Owner != dealer || card is null || !props.IsPoweredAttack())
		{
			return 0m;
		}

		if (!card.Title.Contains("打击"))
		{
			return 0m;
		}

		return 1m;
	}
}
