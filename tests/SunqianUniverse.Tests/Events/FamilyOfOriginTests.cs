using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using Squ;
using Squ.Cards;
using Squ.Events;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Events;

internal static class TestEventRelicObtain
{
    public static bool Enabled { get; set; }

    public static List<RelicModel> Relics { get; } = [];

    public static void Reset()
    {
        Enabled = false;
        Relics.Clear();
    }
}

[HarmonyPatch]
internal static class TestEventRelicObtainPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(RelicCmd),
            nameof(RelicCmd.Obtain),
            [typeof(RelicModel), typeof(Player), typeof(int)]);

    private static bool Prefix(
        RelicModel relic,
        Player player,
        ref Task<RelicModel> __result)
    {
        if (!TestEventRelicObtain.Enabled)
        {
            return true;
        }

        relic.Owner = player;
        TestEventRelicObtain.Relics.Add(relic);
        __result = Task.FromResult(relic);
        return false;
    }
}

public sealed class FamilyOfOriginTests : SunqianCombatTestSuite
{
    private const string EventEntry = "SUNQIAN_UNIVERSE_EVENT_FAMILY_OF_ORIGIN";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("family-of-origin-event");

    [Fact]
    public void Event_is_registered_in_both_first_act_maps_and_the_second_act()
    {
        Assert.Contains(ModelDb.Act<Hive>().AllEvents, evt => evt is FamilyOfOrigin);
        Assert.Contains(ModelDb.Act<Overgrowth>().AllEvents, evt => evt is FamilyOfOrigin);
        Assert.Contains(ModelDb.Act<Underdocks>().AllEvents, evt => evt is FamilyOfOrigin);
        Assert.DoesNotContain(ModelDb.Act<Glory>().AllEvents, evt => evt is FamilyOfOrigin);
    }

    [Fact]
    public async Task Reading_every_chapter_grants_the_three_family_cards_and_the_relic()
    {
        TestEventRelicObtain.Enabled = true;
        try
        {
            FamilyOfOrigin evt = await BeginEvent();

            AssertPage(evt, "INITIAL", optionCount: 2);
            AssertReadOptionTips<CutePet>(evt.CurrentOptions[0].HoverTips);
            await evt.CurrentOptions[0].Chosen();

            AssertPage(evt, "CUTE_PET", optionCount: 2);
            Assert.Contains("蛐蛐罐", evt.Description!.GetFormattedText());
            Assert.Single(Player.Deck.Cards.OfType<CutePet>());
            Assert.IsType<NeverHadTheseRelic>(Assert.Single(TestEventRelicObtain.Relics));
            AssertReadOptionTips<NextOfKin>(evt.CurrentOptions[0].HoverTips);
            Assert.Equal(
                $"{EventEntry}.pages.CUTE_PET.options.READ_NEXT_OF_KIN.title",
                evt.CurrentOptions[0].Title.LocEntryKey);
            Assert.Contains("至亲", evt.CurrentOptions[0].Title.GetFormattedText());
            await evt.CurrentOptions[0].Chosen();

            AssertPage(evt, "NEXT_OF_KIN", optionCount: 2);
            Assert.Contains("似哭似笑", evt.Description!.GetFormattedText());
            Assert.Single(Player.Deck.Cards.OfType<NextOfKin>());
            Assert.Empty(Player.Deck.Cards.OfType<BelovedPartner>());
            AssertReadOptionTips<BelovedPartner>(evt.CurrentOptions[0].HoverTips);
            Assert.Equal(
                $"{EventEntry}.pages.NEXT_OF_KIN.options.READ_BELOVED_PARTNER.title",
                evt.CurrentOptions[0].Title.LocEntryKey);
            Assert.Contains("伴侣", evt.CurrentOptions[0].Title.GetFormattedText());
            await evt.CurrentOptions[0].Chosen();

            AssertFinishedOn(evt, "COMPLETED");
            Assert.Contains("斟酒伴舞", evt.Description!.GetFormattedText());
            Assert.Single(Player.Deck.Cards.OfType<BelovedPartner>());
        }
        finally
        {
            TestEventRelicObtain.Reset();
        }
    }

