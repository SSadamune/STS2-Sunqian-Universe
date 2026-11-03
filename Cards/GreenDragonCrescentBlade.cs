#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "green_dragon_crescent_blade")]
public sealed class GreenDragonCrescentBlade : ModCardTemplate, IRowKeywordsCard
{
    public GreenDragonCrescentBlade() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(8), new DynamicVar("Strength", 2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, DynamicVars.Summon),
        HoverTipFactory.FromPower<StrengthPower>(),
        new HoverTip(new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SPARE_THE_YOUNG_AND_OLD_POWER.title"),
            new LocString("powers", "SUNQIAN_UNIVERSE_COMPANION_SKILL.rules").GetFormattedText()
            + "\n" + new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SPARE_THE_YOUNG_AND_OLD_POWER.preview").GetFormattedText()),
    ];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        SquKeywords.AddNestedLoc(description, "SummonText", CompanionSummonKeywords.NamedCardText(
            CompanionSummons.SecondBrotherId, DynamicVars.Summon));
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var result = await CompanionSummons.SummonSpecific(context, Owner,
            CompanionSummons.SecondBrotherId, DynamicVars.Summon.BaseValue, this);
        if (result.Creature is { IsAlive: true } pet)
        {
            await PowerCmd.Apply<StrengthPower>(context, pet, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
            await CompanionSkills.Unlock<SpareTheYoungAndOldPower>(context, pet, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(3);
        DynamicVars["Strength"].UpgradeValueBy(2);
    }
}
