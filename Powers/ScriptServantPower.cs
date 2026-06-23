using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
public sealed class ScriptServantPower : ScriptPowerTemplate
{
	public const string GeneratedCardVarName = "GeneratedCard";

	private sealed class Data
	{
		public bool GrantUpgradedThrowHimOut;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptServantPower.png",
		BigIconPath: "res://images/powers/ScriptServantPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(GeneratedCardVarName, GeneratedCombatCards.GetDisplayTitle<ThrowHimOut>(upgraded: false)),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<ThrowHimOut>(GetInternalData<Data>().GrantUpgradedThrowHimOut),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		bool upgraded = cardSource is ServantScript { IsUpgraded: true };
		Data data = GetInternalData<Data>();
		data.GrantUpgradedThrowHimOut = upgraded;
		((StringVar)DynamicVars[GeneratedCardVarName]).StringValue =
			$"[gold]{GeneratedCombatCards.GetDisplayTitle<ThrowHimOut>(upgraded)}[/gold]";
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		Player? player = Owner.Player;
		if (player is null || Owner.CombatState is not { } ownerCombatState)
		{
			return;
		}

		Flash();

		await GeneratedCombatCards.AddToHandInCombat<ThrowHimOut>(
			ownerCombatState,
			player,
			GetInternalData<Data>().GrantUpgradedThrowHimOut,
			player);
	}
}
