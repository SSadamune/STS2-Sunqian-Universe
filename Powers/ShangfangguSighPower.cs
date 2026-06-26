using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 灼烧层数修改顺序由原版 <c>Hook.ModifyPowerAmountGiven</c> 保证：先加算（<see cref="ModifyPowerAmountGivenAdditive"/>），再乘算（<see cref="ModifyPowerAmountGivenMultiplicative"/>）。
/// </summary>
[RegisterPower]
public sealed class ShangfangguSighPower : ModPowerTemplate
{
	public const string MultiplierVarName = "Multiplier";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ShangfangguSighPower.png",
		BigIconPath: "res://images/powers/ShangfangguSighPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(MultiplierVarName, 2m),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncMultiplierVar();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		SyncMultiplierVar();
		return Task.CompletedTask;
	}

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

		return 1m + Amount;
	}

	private void SyncMultiplierVar() =>
		DynamicVars[MultiplierVarName].BaseValue = 1m + Amount;
}
