using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>一人一城：激活时立即触发覆甲，并减少一层。</summary>
[RegisterPower]
public sealed class ScriptOneManOneCityPower : ScriptPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/ScriptOneManOneCityPower.png",
        BigIconPath: "res://images/powers/ScriptOneManOneCityPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<PlatingPower>()];

    protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.GetPower<PlatingPower>() is not { Amount: > 0 } plating)
        {
            return;
        }
        Flash();
        await plating.BeforeSideTurnEndEarly(ActivationChoiceContext, Owner.Side, [Owner]);
        await PowerCmd.Decrement(plating);
    }
}
