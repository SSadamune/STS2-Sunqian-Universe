#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Cards;
using Squ.Combat;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Powers;

[RegisterPower]
public sealed class DragonEmperorMarchPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("UpgradedCount", 0)];
    internal int UpgradedCount => IsMutable ? Math.Clamp(DynamicVars["UpgradedCount"].IntValue, 0, Amount) : 0;
    internal int NormalCount => (IsMutable ? Amount : 1) - UpgradedCount;
    internal void AddSource(bool upgraded)
    {
        if (upgraded) DynamicVars["UpgradedCount"].BaseValue++;
        InvokeDisplayAmountChanged();
    }
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/LifeContractPower.png",
        BigIconPath: "res://images/powers/LifeContractPowerBig.png");
    public override LocString Description
    {
        get
        {
            var text = new LocString("powers", Id.Entry + (NormalCount == 0 ? ".upgradedDescription"
                : UpgradedCount == 0 ? ".description" : ".mixedDescription"));
            text.Add("NormalCount", NormalCount);
            text.Add("UpgradedCount", UpgradedCount);
            return text;
        }
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.IsDead || Owner.Player is not { } player || play.Card.Owner != player
            || !play.Card.Tags.Contains(SquCardTags.Dragon)
            || CompanionSummons.GetCompanion(player, CompanionSummons.SecondBrotherId) is not { IsAlive: true }) return;
        int normal = NormalCount, upgraded = UpgradedCount;
        Flash();
        for (int i = 0; i < normal; i++) await GeneratedCombatCards.AddToHandInCombat<EmperorsMarch>(CombatState, player, false);
        for (int i = 0; i < upgraded; i++) await GeneratedCombatCards.AddToHandInCombat<EmperorsMarch>(CombatState, player, true);
    }
}
