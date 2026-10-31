using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 武关羽：获得力量直到剧本失效；打出的攻击牌每消耗 1 点能量，获得等量于力量的格挡。
/// </summary>
[RegisterPower]
public sealed class GuanYuMartialVerPower : ScriptPowerTemplate
{
    private decimal _grantedStrength;

    public override string MutuallyExclusiveGroup => GuanYuCivilVer.FormGroup;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/GuanYuMartialVerPower.png",
        BigIconPath: "res://images/powers/GuanYuMartialVerPowerBig.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount <= 0m)
        {
            return;
        }

        _grantedStrength = Amount;
        await PowerCmd.Apply<StrengthPower>(
            ActivationChoiceContext,
            Owner,
            Amount,
            Owner,
            cardSource);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Player is not { } player
            || Owner.IsDead
            || cardPlay.Card.Type != CardType.Attack
            || cardPlay.Card.Owner != player)
        {
            return;
        }

        int energy = cardPlay.Resources.EnergySpent;
        decimal strength = Owner.GetPower<StrengthPower>()?.Amount ?? 0m;
        if (energy <= 0 || strength <= 0m)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, strength * energy, ValueProp.Unpowered, cardPlay);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_grantedStrength > 0m && !oldOwner.IsDead)
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(),
                oldOwner,
                -_grantedStrength,
                oldOwner,
                null);
        }

        await base.AfterRemoved(oldOwner);
    }
}
