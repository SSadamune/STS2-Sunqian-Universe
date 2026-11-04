#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Combat;
using Squ.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Powers;

[RegisterPower]
public sealed class SharedLifePower : CompanionSkillPower
{
    public override int DisplayAmount => DynamicVars["Healing"].IntValue;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Healing", 0)];
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/LifeContractPower.png",
        BigIconPath: "res://images/powers/LifeContractPowerBig.png");
    private string CompanionId => Owner.Monster is ThirdBrother ? CompanionSummons.ThirdBrotherId : CompanionSummons.SecondBrotherId;
    internal static LocString CreateDescription(string? id = null, int max = 0)
    {
        var text = new LocString("powers", "SUNQIAN_UNIVERSE_POWER_SHARED_LIFE_POWER." + (id == null ? "description" : "unlockedDescription"));
        if (id != null) text.Add("Companion", CompanionSummons.GetName(id));
        text.Add("max", max);
        return text;
    }
    public override LocString Description => IsMutable && Owner.PetOwner is { } player
        ? CreateDescription(CompanionId, SharedLifeSystem.GetTotal(player, CompanionId)) : CreateDescription();
    public void Refresh()
    {
        DynamicVars["Healing"].BaseValue = Owner.PetOwner is { } player ? SharedLifeSystem.GetTotal(player, CompanionId) : 0;
        InvokeDisplayAmountChanged();
    }
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) { Refresh(); return Task.CompletedTask; }
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (IsActive && Owner.IsAlive && Owner.PetOwner is { Creature.IsAlive: true } player)
        {
            int healing = Math.Min(Owner.CurrentHp, SharedLifeSystem.Claim(player, CompanionId));
            if (healing > 0) { Flash(); await CreatureCmd.Heal(player.Creature, healing, playAnim: false); }
        }
    }
}
