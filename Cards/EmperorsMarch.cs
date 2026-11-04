#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "emperors_march")]
public sealed class EmperorsMarch : ModCardTemplate
{
    public EmperorsMarch() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<LiubeiCardPool>();
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CompanionDamageVar(CompanionSummons.SecondBrotherId, 5)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    private bool IsBrotherMissing => Owner == null || CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId) is not { IsAlive: true };
    protected override bool IsPlayable => !IsBrotherMissing;
    protected override bool ShouldGlowRedInternal => IsBrotherMissing;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var pet = CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId);
        if (pet is not { IsAlive: true } || play.Target is not { IsAlive: true } target) return;
        var attack = await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
            .Targeting(target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
        int damage = attack.Results.SelectMany(hit => hit).Where(result => result.Receiver == target).Sum(result => result.TotalDamage);
        if (damage > 0 && target.IsAlive)
            await PowerCmd.Apply<DoomPower>(context, target, damage, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["CompanionDamage"].UpgradeValueBy(2);
}
