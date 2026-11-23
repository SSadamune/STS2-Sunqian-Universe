#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "milk_dragon_emerges")]
public sealed class MilkDragonEmerges : SlightRevisionCardTemplate<BowangParadoxScript>
{
    public MilkDragonEmerges() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Dragon];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VigorPower>(BowangParadoxScript.BaseVigor), new PowerVar<TinderPower>(BowangParadoxScript.BaseTinderStacks)];
    public override STS2RitsuLib.Scaffolding.Content.CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BowangParadoxScript.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [..base.AdditionalHoverTips, HoverTipFactory.FromPower<VigorPower>(),
     ..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(), HoverTipFactory.FromPower<BurningPower>(),
     HoverTipFactory.FromKeyword(CardKeyword.Retain)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var advance = CombatState!.CreateCard<MilkDragonAdvance>(Owner);
        var arson = CombatState.CreateCard<MilkDragonArson>(Owner);
        advance.SetAmount(DynamicVars[nameof(VigorPower)].BaseValue);
        arson.SetAmount(DynamicVars[nameof(TinderPower)].BaseValue);
        var selected = await CardSelectCmd.FromChooseACardScreen(context, [advance, arson], Owner, canSkip: false);
        if (selected is MilkDragonOption option)
            await BowangParadoxEffects.Apply(context, Owner, option.DynamicVars["Amount"].BaseValue, option.IsArson, this);
        advance.RemoveFromState();
        arson.RemoveFromState();
    }
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(VigorPower)].UpgradeValueBy(BowangParadoxScript.UpgradedVigor - BowangParadoxScript.BaseVigor);
        DynamicVars[nameof(TinderPower)].UpgradeValueBy(BowangParadoxScript.UpgradedTinderStacks - BowangParadoxScript.BaseTinderStacks);
    }
}
