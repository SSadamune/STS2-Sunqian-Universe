using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
	public const string GeneratedCardVarName = "GeneratedCard";

	private sealed class Data
	{
		public bool GrantUpgradedShangfangguSigh;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBurningLinkedCampsPower.png",
		BigIconPath: "res://images/powers/ScriptBurningLinkedCampsPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(
			GeneratedCardVarName,
			GeneratedCombatCards.GetDisplayTitle<ShangfangguSigh>(upgraded: false)),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<Burn>(),
		HoverTipFactory.FromCard<ShangfangguSigh>(GetInternalData<Data>().GrantUpgradedShangfangguSigh),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		bool upgraded = cardSource is BurningLinkedCampsScript { IsUpgraded: true };
		Data data = GetInternalData<Data>();
		data.GrantUpgradedShangfangguSigh = upgraded;
		((StringVar)DynamicVars[GeneratedCardVarName]).StringValue =
			$"[gold]{GeneratedCombatCards.GetDisplayTitle<ShangfangguSigh>(upgraded)}[/gold]";
		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		Player? player = oldOwner.Player;
		ICombatState? combatState = oldOwner.CombatState;
		if (player is not null && combatState is not null)
		{
			await GeneratedCombatCards.AddToHandInCombat<Burn>(
				combatState,
				player,
				upgraded: false,
				player);

			await GeneratedCombatCards.AddToDrawPileInCombat<ShangfangguSigh>(
				combatState,
				player,
				1,
				upgraded: GetInternalData<Data>().GrantUpgradedShangfangguSigh,
				player);
		}

		await base.AfterRemoved(oldOwner);
	}
}
