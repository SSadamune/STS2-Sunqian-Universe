#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Powers;

[RegisterPower]
public sealed class ActuallyNotClosePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Debuff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
	public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/ActuallyNotClosePower.png", BigIconPath: "res://images/powers/ActuallyNotClosePowerBig.png");
	public override async Task AfterSideTurnEndLate(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		if (Owner.PetOwner is { } player && side != player.Creature.Side)
			await PowerCmd.Decrement(this);
	}
}
