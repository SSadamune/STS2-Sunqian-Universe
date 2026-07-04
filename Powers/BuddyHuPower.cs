using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 好哥们胡兄：每次获得活力时少获得 Amount 点，改为获得 Amount 点力量。
/// </summary>
[RegisterPower]
public sealed class BuddyHuPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BuddyHuPower.png",
		BigIconPath: "res://images/powers/BuddyHuPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override bool TryModifyPowerAmountReceived(
		PowerModel canonicalPower,
		Creature target,
		decimal amount,
		Creature? applier,
		out decimal modifiedAmount)
	{
		if (canonicalPower is VigorPower && target == Owner && amount > 0)
		{
			modifiedAmount = Math.Max(0m, amount - Amount);
			return true;
		}

		modifiedAmount = amount;
		return false;
	}

	public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
		if (power is VigorPower)
		{
			Flash();
			await PowerCmd.Apply<StrengthPower>(
				new ThrowingPlayerChoiceContext(),
				Owner,
				Amount,
				Owner,
				null);
		}
	}
}
