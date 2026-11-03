#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class HardToRestrainPower : CompanionSkillPower
{
    internal enum Effect { Attack, Burn, StopLifeLoss, ConvertBlock, Wine }
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/ActuallyNotClosePower.png",
        BigIconPath: "res://images/powers/ActuallyNotClosePowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [new HoverTip(new LocString("powers", Id.Entry + ".effects")),
        HoverTipFactory.FromPower<BurningPower>(), HoverTipFactory.FromKeyword(SquKeywords.Enthralled),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromCard<Wine>()];

    private List<CardModel> Attacks => Owner.PetOwner!.PlayerCombatState!.Hand.Cards.Where(card =>
    {
        if (card.Type != CardType.Attack) return false;
        card.CanPlay(out var reason, out _);
        const UnplayableReason ignored = UnplayableReason.EnergyCostTooHigh
            | UnplayableReason.StarCostTooHigh | UnplayableReason.BlockedByHook;
        return (reason & ~ignored) == UnplayableReason.None
            && Hook.ShouldPlay(CombatState, card, out _, AutoPlayType.Default)
            && (card.TargetType != TargetType.AnyEnemy || CombatState.HittableEnemies.Count > 0);
    }).ToList();

    internal IReadOnlyList<Effect> GetEligibleEffects()
    {
        if (Owner.PetOwner is not { Creature.IsAlive: true } player || !Owner.IsAlive || !IsActive) return [];
        List<Effect> result = [];
        if (Attacks.Count > 0) result.Add(Effect.Attack);
        if (player.Creature.GetPower<TinderPower>() is { Amount: > 0 } && CombatState.HittableEnemies.Count > 0)
            result.Add(Effect.Burn);
        if (player.Creature.HasPower<UnstableFactorPower>() && !UnstableFactorPower.IsLifeLossDisabled(player))
            result.Add(Effect.StopLifeLoss);
        if (player.Creature.Block > 0) result.Add(Effect.ConvertBlock);
        result.Add(Effect.Wine);
        return result;
    }

    // Resolve before the end-turn discard so attacks still in hand can be played.
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext context, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.PetOwner is not { } player || side != player.Creature.Side) return;
        var effects = GetEligibleEffects();
        if (effects.Count == 0) return;
        Flash();
        await ExecuteEffect(context, effects[player.RunState.Rng.CombatTargets.NextInt(effects.Count)]);
    }

    internal async Task ExecuteEffect(PlayerChoiceContext context, Effect effect)
    {
        if (!GetEligibleEffects().Contains(effect) || Owner.PetOwner is not { } player) return;
        var rng = player.RunState.Rng.CombatTargets;
        switch (effect)
        {
            case Effect.Attack:
                var attacks = Attacks;
                await CardCmd.AutoPlay(context, attacks[rng.NextInt(attacks.Count)], null);
                break;
            case Effect.Burn:
                var tinder = player.Creature.GetPower<TinderPower>()!;
                int amount = tinder.Amount * 2;
                var targets = CombatState.HittableEnemies;
                var target = targets[rng.NextInt(targets.Count)];
                await PowerCmd.Remove(tinder);
                await PowerCmd.Apply<BurningPower>(context, target, amount, player.Creature, null);
                break;
            case Effect.StopLifeLoss:
                UnstableFactorPower.DisableLifeLoss(player);
                break;
            case Effect.ConvertBlock:
                decimal block = player.Creature.Block;
                await CreatureCmd.LoseBlock(context, player.Creature, block, Owner);
                await CreatureCmd.GainMaxHp(Owner, block);
                break;
            case Effect.Wine:
                await GeneratedCombatCards.AddToDrawPileInCombat<Wine>(CombatState, player, 1, false,
                    player, card =>
                    {
                        CardCmd.ApplyKeyword(card, SquKeywords.Enthralled);
                        CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
                    }, CardPilePosition.Top);
                break;
        }
    }
}
