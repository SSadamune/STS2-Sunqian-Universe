using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Combat;

public sealed class TurnCardHistoryTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("turn-history-index");

    [Fact]
    public async Task Large_history_is_visited_once_and_queries_match_original_multiplayer_predicates()
    {
        var state = Player.Creature.CombatState!;
        var history = new CombatHistory();
        var ally = Players.Single(player => player != Player);
        var attack = await AddToHand<StrikeLongtao>();
        var chaos = await AddToHand<ChaosHarmedYou>();
        var allyAttack = await AddCardToPile<StrikeLongtao>(PileType.Hand, owner: ally);
        for (int i = 0; i < 20000; i++)
            history.BlockGained(state, Player.Creature, 1, ValueProp.Unpowered, null);
        AddPlay(history, state, chaos, 1);
        AddPlay(history, state, attack, 2);
        AddPlay(history, state, attack, 2, playIndex: 1);
        AddPlay(history, state, allyAttack, 3);
        var index = TurnCardHistory.Get(state, history);
        AssertMatches(history, index, state, Player);
        AssertMatches(history, index, state, ally);
        Assert.Equal(0, index.CountAttacks(Player, int.MaxValue, attack));
        long visited = index.EntriesVisited;
        for (int i = 0; i < 1000; i++)
        {
            Assert.Same(index, TurnCardHistory.Get(state, history));
            Assert.Equal(3, index.EnergySpent(Player));
        }
        Assert.Equal(visited, index.EntriesVisited);
        AddPlay(history, state, attack, 4);
        index = TurnCardHistory.Get(state, history);
        Assert.Equal(visited + 2, index.EntriesVisited);
        AssertMatches(history, index, state, Player);
        Assert.Equal(1, index.CountAttacks(Player, 1));
        Assert.Equal(2, index.CountAttacks(Player, 2));
    }

    [Fact]
    public async Task Coordinator_uses_the_same_leader_as_full_listener_order_for_each_owner()
    {
        var ally = Players.Single(player => player != Player);
        await AddCardToPile<CutePet>(PileType.Draw, owner: ally);
        await AddCardToPile<BelovedPartner>(PileType.Discard);
        await AddCardToPile<CutePet>(PileType.Draw);
        await AddCardToPile<NextOfKin>(PileType.Hand);
        await AddCardToPile<Stargazing>(PileType.Draw);
        await AddCardToPile<Stargazing>(PileType.Discard);
        foreach (Player owner in Players)
        {
            var families = owner.Creature.CombatState!.IterateHookListeners()
                .OfType<FamilyCardTemplate>().Where(card => card.Owner == owner).ToList();
            foreach (var card in families)
                Assert.Equal(card != families[0], TurnEndCardCoordinator.HasEarlierPileCard<FamilyCardTemplate>(card));
            var stars = owner.Creature.CombatState!.IterateHookListeners()
                .OfType<Stargazing>().Where(card => card.Owner == owner).ToList();
            foreach (var card in stars)
                Assert.Equal(card != stars[0], TurnEndCardCoordinator.HasEarlierPileCard(card));
        }
    }

    [Fact]
    public async Task Clear_and_refill_to_same_length_and_new_history_do_not_reuse_old_counts()
    {
        var state = Player.Creature.CombatState!;
        var history = new CombatHistory();
        var attack = await AddToHand<StrikeLongtao>();
        AddPlay(history, state, attack, 2);
        Assert.Equal(2, TurnCardHistory.Get(state, history).EnergySpent(Player));
        history.Clear();
        AddPlay(history, state, attack, 7);
        Assert.Equal(7, TurnCardHistory.Get(state, history).EnergySpent(Player));
        Assert.Equal(0, TurnCardHistory.Get(state, new CombatHistory()).EnergySpent(Player));
        Assert.Equal(7, TurnCardHistory.Get(state, history).EnergySpent(Player));
        history.Clear();
        Assert.Equal(0, TurnCardHistory.Get(state, history).CountAttacks(Player, 1));
    }

    [Fact]
    public async Task Turn_transition_without_new_history_entries_invalidates_the_index()
    {
        var state = Player.Creature.CombatState!;
        var history = new CombatHistory();
        var attack = await AddToHand<StrikeLongtao>();
        AddPlay(history, state, attack, 2);
        Assert.Equal(2, TurnCardHistory.Get(state, history).EnergySpent(Player));
        // A teammate's independent turn change must invalidate even when round/side
        // and this player's turn have not changed (the game's predicate checks all).
        Players.Single(player => player != Player).PlayerCombatState!.IncrementTurnNumber();
        var index = TurnCardHistory.Get(state, history);
        AssertMatches(history, index, state, Player);
        Assert.Equal(0, index.EnergySpent(Player));
        AddPlay(history, state, attack, 5);
        AssertMatches(history, TurnCardHistory.Get(state, history), state, Player);
    }

    private static void AssertMatches(CombatHistory history, TurnCardHistory index, ICombatState state, Player player)
    {
        Assert.Equal(history.CardPlaysStarted.Where(entry => entry.HappenedThisTurn(state)
            && entry.CardPlay.Player == player && entry.CardPlay.PlayIndex == 0)
            .Sum(entry => entry.CardPlay.Resources.EnergySpent), index.EnergySpent(player));
        Assert.Equal(history.CardPlaysFinished.Count(entry => entry.HappenedThisTurn(state)
            && entry.CardPlay.Player == player && entry.CardPlay.PlayIndex == 0), index.FinishedFirstPlays(player));
        Assert.Equal(history.CardPlaysStarted.Count(entry => entry.HappenedThisTurn(state)
            && entry.CardPlay.Player == player && entry.CardPlay.Card.Type == CardType.Attack
            && entry.CardPlay.Card is not ChaosHarmedYou), index.CountAttacks(player, int.MaxValue));
    }

    private static void AddPlay(CombatHistory history, ICombatState state, CardModel card, int energy, int playIndex = 0)
    {
        var play = new CardPlay
        {
            Card = card, Player = card.Owner, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = false, PlayIndex = playIndex, PlayCount = 2
        };
        history.CardPlayStarted(state, play);
        history.CardPlayFinished(state, play);
    }
}
