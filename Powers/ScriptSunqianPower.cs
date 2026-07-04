using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptSunqianPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public int DrawOnLift;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptSunqianPower.png",
		BigIconPath: "res://images/powers/ScriptSunqianPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		int drawOnLift = cardSource is SunqianScript { IsUpgraded: true } ? 2 : 1;
		GetInternalData<Data>().DrawOnLift = drawOnLift;
		DynamicVars.Cards.BaseValue = drawOnLift;
		return Task.CompletedTask;
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		int drawOnLift = GetInternalData<Data>().DrawOnLift;
		Player? player = oldOwner.Player;
		if (drawOnLift > 0 && player is not null)
		{
			await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), drawOnLift, player);
		}

		await base.AfterRemoved(oldOwner);
	}
}
