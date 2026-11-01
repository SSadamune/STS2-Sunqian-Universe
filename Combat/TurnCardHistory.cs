using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Read-only, combat-scoped index. Only newly appended events are visited between queries.
/// Keep live CardPlay references: other mods may change a played card's type or owner.
/// Turn matching deliberately uses the game's predicate, including every player's turn.
/// </summary>
internal sealed class TurnCardHistory
{
    private static readonly ConditionalWeakTable<ICombatState, TurnCardHistory> Indices = new();
    private readonly List<CardPlay> _started = [];
    private readonly Dictionary<Player, int> _energy = [];
    private readonly Dictionary<Player, int> _finished = [];
    private readonly List<(Player Player, int? Turn)> _turns = [];
    private CombatHistory? _history;
    private CombatHistoryEntry? _first;
    private CombatHistoryEntry? _last;
    private int _processed;
    private int _round;
    private CombatSide _side;

    internal long EntriesVisited { get; private set; }

    internal static TurnCardHistory Get(ICombatState state) => Get(state, CombatManager.Instance.History);

    internal static TurnCardHistory Get(ICombatState state, CombatHistory history)
    {
        TurnCardHistory index = Indices.GetValue(state, static _ => new());
        index.Refresh(state, history);
        return index;
    }

    private void Refresh(ICombatState state, CombatHistory history)
    {
        // The current game exposes its append-only List through IEnumerable. If this ever
        // changes, a full snapshot remains correct instead of relying on a private field.
        IReadOnlyList<CombatHistoryEntry> entries = history.Entries as IReadOnlyList<CombatHistoryEntry>
            ?? new List<CombatHistoryEntry>(history.Entries);
        bool reset = !ReferenceEquals(_history, history) || _round != state.RoundNumber
            || _side != state.CurrentSide || _turns.Count != state.Players.Count
            || entries.Count < _processed
            || (_processed > 0 && (!ReferenceEquals(_first, entries[0])
                || !ReferenceEquals(_last, entries[_processed - 1])));
        for (int i = 0; !reset && i < _turns.Count; i++)
        {
            Player player = state.Players[i];
            reset = !ReferenceEquals(_turns[i].Player, player)
                || _turns[i].Turn != player.PlayerCombatState?.TurnNumber;
        }
        if (reset)
        {
            _started.Clear();
            _finished.Clear();
            _energy.Clear();
            _turns.Clear();
            foreach (Player player in state.Players)
                _turns.Add((player, player.PlayerCombatState?.TurnNumber));
            _round = state.RoundNumber;
            _side = state.CurrentSide;
            _history = history;
            _processed = 0;
        }
        for (int i = _processed; i < entries.Count; i++)
        {
            CombatHistoryEntry entry = entries[i];
            EntriesVisited++;
            if (entry is CardPlayStartedEntry started && entry.HappenedThisTurn(state))
            {
                _started.Add(started.CardPlay);
                CardPlay play = started.CardPlay;
                if (play.PlayIndex == 0)
                    _energy[play.Player] = checked(_energy.GetValueOrDefault(play.Player) + play.Resources.EnergySpent);
            }
            else if (entry is CardPlayFinishedEntry finished && entry.HappenedThisTurn(state))
            {
                CardPlay play = finished.CardPlay;
                if (play.PlayIndex == 0)
                    _finished[play.Player] = _finished.GetValueOrDefault(play.Player) + 1;
            }
        }
        _processed = entries.Count;
        _first = entries.Count > 0 ? entries[0] : null;
        _last = entries.Count > 0 ? entries[entries.Count - 1] : null;
    }

    // Stop once the caller's threshold is met; never cache mutable card classifications.
    internal int CountAttacks(Player player, int limit, CardModel? excludedCard = null)
    {
        int count = 0;
        foreach (CardPlay play in _started)
            if (play.Player == player && play.Card.Type == CardType.Attack
                && play.Card is not ChaosHarmedYou && !ReferenceEquals(play.Card, excludedCard)
                && ++count >= limit)
                break;
        return count;
    }

    internal int EnergySpent(Player player) => _energy.GetValueOrDefault(player);

    internal int FinishedFirstPlays(Player player) => _finished.GetValueOrDefault(player);
}
