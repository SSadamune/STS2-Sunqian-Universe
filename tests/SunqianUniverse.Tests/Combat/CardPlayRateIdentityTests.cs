using System.Text.Json;
using System.Diagnostics;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Combat;
using STS2RitsuLib.RunData;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Combat;

public sealed class CardPlayRateIdentityTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("entity-play-rate");

    private CardModel[] Strikes() => Player.PlayerCombatState!.AllCards
        .OfType<StrikeIronclad>().Where(card => card.DeckVersion != null).Take(2).ToArray();

    private (int Plays, int Entries) Stats(CardModel card)
    {
        Assert.True(CardDrawPlayRateTracker.TryGetStats(Player, card, 10, true, out int plays, out int entries));
        return (plays, entries);
    }

    private void RestoreHistory(CardDrawPlayRateTracker.PlayerSaveState state)
    {
        var store = (PlayerRunSavedData<CardDrawPlayRateTracker.PlayerSaveState>)
            AccessTools.Field(typeof(CardDrawPlayRateTracker), "SavedData").GetValue(null)!;
        store.Modify(Player, saved =>
        {
            saved.NextCardId = state.NextCardId;
            saved.RecentCombats = state.RecentCombats;
        });
        AccessTools.Method(typeof(CardDrawPlayRateTracker), "LoadSavedState")
            .Invoke(null, [Player, true]);
    }

    [Fact]
    public async Task Identical_deck_cards_accumulate_independently_and_survive_upgrade_and_enchantment()
    {
        CardModel[] cards = Strikes();
        Assert.Equal(2, cards.Length);
        var firstBefore = Stats(cards[0]);
        var secondBefore = Stats(cards[1]);
        await CardPileCmd.Add(cards[0], PileType.Discard);
        await CardPileCmd.Add(cards[0], PileType.Hand);
        await PlayerCmd.SetEnergy(3, Player);
        await Play(cards[0], EnemyAt(0));
        Assert.Equal((firstBefore.Plays + 1, firstBefore.Entries + 1), Stats(cards[0]));
        Assert.Equal(secondBefore, Stats(cards[1]));

        var beforeUpgrade = Stats(cards[0]);
        Upgrade(cards[0].DeckVersion!);
        CardCmd.Enchant(ModelDb.Enchantment<Sharp>().ToMutable(), cards[0].DeckVersion!, 3);
        Assert.Equal(beforeUpgrade, Stats(cards[0]));
        Assert.Equal(beforeUpgrade, Stats(cards[0].DeckVersion!));
        Assert.Equal(secondBefore, Stats(cards[1]));
    }

    [Fact]
    public async Task Saved_identity_round_trips_but_permanent_copies_get_new_ids()
    {
        CardModel original = Strikes()[0].DeckVersion!;
        Assert.True(CardPlayRateIdentity.TryGetKey(original, out string originalKey));
        var restored = CardModel.FromSerializable(original.ToSerializable());
        Assert.True(CardPlayRateIdentity.TryGetKey(restored, out string restoredKey));
        Assert.Equal(originalKey, restoredKey);

        var writer = new PacketWriter();
        original.ToSerializable().Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var packetCard = new SerializableCard();
        packetCard.Deserialize(reader);
        var received = CardModel.FromSerializable(packetCard);
        Assert.True(CardPlayRateIdentity.TryGetKey(received, out string receivedKey));
        Assert.Equal(originalKey, receivedKey);

        var copy = Player.RunState.CloneCard(original);
        Assert.False(CardPlayRateIdentity.TryGetKey(copy, out _));
        await CardPileCmd.Add(copy, PileType.Deck);
        Assert.True(CardPlayRateIdentity.TryGetKey(copy, out string copyKey));
        Assert.NotEqual(originalKey, copyKey);
        Assert.Equal((0, 0), Stats(copy));
    }

    [Fact]
    public void Queries_do_not_allocate_identity_on_generated_cards_or_previews()
    {
        var generated = Combat.CreateCard<StrikeIronclad>(Player);
        string before = JsonSerializer.Serialize(generated.ToSerializable());
        Assert.False(CardDrawPlayRateTracker.TryGetStats(Player, generated, 10, true, out _, out _));
        CardDrawPlayRateTracker.GetHighestPlayRateCards(Player, [generated]);
        Assert.False(CardPlayRateIdentity.TryGetKey(generated, out _));
        Assert.Equal(before, JsonSerializer.Serialize(generated.ToSerializable()));
    }

    [Fact]
    public void Legacy_grouped_history_is_inherited_as_separate_baselines()
    {
        CardModel[] deck = PileType.Deck.GetPile(Player).Cards.OfType<StrikeIronclad>().Take(2).ToArray();
        string floor = deck[0].FloorAddedToDeck?.ToString() ?? "none";
        string legacy = $"{deck[0].Id.Entry}|u0|f{floor}|enone";
        List<CardDrawPlayRateTracker.CombatSnapshot> history = [new()];
        history[0].Cards[legacy] = new() { PlayFromHandCount = 2, HandEntryCount = 5 };
        CardDrawPlayRateTracker.MigrateLegacyHistory(deck, history);
        Assert.Equal(2, history[0].Cards.Count);
        Assert.All(history[0].Cards.Values, stats => Assert.Equal(2, stats.PlayFromHandCount));
        Assert.NotSame(history[0].Cards.Values.First(), history[0].Cards.Values.Last());
        var state = new CardDrawPlayRateTracker.PlayerSaveState { NextCardId = 100, RecentCombats = history };
        var restored = JsonSerializer.Deserialize<CardDrawPlayRateTracker.PlayerSaveState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(100, restored.NextCardId);
        Assert.Equal(2, restored.RecentCombats[0].Cards.Count);
    }

    [Fact]
    public async Task Removed_ids_are_not_reused_and_deck_reordering_preserves_identity()
    {
        CardModel original = Strikes()[0].DeckVersion!;
        Assert.True(CardPlayRateIdentity.TryGetKey(original, out string key));
        PileType.Deck.GetPile(Player).MoveToBottomInternal(original);
        Assert.True(CardPlayRateIdentity.TryGetKey(original, out string reorderedKey));
        Assert.Equal(key, reorderedKey);
        var copy = Player.RunState.CloneCard(original);
        await CardPileCmd.RemoveFromDeck(original, showPreview: false);
        await CardPileCmd.Add(copy, PileType.Deck);
        Assert.True(CardPlayRateIdentity.TryGetKey(copy, out string newKey));
        Assert.NotEqual(key, newKey);
    }

    [Fact]
    public async Task Restored_finished_history_follows_the_card_and_counter_continues()
    {
        CardModel original = Strikes()[0].DeckVersion!;
        Assert.True(CardPlayRateIdentity.TryGetKey(original, out string key));
        var state = new CardDrawPlayRateTracker.PlayerSaveState { NextCardId = 100 };
        state.RecentCombats.Add(new());
        state.RecentCombats[0].Cards[key] = new() { PlayFromHandCount = 3, HandEntryCount = 7 };
        var restoredState = JsonSerializer.Deserialize<CardDrawPlayRateTracker.PlayerSaveState>(
            JsonSerializer.Serialize(state))!;
        var serializedCard = original.ToSerializable();
        await CardPileCmd.RemoveFromDeck(original, showPreview: false);
        CardModel restoredCard = Player.RunState.LoadCard(serializedCard, Player);
        await CardPileCmd.Add(restoredCard, PileType.Deck);
        RestoreHistory(restoredState);
        Assert.Equal((3, 7), Stats(restoredCard));
        Upgrade(restoredCard);
        CardCmd.Enchant(ModelDb.Enchantment<Sharp>().ToMutable(), restoredCard, 3);
        Assert.Equal((3, 7), Stats(restoredCard));
        var copy = Player.RunState.CloneCard(restoredCard);
        await CardPileCmd.Add(copy, PileType.Deck);
        Assert.True(CardPlayRateIdentity.TryGetKey(copy, out string copyKey));
        Assert.Equal("i100", copyKey);
        Assert.Equal((0, 0), Stats(copy));
    }

    [Fact]
    public void Persistence_size_and_lookup_cost_probe()
    {
        // Report actual serializer overhead, including the RitsuLib envelope.
        // No timing assertion: CI scheduling must not make a correct implementation flaky.
        CardModel original = Strikes()[0].DeckVersion!;
        var clone = (CardModel)original.MutableClone();
        var options = new JsonSerializerOptions { IncludeFields = true };
        int identityBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(original.ToSerializable(), options))
            - Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(clone.ToSerializable(), options));
        var state = new CardDrawPlayRateTracker.PlayerSaveState { NextCardId = 101 };
        for (int combat = 0; combat < 10; combat++)
        {
            var snapshot = new CardDrawPlayRateTracker.CombatSnapshot();
            for (int card = 1; card <= 100; card++)
                snapshot.Cards[$"i{card}"] = new() { PlayFromHandCount = 12, HandEntryCount = 34 };
            state.RecentCombats.Add(snapshot);
        }
        int historyBytes = JsonSerializer.SerializeToUtf8Bytes(state).Length;
        Assert.InRange(identityBytes, 1, 1024);
        Assert.InRange(historyBytes, 1, 20000);

        RestoreHistory(state); // Measure the full ten-combat window, not an empty history.

        for (int i = 0; i < 100; i++) Stats(original);
        long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
            CardDrawPlayRateTracker.TryGetStats(Player, original, 10, true, out _, out _);
        timer.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        SquMod.Logger.Info($"[EntityPlayRateProbe] identityBytes={identityBytes}, history100x10Bytes={historyBytes}, "
            + $"queries10000Ms={timer.Elapsed.TotalMilliseconds:F3}, allocatedBytes={allocated}");
    }
}
