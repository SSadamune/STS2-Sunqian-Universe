using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Squ.Cards;
using Squ.Events;
using Squ.Interop;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Events;

internal enum TestNewsanguoFaction
{
    None,
    CaoWei,
    ShuHan,
}

internal static class TestNewsanguoInterop
{
    public static TestNewsanguoFaction Faction { get; set; }
}

[HarmonyPatch(typeof(NewsanguoPublicApiInterop), "get_IsReady")]
internal static class TestNewsanguoReadyPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (TestNewsanguoInterop.Faction == TestNewsanguoFaction.None)
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.IsCaoWeiCharacter))]
internal static class TestCaoWeiCharacterPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (TestNewsanguoInterop.Faction == TestNewsanguoFaction.None)
        {
            return true;
        }

        __result = TestNewsanguoInterop.Faction == TestNewsanguoFaction.CaoWei;
        return false;
    }
}

[HarmonyPatch(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.IsShuHanCharacter))]
internal static class TestShuHanCharacterPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (TestNewsanguoInterop.Faction == TestNewsanguoFaction.None)
        {
            return true;
        }

        __result = TestNewsanguoInterop.Faction == TestNewsanguoFaction.ShuHan;
        return false;
    }
}

public sealed class UnfriendlyVisitorsTests : SunqianCombatTestSuite
{
    private const string EventEntry = "SUNQIAN_UNIVERSE_EVENT_UNFRIENDLY_VISITORS";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>()
            .AddEnemy<BigDummy>().WithSeed("unfriendly-visitors");

    [Fact]
    public void Event_is_allowed_only_when_every_player_has_a_basic_strike_or_defend()
    {
        var canonical = ModelDb.Event<UnfriendlyVisitors>();
        Assert.True(canonical.IsAllowed(Player.RunState));

        Player ally = Players.Single(player => player != Player);
        foreach (CardModel card in ally.Deck.Cards.ToArray())
        {
            ally.Deck.RemoveInternal(card);
        }

        Assert.False(canonical.IsAllowed(Player.RunState));
    }

    [Fact]
    public void Event_is_registered_in_both_supported_acts()
    {
        Assert.Contains(
            ModelDb.Act<Overgrowth>().AllEvents,
            evt => evt is UnfriendlyVisitors);
        Assert.Contains(
            ModelDb.Act<Underdocks>().AllEvents,
            evt => evt is UnfriendlyVisitors);
    }

    [Theory]
    [InlineData(CardTag.Defend, true, true, false)]
    [InlineData(CardTag.Strike, false, false, true)]
    public async Task Initial_options_lock_exactly_the_choices_whose_required_basic_card_is_missing(
        CardTag removedTag,
        bool firstLocked,
        bool secondLocked,
        bool thirdLocked)
    {
        RemoveBasicCardsWithTag(removedTag);
        UnfriendlyVisitors evt = await BeginEvent();

        Assert.Equal(3, evt.CurrentOptions.Count);
        Assert.Equal(firstLocked, evt.CurrentOptions[0].IsLocked);
        Assert.Equal(secondLocked, evt.CurrentOptions[1].IsLocked);
        Assert.Equal(thirdLocked, evt.CurrentOptions[2].IsLocked);
        Assert.Equal(
            $"{EventEntry}.pages.INITIAL.description",
            evt.Description!.LocEntryKey);
        AssertCurrentPageLocalized(evt);
    }

    [Theory]
    [InlineData(0, CardTag.Defend, typeof(Taunt))]
    [InlineData(1, CardTag.Defend, typeof(ShrugItOff))]
    [InlineData(2, CardTag.Strike, typeof(SuckerPunch))]
    public async Task Every_default_option_transforms_the_selected_basic_card_and_finishes(
        int optionIndex,
        CardTag sourceTag,
        Type replacementType)
    {
        CardModel selected = FindBasicCard(sourceTag);
        CardCmd.Upgrade(selected, CardPreviewStyle.None);
        UnfriendlyVisitors evt = await BeginEvent();
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await evt.CurrentOptions[optionIndex].Chosen();
        }

