#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class StopRunningPower : ModPowerTemplate
{
	private int _pendingRealLoss;
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/StopRunningPower.png",
		BigIconPath: "res://images/powers/StopRunningPowerBig.png");
	internal void RecordRealLoss(int amount)
	{
		if (CombatManager.Instance.IsInProgress && Owner.CombatState is { } combat
			&& combat.CurrentSide != Owner.Side && Owner.IsAlive && amount > 0)
			_pendingRealLoss += amount;
	}
	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != Owner || delta >= 0) return;
		int amount = _pendingRealLoss;
		_pendingRealLoss = 0;
		if (amount <= 0 || Owner.IsDead) return;
		Flash();
		await PowerCmd.Apply<TemporaryHealthPower>(new BlockingPlayerChoiceContext(), Owner, amount, Owner, null);
	}
	public override async Task AfterSideTurnEndLate(PlayerChoiceContext context, CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side) await PowerCmd.TickDownDuration(this);
	}
}
