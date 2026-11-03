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
public sealed class StopRunningPower : StopFleeingPowerBase
{
	protected override bool TriggersDuringOwnTurn => false;
}

public abstract class StopFleeingPowerBase : ModPowerTemplate
{
	private int _pendingRealLoss;
	protected abstract bool TriggersDuringOwnTurn { get; }
	protected override string SmartDescriptionLocKey => Id.Entry + (IsMutable && Amount > 1 ? ".stackedDescription" : ".description");
	public override MegaCrit.Sts2.Core.Localization.LocString Description
	{
		get { var text = new MegaCrit.Sts2.Core.Localization.LocString("powers", SmartDescriptionLocKey); text.Add("Amount", IsMutable ? Amount : 1); return text; }
	}
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/StopRunningPower.png",
		BigIconPath: "res://images/powers/StopRunningPowerBig.png");
	internal void RecordRealLoss(int amount)
	{
		if (CombatManager.Instance.IsInProgress && Owner.CombatState is { } combat && (TriggersDuringOwnTurn || combat.CurrentSide != Owner.Side) && Owner.IsAlive && amount > 0)
			_pendingRealLoss += amount;
	}
	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != Owner || delta >= 0) return;
		int amount = _pendingRealLoss;
		_pendingRealLoss = 0;
		if (amount <= 0 || Owner.IsDead) return;
		Flash();
		await PowerCmd.Apply<TemporaryHealthPower>(new BlockingPlayerChoiceContext(), Owner, amount * Amount, Owner, null);
	}
	public override async Task AfterSideTurnEndLate(PlayerChoiceContext context, CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side) await PowerCmd.Remove(this);
	}
}