        AssertAllSelectionsConsumed(selector);
        AssertFinishedOn(evt, "RESULT");
        Assert.DoesNotContain(selected, Player.Deck.Cards);
        CardModel replacement = Assert.Single(
            Player.Deck.Cards,
            card => card.GetType() == replacementType);
        Assert.True(replacement.IsUpgraded);
        Assert.Equal(Player, replacement.Owner);
    }

    private async Task<UnfriendlyVisitors> BeginEvent()
    {
        var evt = (UnfriendlyVisitors)ModelDb.Event<UnfriendlyVisitors>().ToMutable();
        await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);
        return evt;
    }

    private CardModel FindBasicCard(CardTag tag) =>
        Player.Deck.Cards.First(card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(tag));

    private void RemoveBasicCardsWithTag(CardTag tag)
    {
        foreach (CardModel card in Player.Deck.Cards.Where(card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(tag)).ToArray())
        {
            Player.Deck.RemoveInternal(card);
        }
    }

    private static void AssertFinishedOn(UnfriendlyVisitors evt, string page)
    {
        Assert.True(evt.IsFinished);
        Assert.Empty(evt.CurrentOptions);
        Assert.Equal($"{EventEntry}.pages.{page}.description", evt.Description!.LocEntryKey);
        Assert.True(evt.Description.Exists());
    }

    private static void AssertCurrentPageLocalized(UnfriendlyVisitors evt)
    {
        Assert.True(evt.Description!.Exists());
        Assert.All(evt.CurrentOptions, option =>
        {
            Assert.True(option.Title.Exists());
            Assert.True(option.Description.Exists());
        });
    }
}

public sealed class UnfriendlyVisitorsCaoWeiTests : SunqianCombatTestSuite
{
    private const string EventEntry = "SUNQIAN_UNIVERSE_EVENT_UNFRIENDLY_VISITORS";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("unfriendly-visitors-cao-wei");

    [Theory]
    [InlineData(0, CardTag.Defend, typeof(Taunt), "CAO_WEI_TAUNT_RESULT")]
    [InlineData(1, CardTag.Defend, typeof(ShrugItOff), "CAO_WEI_SHRUG_IT_OFF_RESULT")]
    [InlineData(2, CardTag.Strike, typeof(SuckerPunch), "CAO_WEI_SUCKER_PUNCH_RESULT")]
    public async Task Every_cao_wei_option_uses_its_faction_page_and_result(
        int optionIndex,
        CardTag sourceTag,
        Type replacementType,
        string resultPage)
    {
        TestNewsanguoInterop.Faction = TestNewsanguoFaction.CaoWei;
        try
        {
            CardModel selected = Player.Deck.Cards.First(card =>
                card.Rarity == CardRarity.Basic && card.Tags.Contains(sourceTag));
            var evt = (UnfriendlyVisitors)ModelDb.Event<UnfriendlyVisitors>().ToMutable();
            await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);
            Assert.Equal(
                $"{EventEntry}.pages.CAO_WEI_INITIAL.description",
                evt.Description!.LocEntryKey);
            Assert.All(evt.CurrentOptions, option => Assert.Contains(".pages.CAO_WEI_INITIAL.options.", option.TextKey));
            Assert.True(evt.Description.Exists());
            Assert.All(evt.CurrentOptions, option =>
            {
                Assert.True(option.Title.Exists());
                Assert.True(option.Description.Exists());
            });
            var selector = new ScriptedCardSelector().ThenSelect(selected);

            using (UseCardSelector(selector))
            {
                await evt.CurrentOptions[optionIndex].Chosen();
            }

            AssertAllSelectionsConsumed(selector);
            Assert.True(evt.IsFinished);
            Assert.Empty(evt.CurrentOptions);
            Assert.Equal(
                $"{EventEntry}.pages.{resultPage}.description",
                evt.Description!.LocEntryKey);
            Assert.True(evt.Description.Exists());
            Assert.Contains(Player.Deck.Cards, card => card.GetType() == replacementType);
        }
        finally
        {
            TestNewsanguoInterop.Faction = TestNewsanguoFaction.None;
        }
    }
}

public sealed class UnfriendlyVisitorsShuHanTests : SunqianCombatTestSuite
{
    private const string EventEntry = "SUNQIAN_UNIVERSE_EVENT_UNFRIENDLY_VISITORS";
    private static readonly ModelId ProxyStrikeId =
        new("CARD", "NEWSANGUO_CARD_PROXY_STRIKE");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Silent>().AddEnemy<BigDummy>()
            .WithSeed("unfriendly-visitors-shu-han");

