using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GrandGobletPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GrandGobletPower.png",
		BigIconPath: "res://images/powers/GrandGobletPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(0),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this && amount > 0)
		{
			DynamicVars.Energy.BaseValue++;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.Player is not { } player)
		{
			return;
		}

		Flash();
		SquSfx.Play(SquSfx.WontBePoliteEvent);
		await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, player);
		await PowerCmd.Apply<VigorPower>(
			new ThrowingPlayerChoiceContext(),
			Owner,
			Amount,
			Owner,
			null);
	}
}
