using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanYuMartialVerPower : ScriptPowerTemplate
{
    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;
    private sealed class Data
    {
        public Dictionary<CardModel, int> PendingEnergySpent { get; } = [];
    }
    protected override object InitInternalData() => new Data();
    public override Task AfterEnergySpent(CardModel card, int energySpent)
    {
        if (card.Owner?.Creature == Owner && energySpent > 0)
            GetInternalData<Data>().PendingEnergySpent[card] = energySpent;
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsLastInSeries || cardPlay.Card.Owner.Creature != Owner) return;
        GetInternalData<Data>().PendingEnergySpent.Remove(cardPlay.Card, out int energySpent);
        if (!Owner.IsDead && energySpent > 0)
        {
            Flash();
            // One grant for the entire payment, including X-cost cards.
            // Conversion effects such as Buddy Hu therefore run only once.
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount * energySpent, Owner, cardPlay.Card);
        }
    }
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuMartialVerPower.png",
        BigIconPath: "res://images/powers/GuanYuMartialVerPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.ForEnergy(this)];
}
