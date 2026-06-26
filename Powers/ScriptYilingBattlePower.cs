using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptYilingBattlePower : ScriptPowerTemplate
{
	public const string GeneratedCardVarName = "GeneratedCard";

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptYilingBattlePower.png",
		BigIconPath: "res://images/powers/ScriptYilingBattlePowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(
			GeneratedCardVarName,
			GeneratedCombatCards.GetDisplayTitle<ShangfangguSigh>(upgraded: false)),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<ShangfangguSigh>(upgrade: false),
	];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		((StringVar)DynamicVars[GeneratedCardVarName]).StringValue =
			$"[gold]{GeneratedCombatCards.GetDisplayTitle<ShangfangguSigh>(upgraded: false)}[/gold]";
		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		Player? player = oldOwner.Player;
		ICombatState? combatState = oldOwner.CombatState;
		if (player is not null && combatState is not null)
		{
			await GeneratedCombatCards.AddToDrawPileInCombat<ShangfangguSigh>(
				combatState,
				player,
				1,
				upgraded: false,
				player);
		}

		await base.AfterRemoved(oldOwner);
	}
}
