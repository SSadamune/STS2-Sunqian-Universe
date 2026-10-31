using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>累积火烧连营的灼伤加成，供下一张上方谷之叹使用。</summary>
[RegisterPower]
public sealed class PendingGoodFirePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override Color AmountLabelColor => MegaCrit.Sts2.Core.Models.PowerModel._normalAmountLabelColor;
}
