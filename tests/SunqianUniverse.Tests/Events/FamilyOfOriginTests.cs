using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Cards;
using Squ.Events;
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
    public void Event_is_registered_only_in_the_second_act()
    {
        Assert.Contains(ModelDb.Act<Hive>().AllEvents, evt => evt is FamilyOfOrigin);
        Assert.DoesNotContain(ModelDb.Act<Overgrowth>().AllEvents, evt => evt is FamilyOfOrigin);
        Assert.DoesNotContain(ModelDb.Act<Underdocks>().AllEvents, evt => evt is FamilyOfOrigin);
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

            AssertPage(evt, "PARTNER", optionCount: 2);
            Assert.Single(Player.Deck.Cards.OfType<CutePet>());
            Assert.IsType<NeverHadTheseRelic>(Assert.Single(TestEventRelicObtain.Relics));
            AssertReadOptionTips<BelovedPartner>(evt.CurrentOptions[0].HoverTips);
            await evt.CurrentOptions[0].Chosen();

            AssertPage(evt, "NEXT_OF_KIN", optionCount: 2);
            Assert.Single(Player.Deck.Cards.OfType<BelovedPartner>());
            AssertReadOptionTips<NextOfKin>(evt.CurrentOptions[0].HoverTips);
            await evt.CurrentOptions[0].Chosen();

            AssertFinishedOn(evt, "COMPLETED");
            Assert.Single(Player.Deck.Cards.OfType<NextOfKin>());
        }
        finally
        {
            TestEventRelicObtain.Reset();
        }
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
