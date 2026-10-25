using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptBurningLinkedCampsPower : ScriptPowerTemplate
{
	public const int BurnTinder = 6;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBurningLinkedCampsPower.png",
		BigIconPath: "res://images/powers/ScriptBurningLinkedCampsPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override async Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		MegaCrit.Sts2.Core.ValueProps.ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (target != Owner
			|| cardSource is not Burn
			|| result.TotalDamage + result.OverkillDamage <= 0)
		{
			return;
		}

		await GainTinderAsync(choiceContext, BurnTinder, cardSource);
	}

	internal Task AfterBurningDamage(
		PlayerChoiceContext choiceContext,
		IEnumerable<DamageResult> results,
		CardModel? cardSource)
	{
		int damageDealt = results.Sum(result => result.TotalDamage + result.OverkillDamage);
		return damageDealt > 0
			? GainTinderAsync(choiceContext, damageDealt, cardSource)
			: Task.CompletedTask;
	}

	private async Task GainTinderAsync(
		PlayerChoiceContext choiceContext,
		decimal amount,
		CardModel? cardSource)
	{
		Flash();
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			Owner,
			amount,
			Owner,
			cardSource);
	}
}
