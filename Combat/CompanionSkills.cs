#nullable enable
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;

namespace Squ.Combat;

/// <summary>Skills retain their unlock order across death and revival.</summary>
public static class CompanionSkills
{
    private static readonly ConditionalWeakTable<ICombatState, Dictionary<(Player, string), List<PowerModel>>> Unlocks = new();
    public static int RequiredMaxHp(int slot) => Math.Max(0, slot) * 10;

    public static async Task UnlockForCompanion<T>(PlayerChoiceContext context, Player player,
        string id) where T : CompanionSkillPower
    {
        var data = Unlocks.GetOrCreateValue(player.Creature.CombatState!);
        if (!data.TryGetValue((player, id), out var skills)) data[(player, id)] = skills = [];
        var skill = ModelDb.Power<T>();
        if (!skills.Contains(skill)) skills.Add(skill);
        if (CompanionSummons.GetCompanion(player, id) is { IsAlive: true } pet)
            await ApplyUnlocks(context, player, id, pet);
    }

    internal static async Task ApplyUnlocks(PlayerChoiceContext context, Player player, string id, Creature pet)
    {
        if (!Unlocks.TryGetValue(player.Creature.CombatState!, out var data)
            || !data.TryGetValue((player, id), out var skills)) return;
        foreach (var skill in skills)
            if (!pet.HasPower(skill.Id)) await PowerCmd.Apply(context, skill.ToMutable(), pet, 1, pet, null);
    }

    public static async Task Unlock<T>(PlayerChoiceContext context, Creature pet,
        CardModel source) where T : CompanionSkillPower
    {
        if (!pet.HasPower<T>())
            await PowerCmd.Apply<T>(context, pet, 1, pet, source);
    }
}
