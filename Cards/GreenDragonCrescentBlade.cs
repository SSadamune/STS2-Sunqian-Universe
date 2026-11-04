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
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Dragon];
    public GreenDragonCrescentBlade() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CompanionDamageVar(CompanionSummons.SecondBrotherId, 9), new DynamicVar("Strength", 2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        new HoverTip(new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SPARE_THE_YOUNG_AND_OLD_POWER.title"),
            new LocString("powers", "SUNQIAN_UNIVERSE_COMPANION_SKILL.rules").GetFormattedText()
            + "\n" + new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SPARE_THE_YOUNG_AND_OLD_POWER.preview").GetFormattedText()),
    ];
    private bool IsBrotherMissing => Owner == null || CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId) is not { IsAlive: true };
    protected override bool IsPlayable => !IsBrotherMissing;
    protected override bool ShouldGlowRedInternal => IsBrotherMissing;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId) is { IsAlive: true } pet)
        {
            await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
                .TargetingAllOpponents(CombatState!).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
            if (!pet.IsAlive) return;
            await PowerCmd.Apply<StrengthPower>(context, pet, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
            await CompanionSkills.Unlock<SpareTheYoungAndOldPower>(context, pet, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars["CompanionDamage"].UpgradeValueBy(4);
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}
