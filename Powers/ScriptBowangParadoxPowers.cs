using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

public abstract class ScriptBowangParadoxPowerTemplate : ScriptPowerTemplate
{
    protected abstract bool IsArson { get; }
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/ScriptBowangParadoxPower.png",
        BigIconPath: "res://images/powers/ScriptBowangParadoxPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => IsArson
        ? [..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
            HoverTipFactory.FromPower<BurningPower>(), HoverTipFactory.FromKeyword(CardKeyword.Retain)]
        : [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is not { } player)
        {
            return;
        }
        await Squ.Combat.BowangParadoxEffects.Apply(ActivationChoiceContext, player, Amount, IsArson, cardSource);
    }
}

[RegisterPower]
public sealed class ScriptBowangParadoxAdvancePower : ScriptBowangParadoxPowerTemplate
{
    protected override bool IsArson => false;
}

[RegisterPower]
public sealed class ScriptBowangParadoxArsonPower : ScriptBowangParadoxPowerTemplate
{
    protected override bool IsArson => true;
}