    [Theory]
    [InlineData(0, CardTag.Defend, typeof(WatchFireFromShore), "SHU_HAN_WATCH_FIRE_RESULT")]
    [InlineData(2, CardTag.Strike, typeof(GoForTheEyes), "SHU_HAN_GO_FOR_THE_EYES_RESULT")]
    public async Task Built_in_shu_han_options_transform_and_use_their_distinct_result_pages(
        int optionIndex,
        CardTag sourceTag,
        Type replacementType,
        string resultPage)
    {
        TestNewsanguoInterop.Faction = TestNewsanguoFaction.ShuHan;
        try
        {
            CardModel selected = Player.Deck.Cards.First(card =>
                card.Rarity == CardRarity.Basic && card.Tags.Contains(sourceTag));
            var evt = (UnfriendlyVisitors)ModelDb.Event<UnfriendlyVisitors>().ToMutable();
            await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);
            Assert.Equal(
                $"{EventEntry}.pages.SHU_HAN_INITIAL.description",
                evt.Description!.LocEntryKey);
            Assert.All(evt.CurrentOptions, option => Assert.Contains(".pages.SHU_HAN_INITIAL.options.", option.TextKey));
            Assert.True(evt.Description.Exists());
            Assert.All(evt.CurrentOptions, option =>
            {
                Assert.True(option.Title.Exists());
                Assert.True(option.Description.Exists());
            });
            var selector = new ScriptedCardSelector().ThenSelect(selected);

            using (UseCardSelector(selector))
            {
                await evt.CurrentOptions[optionIndex].Chosen();
            }

            AssertAllSelectionsConsumed(selector);
            Assert.True(evt.IsFinished);
            Assert.Equal(
                $"{EventEntry}.pages.{resultPage}.description",
                evt.Description!.LocEntryKey);
            Assert.True(evt.Description.Exists());
            Assert.Contains(Player.Deck.Cards, card => card.GetType() == replacementType);
        }
        finally
        {
            TestNewsanguoInterop.Faction = TestNewsanguoFaction.None;
        }
    }

    [Fact]
    public async Task Proxy_strike_option_is_locked_when_the_optional_dependency_card_is_absent()
    {
        TestNewsanguoInterop.Faction = TestNewsanguoFaction.ShuHan;
        try
        {
            Assert.Null(ModelDb.GetByIdOrNull<CardModel>(ProxyStrikeId));
            var evt = (UnfriendlyVisitors)ModelDb.Event<UnfriendlyVisitors>().ToMutable();
            await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);

            Assert.True(evt.CurrentOptions[1].IsLocked);
            Assert.False(evt.CurrentOptions[0].IsLocked);
            Assert.False(evt.CurrentOptions[2].IsLocked);
        }
        finally
        {
            TestNewsanguoInterop.Faction = TestNewsanguoFaction.None;
        }
    }

    [Fact]
    public async Task Proxy_strike_option_transforms_when_the_optional_dependency_card_is_available()
    {
        IDictionary models = GetModelDictionary();
        Assert.False(models.Contains(ProxyStrikeId));
        models.Add(ProxyStrikeId, ModelDb.Card<StrikeIronclad>());
        TestNewsanguoInterop.Faction = TestNewsanguoFaction.ShuHan;

        try
        {
            CardModel selected = Player.Deck.Cards.First(card =>
                card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike));
            CardCmd.Upgrade(selected, CardPreviewStyle.None);
            var evt = (UnfriendlyVisitors)ModelDb.Event<UnfriendlyVisitors>().ToMutable();
            await evt.BeginEvent(Player, combatSynchronizer: null, isPreFinished: false);
            Assert.False(evt.CurrentOptions[1].IsLocked);
            var selector = new ScriptedCardSelector().ThenSelect(selected);

            using (UseCardSelector(selector))
            {
                await evt.CurrentOptions[1].Chosen();
            }

            AssertAllSelectionsConsumed(selector);
            Assert.True(evt.IsFinished);
            Assert.Equal(
                $"{EventEntry}.pages.SHU_HAN_PROXY_STRIKE_RESULT.description",
                evt.Description!.LocEntryKey);
            Assert.True(evt.Description.Exists());
            CardModel replacement = Assert.Single(
                Player.Deck.Cards,
                card => card is StrikeIronclad && !ReferenceEquals(card, selected));
            Assert.True(replacement.IsUpgraded);
        }
        finally
        {
            TestNewsanguoInterop.Faction = TestNewsanguoFaction.None;
            models.Remove(ProxyStrikeId);
        }
    }

    private static IDictionary GetModelDictionary()
    {
        FieldInfo field = typeof(ModelDb).GetField(
            "_contentById",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingFieldException(typeof(ModelDb).FullName, "_contentById");
        return (IDictionary)(field.GetValue(null)
            ?? throw new InvalidOperationException("ModelDb content dictionary is null."));
    }
}
