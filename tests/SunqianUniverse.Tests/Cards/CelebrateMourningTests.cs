using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using HarmonyLib;
using Squ;
using Squ.Cards;
using Squ.Combat;
using System.Text.Json;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

internal static class TestCelebrateMourningCombatState
{
    public static bool ForceOutsideCombat { get; set; }

    public static bool ForceOutsideRun { get; set; }
}

[HarmonyPatch(typeof(CombatManager), "get_IsInProgress")]
internal static class TestCelebrateMourningCombatStatePatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!TestCelebrateMourningCombatState.ForceOutsideCombat)
        {
            return true;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(RunManager), "get_IsInProgress")]
internal static class TestCelebrateMourningRunStatePatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!TestCelebrateMourningCombatState.ForceOutsideRun)
        {
            return true;
        }

        __result = false;
        return false;
    }
}

public sealed class CelebrateMourningTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("celebrate-mourning");

    [Fact]
    public async Task Exhausting_a_highest_rate_family_card_returns_celebrate_mourning_to_hand()
    {
        var celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        AssertInPile(celebrate, PileType.Discard);
        var family = await AddToHand<CutePet>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), family);

        AssertInPile(family, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Base_card_grants_five_vigor_at_zero_cost()
    {
        int energy = Player.PlayerCombatState!.Energy;
        await Play(await AddToHand<CelebrateMourning>());

        Assert.Equal(energy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, CelebrateMourning.BaseVigor);
    }

    [Fact]
    public async Task Upgrade_grants_seven_vigor_at_zero_cost()
    {
        int energy = Player.PlayerCombatState!.Energy;
        await Play(Upgrade(await AddToHand<CelebrateMourning>()));

        Assert.Equal(energy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, CelebrateMourning.UpgradedVigor);
    }

    [Fact]
    public async Task Highest_rate_celebrate_mourning_returns_all_copies_including_itself()
    {
        CelebrateMourning returning = await AddToHand<CelebrateMourning>();
        await Play(returning);
        AssertInPile(returning, PileType.Discard);
        CelebrateMourning exhausted = await AddToHand<CelebrateMourning>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);

        AssertInPile(exhausted, PileType.Hand);
        AssertInPile(returning, PileType.Hand);
    }

    [Fact]
    public async Task Previously_exhausted_higher_rate_card_is_excluded_from_later_comparisons()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        AssertInPile(celebrate, PileType.Discard);
        CardModel lowerRate = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet higherRate = await AddToHand<CutePet>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), lowerRate);
        AssertInPile(celebrate, PileType.Discard);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), higherRate);
        AssertInPile(celebrate, PileType.Hand);
        await CardPileCmd.Add(celebrate, PileType.Discard);
        await CardPileCmd.Add(lowerRate, PileType.Hand);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), lowerRate);

        AssertInPile(higherRate, PileType.Exhaust);
        AssertInPile(lowerRate, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Combat_hover_shows_return_targets_without_out_of_combat_annotation()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        AllIn target = await AddToHand<AllIn>();
        string annotation = new LocString("cards", celebrate.Id.Entry + ".annotation").GetFormattedText()
            ?? throw new InvalidOperationException("Celebrate Mourning annotation is missing.");
        HoverTip contextualTip = Assert.Single(
            celebrate.HoverTips.OfType<HoverTip>().Where(tip =>
                tip.Title == SquCommonL10n.AnnotationTitle().GetFormattedText()));

        Assert.NotEqual(annotation, contextualTip.Description);
        Assert.Contains(target.Title, contextualTip.Description);
    }

    [Fact]
    public async Task In_run_outside_combat_hover_still_shows_return_targets_without_annotation()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        AllIn target = await AddToHand<AllIn>();
        string annotation = new LocString("cards", celebrate.Id.Entry + ".annotation").GetFormattedText()
            ?? throw new InvalidOperationException("Celebrate Mourning annotation is missing.");

        TestCelebrateMourningCombatState.ForceOutsideCombat = true;
        try
        {
            HoverTip contextualTip = Assert.Single(
                celebrate.HoverTips.OfType<HoverTip>().Where(tip =>
                    tip.Title == SquCommonL10n.AnnotationTitle().GetFormattedText()));
            Assert.NotEqual(annotation, contextualTip.Description);
            Assert.Contains(target.Title, contextualTip.Description);
        }
        finally
        {
            TestCelebrateMourningCombatState.ForceOutsideCombat = false;
        }
    }

    [Fact]
    public async Task Outside_run_hover_shows_annotation_without_return_targets()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        string annotation = new LocString("cards", celebrate.Id.Entry + ".annotation").GetFormattedText()
            ?? throw new InvalidOperationException("Celebrate Mourning annotation is missing.");

        TestCelebrateMourningCombatState.ForceOutsideRun = true;
        try
        {
            HoverTip contextualTip = Assert.Single(
                celebrate.HoverTips.OfType<HoverTip>().Where(tip =>
                    tip.Title == SquCommonL10n.AnnotationTitle().GetFormattedText()));
            Assert.Equal(annotation, contextualTip.Description);
        }
        finally
        {
            TestCelebrateMourningCombatState.ForceOutsideRun = false;
        }
    }

    [Fact]
    public async Task Returned_highest_card_reclaims_priority_over_the_former_second()
    {
        // A 被消耗后 B 成为最高；其它效果（例如变声期）把 A 放回抽牌堆后，只有再消耗 A 才会回手。
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel secondHighest = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet highest = await AddToHand<CutePet>();
        Assert.True(IsCurrentHighestPlayRate(highest));
        Assert.False(IsCurrentHighestPlayRate(secondHighest));

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), highest);

        AssertInPile(highest, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
        Assert.True(IsCurrentHighestPlayRate(secondHighest));
        Assert.False(IsCurrentHighestPlayRate(highest));
        await CardPileCmd.Add(celebrate, PileType.Discard);
        await CardPileCmd.Add(highest, PileType.Draw, CardPilePosition.Top);

        AssertInPile(highest, PileType.Draw);
        Assert.True(IsCurrentHighestPlayRate(highest));
        Assert.False(IsCurrentHighestPlayRate(secondHighest));

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), secondHighest);

        AssertInPile(secondHighest, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Discard);
        AssertInPile(highest, PileType.Draw);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), highest);

        AssertInPile(highest, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Hover_preview_dynamically_excludes_cards_already_in_exhaust_pile()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel lowerRate = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet higherRate = await AddToHand<CutePet>();

        string beforeExhaust = ReturnTriggerDescription(celebrate, higherRate.Title);
        Assert.Contains(higherRate.Title, beforeExhaust);
        Assert.DoesNotContain(lowerRate.Title, beforeExhaust);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), higherRate);

        string afterExhaust = ReturnTriggerDescription(celebrate, lowerRate.Title);
        Assert.DoesNotContain(higherRate.Title, afterExhaust);
        Assert.Contains(lowerRate.Title, afterExhaust);
    }

    [Theory]
    [InlineData(PileType.Hand)]
    [InlineData(PileType.Draw)]
    [InlineData(PileType.Discard)]
    public async Task Higher_rate_card_in_each_eligible_pile_blocks_return(PileType higherRatePile)
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel lowerRate = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet higherRate = await AddToHand<CutePet>();
        if (higherRatePile != PileType.Hand)
        {
            await CardPileCmd.Add(higherRate, higherRatePile);
        }

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), lowerRate);

        AssertInPile(lowerRate, PileType.Exhaust);
        AssertInPile(higherRate, higherRatePile);
        AssertInPile(celebrate, PileType.Discard);
    }

    [Fact]
    public async Task Higher_rate_card_in_play_pile_does_not_block_return()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel lowerRate = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet higherRate = await AddToHand<CutePet>();
        await CardPileCmd.Add(higherRate, PileType.Play);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), lowerRate);

        AssertInPile(higherRate, PileType.Play);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Theory]
    [InlineData(PileType.Draw)]
    [InlineData(PileType.Discard)]
    [InlineData(PileType.Exhaust)]
    public async Task Celebrate_mourning_listens_from_each_non_hand_supported_pile(PileType listenerPile)
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, listenerPile);
        CutePet highest = await AddToHand<CutePet>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), highest);

        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Celebrate_mourning_does_not_listen_from_play_pile()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Play);
        CutePet highest = await AddToHand<CutePet>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), highest);

        AssertInPile(celebrate, PileType.Play);
    }

    [Fact]
    public async Task Exhaust_result_remains_blocked_if_higher_rate_card_moves_before_hook_runs()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel lowerRate = await CreateHighestPartialPlayRateStrikeAsync();
        CutePet higherRate = await AddToHand<CutePet>();

        await CardPileCmd.Add(lowerRate, PileType.Exhaust);
        await CardPileCmd.Add(higherRate, PileType.Exhaust);
        await celebrate.AfterCardExhausted(
            new BlockingPlayerChoiceContext(),
            lowerRate,
            causedByEthereal: false);

        AssertInPile(celebrate, PileType.Discard);
    }

    [Fact]
    public async Task Exhaust_result_remains_triggered_if_higher_rate_card_appears_before_hook_runs()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CardModel exhausted = await CreateHighestPartialPlayRateStrikeAsync();

        await CardPileCmd.Add(exhausted, PileType.Exhaust);
        await AddToHand<CutePet>();
        await celebrate.AfterCardExhausted(
            new BlockingPlayerChoiceContext(),
            exhausted,
            causedByEthereal: false);

        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Same_card_can_trigger_return_after_being_exhausted_more_than_once()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        AllIn exhausted = await AddToHand<AllIn>();

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);
        AssertInPile(celebrate, PileType.Hand);
        await CardPileCmd.Add(celebrate, PileType.Discard);
        await CardPileCmd.Add(exhausted, PileType.Hand);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);

        AssertInPile(exhausted, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Ethereal_exhaust_uses_the_same_return_rule()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        AllIn exhausted = await AddToHand<AllIn>();

        await CardCmd.Exhaust(
            new BlockingPlayerChoiceContext(),
            exhausted,
            causedByEthereal: true);

        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Full_hand_keeps_returning_card_in_discard_pile()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await CardPileCmd.Add(celebrate, PileType.Discard);
        CutePet exhausted = await AddToHand<CutePet>();
        await CardPileCmd.Add(exhausted, PileType.Draw);
        while (PileType.Hand.GetPile(Player).Cards.Count < CardPile.MaxCardsInHand)
        {
            await AddCardToPile<DefendIronclad>(PileType.Hand);
        }

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);

        Assert.Equal(CardPile.MaxCardsInHand, PileType.Hand.GetPile(Player).Cards.Count);
        AssertInPile(celebrate, PileType.Discard);
    }

    [Fact]
    public async Task Hover_preview_groups_duplicate_titles()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        AllIn first = await AddToHand<AllIn>();
        await AddToHand<AllIn>();

        string description = ReturnTriggerDescription(celebrate, first.Title);
        IHoverTip[] tips = celebrate.HoverTips.ToArray();

        Assert.Contains("× 2", description);
        Assert.Empty(tips.OfType<CardHoverTip>());
    }

    [Fact]
    public async Task Hover_preview_caps_text_without_creating_nested_card_nodes()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await AddToDrawPileTop<AllIn>();
        await AddToDrawPileTop<Audition>();
        await AddToDrawPileTop<BurnAfterReading>();
        await AddToDrawPileTop<BurnCity>();
        await AddToDrawPileTop<Circus>();
        await AddToDrawPileTop<Denied>();
        await AddToDrawPileTop<DigRaid>();
        await AddToDrawPileTop<EunuchScript>();
        await AddToDrawPileTop<FireNova>();

        List<IGrouping<string, CardModel>> groups = GetCurrentHighestCards()
            .GroupBy(card => card.Title, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToList();
        Assert.True(groups.Count > CelebrateMourning.MaxReturnTriggerDetailedCards);
        int hiddenCardCount = groups
            .Skip(CelebrateMourning.MaxReturnTriggerDetailedCards)
            .Sum(group => group.Count());
        LocString moreCards = new("cards", celebrate.Id.Entry + ".returnTriggerMoreCards");
        moreCards.Add("Cards", hiddenCardCount);
        string description = ReturnTriggerDescription(celebrate, groups[0].Key);
        IHoverTip[] tips = celebrate.HoverTips.ToArray();

        Assert.Contains(moreCards.GetFormattedText()!, description);
        Assert.Empty(tips.OfType<CardHoverTip>());
    }

    [Fact]
    public void Play_rate_history_uses_compact_json_and_migrates_legacy_v4_data()
    {
        const string firstKey = "CARD_A|u0|f1|enone";
        const string secondKey = "CARD_B|u1|f2|eENCHANTMENT:1";
        var firstCombat = new CardDrawPlayRateTracker.CombatSnapshot();
        firstCombat.Cards[firstKey] = new CardDrawPlayRateTracker.CardCombatStats
        {
            PlayFromHandCount = 2,
            HandEntryCount = 3,
        };
        var secondCombat = new CardDrawPlayRateTracker.CombatSnapshot();
        secondCombat.Cards[firstKey] = new CardDrawPlayRateTracker.CardCombatStats
        {
            PlayFromHandCount = 1,
            HandEntryCount = 4,
        };
        secondCombat.Cards[secondKey] = new CardDrawPlayRateTracker.CardCombatStats
        {
            PlayFromHandCount = 5,
            HandEntryCount = 6,
        };
        var state = new CardDrawPlayRateTracker.PlayerSaveState
        {
            RecentCombats = [firstCombat, secondCombat],
        };

        string compactJson = JsonSerializer.Serialize(state);
        Assert.Contains("\"n\":2", compactJson);
        Assert.Contains("\"h\":", compactJson);
        Assert.DoesNotContain(nameof(CardDrawPlayRateTracker.PlayerSaveState.RecentCombats), compactJson);
        Assert.DoesNotContain(nameof(CardDrawPlayRateTracker.CardCombatStats.PlayFromHandCount), compactJson);
        Assert.Equal(1, compactJson.Split(firstKey, StringSplitOptions.None).Length - 1);

        CardDrawPlayRateTracker.PlayerSaveState roundTripped =
            JsonSerializer.Deserialize<CardDrawPlayRateTracker.PlayerSaveState>(compactJson)!;
        Assert.Equal(2, roundTripped.RecentCombats.Count);
        Assert.Equal(2, roundTripped.RecentCombats[0].Cards[firstKey].PlayFromHandCount);
        Assert.Equal(4, roundTripped.RecentCombats[1].Cards[firstKey].HandEntryCount);
        Assert.Equal(5, roundTripped.RecentCombats[1].Cards[secondKey].PlayFromHandCount);

        const string legacyJson =
            "{\"RecentCombats\":[{\"Cards\":{\"CARD_A|u0|f1|enone\":{\"PlayFromHandCount\":7,\"HandEntryCount\":9}}}]}";
        CardDrawPlayRateTracker.PlayerSaveState migrated =
            JsonSerializer.Deserialize<CardDrawPlayRateTracker.PlayerSaveState>(legacyJson)!;
        Assert.Single(migrated.RecentCombats);
        Assert.Equal(7, migrated.RecentCombats[0].Cards[firstKey].PlayFromHandCount);
        Assert.Equal(9, migrated.RecentCombats[0].Cards[firstKey].HandEntryCount);
        Assert.DoesNotContain("RecentCombats", JsonSerializer.Serialize(migrated));
    }

    [Fact]
    public async Task Exhausting_a_family_card_created_by_never_had_these_returns_celebrate_mourning()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        AssertInPile(celebrate, PileType.Discard);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<NeverHadThese>());
        }

        AssertAllSelectionsConsumed(selector);
        BelovedPartner family = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<BelovedPartner>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<CutePet>());
        Assert.Single(PileType.Draw.GetPile(Player).Cards.OfType<NextOfKin>());

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), family);

        AssertInPile(family, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Hand);
    }

    [Fact]
    public async Task Highest_partial_deck_play_rate_card_does_not_return_celebrate_mourning_while_family_remains()
    {
        CelebrateMourning celebrate = await AddToHand<CelebrateMourning>();
        await Play(celebrate);
        CardModel strike = await CreateHighestPartialPlayRateStrikeAsync();
        await PlayerCmd.SetEnergy(1, Player);
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is BelovedPartner);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<NeverHadThese>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Contains(
            LivingFamilyCards(),
            card => card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Play);
        Assert.False(IsCurrentHighestPlayRate(strike));

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), strike);

        AssertInPile(strike, PileType.Exhaust);
        AssertInPile(celebrate, PileType.Discard);
        Assert.Contains(LivingFamilyCards(), card => card.Pile?.Type != PileType.Exhaust);
    }

    private async Task<CardModel> CreateHighestPartialPlayRateStrikeAsync()
    {
        CardModel strike = PileType.Hand.GetPile(Player).Cards
            .Concat(PileType.Draw.GetPile(Player).Cards)
            .Concat(PileType.Discard.GetPile(Player).Cards)
            .OfType<StrikeIronclad>()
            .First(card => card.DeckVersion is not null);

        if (strike.Pile?.Type != PileType.Discard)
        {
            await CardPileCmd.Add(strike, PileType.Discard);
        }

        await CardPileCmd.Add(strike, PileType.Hand);
        await PlayerCmd.SetEnergy(1, Player);
        await Play(strike, EnemyAt(0));
        await CardPileCmd.Add(strike, PileType.Hand);

        Assert.True(CardDrawPlayRateTracker.TryGetStats(
            Player,
            strike,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true,
            out int plays,
            out int entries));
        Assert.True(plays > 0 && plays < entries);
        Assert.True(CardDrawPlayRateTracker.IsAmongHighestPlayRateDeckCards(
            Player,
            strike,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true));
        return strike;
    }

    private bool IsCurrentHighestPlayRate(CardModel card)
    {
        List<CardModel> candidates =
            CardDrawPlayRateTracker.GetCelebrateMourningPlayRateCandidates(Player);
        return CardDrawPlayRateTracker.IsAmongHighestPlayRateCards(
            Player,
            card,
            candidates,
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true);
    }

    private HashSet<CardModel> GetCurrentHighestCards() =>
        CardDrawPlayRateTracker.GetHighestPlayRateCards(
            Player,
            CardDrawPlayRateTracker.GetCelebrateMourningPlayRateCandidates(Player),
            windowSize: CelebrateMourning.PlayRateWindow,
            includeCurrentCombat: true);

    private static string ReturnTriggerDescription(CelebrateMourning card, string expectedCardTitle)
    {
        HoverTip tip = Assert.Single(
            card.HoverTips.OfType<HoverTip>().Where(candidate =>
                candidate.Description?.Contains(expectedCardTitle, StringComparison.Ordinal) == true));
        return tip.Description
               ?? throw new InvalidOperationException("Celebrate Mourning return-trigger hover tip is missing.");
    }

    private IEnumerable<CardModel> LivingFamilyCards() =>
        Player.PlayerCombatState!.AllCards.Where(card =>
            card.DeckVersion is null
            && card is FamilyCardTemplate
            && card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Play);
}

public sealed class CelebrateMourningMultiplayerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer<Silent>(2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("celebrate-mourning-multiplayer");

    [Fact]
    public async Task Exhaust_snapshots_and_return_triggers_are_isolated_per_player()
    {
        Player ally = PlayerWithNetId(2);
        CelebrateMourning playerCelebrate = await AddToHand<CelebrateMourning>(Player);
        CelebrateMourning allyCelebrate = await AddToHand<CelebrateMourning>(ally);
        await Play(playerCelebrate);
        await Play(allyCelebrate);
        CutePet playerFamily = await AddToHand<CutePet>(Player);
        CutePet allyFamily = await AddToHand<CutePet>(ally);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), playerFamily);

        AssertInPile(playerCelebrate, PileType.Hand);
        AssertInPile(allyCelebrate, PileType.Discard);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), allyFamily);

        AssertInPile(playerCelebrate, PileType.Hand);
        AssertInPile(allyCelebrate, PileType.Hand);
    }

    [Fact]
    public async Task Players_can_capture_opposite_results_without_overwriting_each_other()
    {
        Player ally = PlayerWithNetId(2);
        CelebrateMourning playerCelebrate = await AddToHand<CelebrateMourning>(Player);
        CelebrateMourning allyCelebrate = await AddToHand<CelebrateMourning>(ally);
        await CardPileCmd.Add(playerCelebrate, PileType.Discard);
        await CardPileCmd.Add(allyCelebrate, PileType.Discard);
        AllIn playerExhausted = await AddToHand<AllIn>(Player);
        await AddToHand<CutePet>(Player);
        AllIn allyExhausted = await AddToHand<AllIn>(ally);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), playerExhausted);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), allyExhausted);

        AssertInPile(playerCelebrate, PileType.Discard);
        AssertInPile(allyCelebrate, PileType.Hand);
    }
}
