#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "serpent_spear")]
public sealed class SerpentSpear : ModCardTemplate
{
    public SerpentSpear() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/SerpentSpear.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CompanionDamageVar(CompanionSummons.ThirdBrotherId, 11), new DynamicVar("Strength", 2)];
    private bool IsBrotherMissing => Owner == null || CompanionSummons.GetCompanion(Owner, CompanionSummons.ThirdBrotherId) is not { IsAlive: true };
    protected override bool IsPlayable => !IsBrotherMissing;
    protected override bool ShouldGlowRedInternal => IsBrotherMissing;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [HoverTipFactory.FromPower<StrengthPower>(),
     HoverTipFactory.FromPower<MakeHolesPower>(),
     HoverTipFactory.FromKeyword(SquKeywords.CompanionSkill)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var pet = CompanionSummons.GetCompanion(Owner, CompanionSummons.ThirdBrotherId);
        if (pet is not { IsAlive: true } || CombatState!.HittableEnemies.Count == 0) return;
        var targets = CombatState.HittableEnemies;
        var target = targets[Owner.RunState.Rng.CombatTargets.NextInt(targets.Count)];
        await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
            .Targeting(target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
        if (!pet.IsAlive) return;
        await PowerCmd.Apply<StrengthPower>(context, pet, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await CompanionSkills.Unlock<MakeHolesPower>(context, pet, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["CompanionDamage"].UpgradeValueBy(4);
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}
