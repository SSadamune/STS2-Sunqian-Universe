#nullable enable
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Powers;
using Squ.Combat;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

public abstract class CompanionSkillPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    public int RequiredMaxHp => CompanionSkills.RequiredMaxHp(
        Owner.Powers.OfType<CompanionSkillPower>().TakeWhile(skill => skill != this).Count());
    public bool IsActive => Owner.MaxHp >= RequiredMaxHp;
}