    [Fact]
    public async Task Paels_tooth_removal_and_returned_family_cards_can_strengthen_the_relic_twice()
    {
        // TestTheSpire needs at least one queued combat action before teardown. All event,
        // deck-removal, and relic lifecycle APIs below otherwise execute outside that queue.
        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        NeverHadTheseRelic familyRelic;
        TestEventRelicObtain.Enabled = true;
        try
        {
            FamilyOfOrigin evt = await BeginEvent();
            await evt.CurrentOptions[0].Chosen();
            await evt.CurrentOptions[0].Chosen();
            await evt.CurrentOptions[0].Chosen();
            familyRelic = Assert.IsType<NeverHadTheseRelic>(
                Assert.Single(TestEventRelicObtain.Relics));
        }
        finally
        {
            TestEventRelicObtain.Reset();
        }

        // Attach the event relic silently so deck removal still traverses the real hook list,
        // without running an out-of-room relic-obtain animation during an active test combat.
        Player.AddRelicInternal(familyRelic, silent: true);
        CardModel[] originalFamilyCards = Player.Deck.Cards
            .Where(card => card is CutePet or BelovedPartner or NextOfKin)
            .ToArray();
        Assert.Equal(3, originalFamilyCards.Length);
        HashSet<CardModel> originalFamilyCardSet = [..originalFamilyCards];

        var selector = new ScriptedCardSelector().ThenSelect(options => originalFamilyCards
            .Concat(options.Where(card => !originalFamilyCardSet.Contains(card)).Take(2))
            .ToArray());
        var paelsTooth = (PaelsTooth)ModelDb.Relic<PaelsTooth>().ToMutable();
        paelsTooth.Owner = Player;
        using (UseCardSelector(selector))
        {
            await paelsTooth.AfterObtained();
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Empty(Player.Deck.Cards.Where(card => card is FamilyCardTemplate));
        Assert.Equal(PaelsTooth.cardsCount, paelsTooth.SerializableCards.Count);
        AssertFamilyRemovalCounts(familyRelic, expectedCopiesOfEachCard: 1);

        int energyBeforeFirstTrigger = Player.PlayerCombatState!.Energy;
        int drawBeforeFirstTrigger = PileType.Draw.GetPile(Player).Cards.Count;
        int petsBeforeFirstTrigger = Combat.Creatures.Count(creature => creature.IsPet);
        await familyRelic.AfterSideTurnStart(
            MegaCrit.Sts2.Core.Combat.CombatSide.Player,
            [Player.Creature],
            Combat);

        Assert.Equal(energyBeforeFirstTrigger + 3, Player.PlayerCombatState.Energy);
        Assert.Equal(drawBeforeFirstTrigger - NextOfKin.DrawAmount, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Equal(petsBeforeFirstTrigger + 1, Combat.Creatures.Count(creature => creature.IsPet));
        CardTestAssertions.AssertPowerAmount<RegenPower>(Player.Creature, BelovedPartner.RegenAmount);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, BelovedPartner.TinderAmount);

        // PaelsTooth returns one stored card after each combat. Resolve enough combat-end
        // hooks to return all five cards, including the three Family cards.
        while (paelsTooth.SerializableCards.Count > 0)
        {
            await paelsTooth.AfterCombatEnd(null!);
        }

        CardModel[] returnedFamilyCards = Player.Deck.Cards
            .Where(card => card is CutePet or BelovedPartner or NextOfKin)
            .ToArray();
        Assert.Equal(3, returnedFamilyCards.Length);
        Assert.All(returnedFamilyCards, card => Assert.True(card.IsUpgraded));
        Assert.Empty(paelsTooth.SerializableCards);

        await CardPileCmd.RemoveFromDeck(returnedFamilyCards, showPreview: false);

        Assert.Empty(Player.Deck.Cards.Where(card => card is FamilyCardTemplate));
        AssertFamilyRemovalCounts(familyRelic, expectedCopiesOfEachCard: 2);

        var description = familyRelic.DynamicDescription;
        familyRelic.DynamicVars.AddTo(description);
        string formattedDescription = description.GetFormattedText();
        Assert.Contains("召唤[/gold]8", formattedDescription);
        Assert.Contains(
            $"获得{BelovedPartner.RegenAmount * 2}层[gold]再生[/gold]和{BelovedPartner.TinderAmount * 2}层[gold]火种[/gold]",
            formattedDescription);
        Assert.Contains("抽4张牌", formattedDescription);

        Player.RemoveRelicInternal(familyRelic, silent: true);
    }

    [Fact]
    public async Task Rejecting_the_first_chapter_adds_normal_thinking_and_uses_the_extended_ending()
    {
        FamilyOfOrigin evt = await BeginEvent();

        await evt.CurrentOptions[1].Chosen();

        AssertFinishedOn(evt, "FIRST_REJECTED");
        Assert.Single(Player.Deck.Cards.OfType<NormalThinking>());
        Assert.Empty(Player.Relics.OfType<NeverHadTheseRelic>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Rejecting_after_reading_uses_the_generic_ending_without_extra_cards(int stage)
    {
        TestEventRelicObtain.Enabled = true;
        try
        {
            FamilyOfOrigin evt = await BeginEvent();
            await evt.CurrentOptions[0].Chosen();
            if (stage == 2)
            {
                await evt.CurrentOptions[0].Chosen();
            }
            int deckSizeBefore = Player.Deck.Cards.Count;

            await evt.CurrentOptions[1].Chosen();

            AssertFinishedOn(evt, "REJECTED");
            Assert.Equal(deckSizeBefore, Player.Deck.Cards.Count);
            Assert.Empty(Player.Deck.Cards.OfType<NormalThinking>());
            Assert.IsType<NeverHadTheseRelic>(Assert.Single(TestEventRelicObtain.Relics));
        }
        finally
        {
            TestEventRelicObtain.Reset();
        }
    }

    private async Task<FamilyOfOrigin> BeginEvent()
    {
        var evt = (FamilyOfOrigin)ModelDb.Event<FamilyOfOrigin>().ToMutable();
        await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);
        return evt;
    }

    private static void AssertFamilyRemovalCounts(
        NeverHadTheseRelic relic,
        int expectedCopiesOfEachCard)
    {
        Assert.Equal(expectedCopiesOfEachCard * 3, relic.RemovedFamilyCards);
        Assert.Equal(expectedCopiesOfEachCard, relic.RemovedCutePets);
        Assert.Equal(expectedCopiesOfEachCard, relic.RemovedBelovedPartners);
        Assert.Equal(expectedCopiesOfEachCard, relic.RemovedNextOfKin);
    }

    private static void AssertReadOptionTips<TCard>(IEnumerable<IHoverTip> tips)
        where TCard : CardModel
    {
        IHoverTip[] all = tips.ToArray();
        Assert.Contains(all, tip => tip is CardHoverTip cardTip && cardTip.Card is TCard);
        Assert.Contains(all, tip => tip.CanonicalModel is NeverHadTheseRelic);
        string dexterityTipId = ModelDb.Power<DexterityPower>().Id.ToString();
        Assert.DoesNotContain(
            all.OfType<HoverTip>(),
            tip => tip.Id.Contains("ENERGY.title", StringComparison.Ordinal)
                || tip.Id == dexterityTipId
                || tip.Id.Contains(SquCommonL10n.PlayRateTitleKey, StringComparison.Ordinal));
    }

    private static void AssertPage(FamilyOfOrigin evt, string page, int optionCount)
    {
        Assert.False(evt.IsFinished);
        Assert.Equal(optionCount, evt.CurrentOptions.Count);
        Assert.Equal($"{EventEntry}.pages.{page}.description", evt.Description!.LocEntryKey);
        Assert.True(evt.Description.Exists());
        Assert.All(evt.CurrentOptions, option =>
        {
            Assert.True(option.Title.Exists());
            Assert.True(option.Description.Exists());
        });
    }

    private static void AssertFinishedOn(FamilyOfOrigin evt, string page)
    {
        Assert.True(evt.IsFinished);
        Assert.Empty(evt.CurrentOptions);
        Assert.Equal($"{EventEntry}.pages.{page}.description", evt.Description!.LocEntryKey);
        Assert.True(evt.Description.Exists());
    }
}
