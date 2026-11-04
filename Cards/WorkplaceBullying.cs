#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "workplace_bullying")]
public sealed class WorkplaceBullying : ModCardTemplate
{
    private static readonly string[] Brothers = [CompanionSummons.SecondBrotherId, CompanionSummons.ThirdBrotherId];
    public WorkplaceBullying() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(5)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, DynamicVars.Summon),
     CompanionSummonKeywords.NamedHoverTip(CompanionSummons.ThirdBrotherId, DynamicVars.Summon)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (string id in Brothers)
            await CompanionSummons.SummonSpecific(context, Owner, id, DynamicVars.Summon.BaseValue, this);
        foreach (string id in Brothers)
        {
            var pet = CompanionSummons.GetCompanion(Owner, id);
            if (pet is not { IsAlive: true } || play.Target is not { IsAlive: true }) continue;
            await DamageCmd.Attack(pet.CurrentHp).FromCompanion(pet, this, play)
                .Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Summon.UpgradeValueBy(2);
}
