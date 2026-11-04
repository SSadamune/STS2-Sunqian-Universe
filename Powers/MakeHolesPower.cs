#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Powers;

[RegisterPower]
public sealed class MakeHolesPower : CompanionSkillPower
{
    internal enum Effect { Spread, FollowUp, Burn, Vulnerable, Weak }
    private sealed record Plan(Effect Effect, IReadOnlyList<Creature> Targets);
    private sealed record Choice(Effect Effect);
    private static readonly ConditionalWeakTable<AttackCommand, Plan> Plans = new();
    private static readonly ConditionalWeakTable<AttackCommand, object> ExtraAttacks = new();
    private readonly ConditionalWeakTable<CardPlay, Choice> _choices = new();
    private readonly List<AttackCommand> _running = [];
    private static readonly AccessTools.FieldRef<AttackCommand, Creature?> SingleTarget =
        AccessTools.FieldRefAccess<AttackCommand, Creature?>("_singleTarget");
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/ActuallyNotClosePower.png",
        BigIconPath: "res://images/powers/ActuallyNotClosePowerBig.png");

    private IReadOnlyList<Creature> Targets(AttackCommand command) => command.IsSingleTargeted
        ? [SingleTarget(command)!] : CombatState.HittableEnemies.ToArray();
    internal IReadOnlyList<Effect> GetEligibleEffects(AttackCommand command)
    {
        var targets = Targets(command).Where(target => target.IsAlive).ToArray();
        if (targets.Length == 0) return [];
        List<Effect> effects = [Effect.FollowUp, Effect.Burn];
        if (command.IsSingleTargeted && CombatState.HittableEnemies.Count > 1) effects.Add(Effect.Spread);
        // Both debuff outcomes intentionally use the absence of Weak as their eligibility rule.
        if (targets.Any(target => !target.HasPower<WeakPower>())) effects.AddRange([Effect.Vulnerable, Effect.Weak]);
        return effects;
    }

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (!IsActive || command.Attacker != Owner || command.ModelSource is not CardModel { Type: CardType.Attack }
            || command.CardPlay is not { } play || ExtraAttacks.TryGetValue(command, out _) || Plans.TryGetValue(command, out _)) return;
        var effects = GetEligibleEffects(command);
        if (effects.Count == 0) return;
        var choice = _choices.GetValue(play, _ => new Choice(effects[Owner.PetOwner!.RunState.Rng.CombatTargets.NextInt(effects.Count)]));
        if (effects.Contains(choice.Effect)) await Prepare(command, choice.Effect);
    }

    internal async Task Prepare(AttackCommand command, Effect effect)
    {
        if (!GetEligibleEffects(command).Contains(effect)) return;
        var targets = Targets(command).ToList();
        if (effect == Effect.Spread)
        {
            var others = CombatState.HittableEnemies.Where(target => !targets.Contains(target)).ToList();
            while (targets.Count < 3 && others.Count > 0)
            {
                int index = Owner.PetOwner!.RunState.Rng.CombatTargets.NextInt(others.Count);
                targets.Add(others[index]);
                others.RemoveAt(index);
            }
        }
        Plans.Add(command, new Plan(effect, targets));
        _running.Add(command);
        Flash();
        if (effect == Effect.Vulnerable)
            foreach (var target in targets.Where(target => target.IsAlive))
                await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), target, 1, Owner, command.ModelSource as CardModel);
    }

    internal static IReadOnlyList<Creature> ResolveTargets(AttackCommand command, IReadOnlyList<Creature> original) =>
        Plans.TryGetValue(command, out var plan) && plan.Effect == Effect.Spread ? plan.Targets : original;

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0 || !target.IsAlive) return;
        var command = _running.LastOrDefault();
        if (command != null && command.ModelSource == cardSource && Plans.TryGetValue(command, out var plan) && plan.Effect == Effect.Burn)
            await PowerCmd.Apply<BurningPower>(context, target, 4, Owner.PetOwner!.Creature, cardSource);
    }

    public override async Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        if (command.Attacker != Owner || !Plans.TryGetValue(command, out var plan)) return;
        Plans.Remove(command);
        _running.Remove(command);
        if (plan.Effect == Effect.Weak)
            foreach (var target in plan.Targets.Where(target => target.IsAlive))
                await PowerCmd.Apply<WeakPower>(context, target, 1, Owner, command.ModelSource as CardModel);
        if (plan.Effect != Effect.FollowUp || !Owner.IsAlive || command.ModelSource is not CardModel card || command.CardPlay is not { } play) return;
        foreach (var group in command.Results.SelectMany(results => results).GroupBy(result => result.Receiver))
        {
            if (!group.Key.IsAlive) continue;
            // TotalDamage includes blocked damage: block is resolved afresh for the extra attack.
            // Unpowered prevents applying Strength and other damage bonuses a second time.
            var extra = DamageCmd.Attack(group.Sum(result => result.TotalDamage) * 0.5m)
                .FromCompanion(Owner, card, play).Targeting(group.Key).Unpowered();
            ExtraAttacks.Add(extra, new object());
            try { await extra.Execute(context); }
            finally { ExtraAttacks.Remove(extra); }
        }
    }
}

[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class MakeHolesTargetPatch
{
    [HarmonyPostfix]
    private static void Postfix(AttackCommand __instance, ref IReadOnlyList<Creature> __result) =>
        __result = MakeHolesPower.ResolveTargets(__instance, __result);
}
