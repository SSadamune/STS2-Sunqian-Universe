using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanYuMartialVerPower : ScriptPowerTemplate
{
    public override bool CountsTowardActivationLimit => false;
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuMartialVerPower.png",
        BigIconPath: "res://images/powers/GuanYuMartialVerPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), ..NewsanguoDrunkenMightEffect.HoverTips()];
    protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
    {
        if (CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
            entry.HappenedThisTurn(CombatState) && entry.CardPlay.Player == Owner.Player
            && entry.CardPlay.Card.Type == CardType.Attack
            && !ChaosHarmedYou.DoesNotConsumeAttackPlayTracking(entry.CardPlay.Card)))
        {
            return;
        }
        var current = Owner.GetPower<VigorAmplificationPower>();
        if (current is { OnlyFirstAttackThisTurn: true })
        {
            await PowerCmd.Remove(current);
        }
        var bonus = (VigorAmplificationPower)ModelDb.Power<VigorAmplificationPower>().ToMutable();
        bonus.OnlyFirstAttackThisTurn = true;
        await PowerCmd.Apply(ActivationChoiceContext, bonus, Owner, 1, Owner, cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.GetPower<VigorAmplificationPower>() is { OnlyFirstAttackThisTurn: true } bonus)
        {
            await PowerCmd.Remove(bonus);
        }
        await base.AfterRemoved(oldOwner);
    }
}
