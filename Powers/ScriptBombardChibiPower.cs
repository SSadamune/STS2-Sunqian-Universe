using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptBombardChibiPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptBombardChibiPower.png",
		BigIconPath: "res://images/powers/ScriptBombardChibiPowerBig.png");

	public override async Task AfterRemoved(Creature oldOwner)
	{
		if (oldOwner.Player is { } player)
		{
			await PlayerCmd.GainEnergy(1, player);
		}

		await base.AfterRemoved(oldOwner);
	}
}
