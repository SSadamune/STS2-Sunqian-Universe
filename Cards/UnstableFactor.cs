#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "unstable_factor")]
public sealed class UnstableFactor : ModCardTemplate, IRowKeywordsCard
{
    public UnstableFactor() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/UnstableFactor.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(4)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [CompanionSummonKeywords.NamedHoverTip(CompanionSummons.ThirdBrotherId, DynamicVars.Summon),
        HoverTipFactory.FromPower<HardToRestrainPower>(),
        HoverTipFactory.FromKeyword(SquKeywords.CompanionSkill)];
    protected override void AddExtraArgsToDescription(LocString description) =>
        SquKeywords.AddNestedLoc(description, "SummonText", CompanionSummonKeywords.NamedCardText(
            CompanionSummons.ThirdBrotherId, DynamicVars.Summon));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<UnstableFactorPower>(context, Owner.Creature,
            DynamicVars.Summon.BaseValue, Owner.Creature, this);
        if (power != null) power.AddLifeLoss();
        await CompanionSkills.UnlockForCompanion<HardToRestrainPower>(context, Owner, CompanionSummons.ThirdBrotherId);
    }
    protected override void OnUpgrade() => DynamicVars.Summon.UpgradeValueBy(1);
}
