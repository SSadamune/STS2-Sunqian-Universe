#nullable enable
using STS2RitsuLib.Interop.AutoRegistration;
namespace Squ.Powers;

[RegisterPower]
public sealed class StopFleeingUntilEnemyTurnEndPower : StopFleeingPowerBase
{
    protected override bool TriggersDuringOwnTurn => true;
}
