#nullable enable
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;

namespace Squ.Combat;

internal static class BurningApplicationTarget
{
    // PowerVar previews use the canonical power and may omit a hovered target.
    // Infer only the enemy-facing Burning value of a tagged card in hand/play;
    // real applications always require an actual enemy target.
    internal static bool IsEnemyApplication(PowerModel power, Creature giver, Creature? target, CardModel? source)
    {
        if (target != null) return target.Side != giver.Side;
        return !power.IsMutable && power is BurningPower && source != null
            && SquCardTags.AppliesBurning(source)
            && source.Pile?.Type is PileType.Hand or PileType.Play
            && source.CombatState?.HittableEnemies.Count > 0;
    }
}
