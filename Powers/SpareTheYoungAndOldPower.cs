#nullable enable
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class SpareTheYoungAndOldPower : CompanionSkillPower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/LifeContractPower.png",
        BigIconPath: "res://images/powers/LifeContractPowerBig.png");
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? card, CardPlay? cardPlay) =>
        IsActive && dealer == Owner && props.IsPoweredAttack() && target != null && target.Side != Owner.Side
        && target.CurrentHp * 4m > target.MaxHp && target.CurrentHp * 4m < target.MaxHp * 3m ? 2m : 1m;
}
