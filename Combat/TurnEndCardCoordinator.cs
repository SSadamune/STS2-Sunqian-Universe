using MegaCrit.Sts2.Core.Models;

namespace Squ.Combat;

internal static class TurnEndCardCoordinator
{
    // IterateHookListeners visits this owner's piles in AllPiles order before mod
    // subscribers. Reject followers cheaply; the leader still takes the original
    // full snapshot, preserving order and the selection-pause coordination.
    internal static bool HasEarlierPileCard<T>(T card) where T : CardModel
    {
        if (card.Owner?.IsActiveForHooks != true || card.Owner.PlayerCombatState is not { } state)
            return false;

        foreach (var pile in state.AllPiles)
            foreach (CardModel candidate in pile.Cards)
                if (candidate is T && candidate.Owner == card.Owner && !candidate.HasBeenRemovedFromState)
                    return !ReferenceEquals(candidate, card);
        return false;
    }
}
