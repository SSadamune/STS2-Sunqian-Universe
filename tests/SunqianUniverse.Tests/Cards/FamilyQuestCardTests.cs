using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Cards;
using Squ.Interop;
using Squ.Powers;
using Squ.Tests.Events;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

[HarmonyPatch(typeof(NewsanguoPublicApiInterop), nameof(NewsanguoPublicApiInterop.AddHeavensForce))]
internal static class TestAddHeavensForcePatch
{
    public static int CallCount { get; private set; }

    public static int LastDelta { get; private set; }

    public static Player? LastPlayer { get; private set; }

    public static AbstractModel? LastSource { get; private set; }

    public static void Reset()
    {
        CallCount = 0;
        LastDelta = 0;
        LastPlayer = null;
        LastSource = null;
    }

    private static bool Prefix(
        Player? player,
        int delta,
        AbstractModel? source,
        ref Task __result)
    {
        if (TestNewsanguoInterop.Faction == TestNewsanguoFaction.None)
        {
            return true;
        }

        CallCount++;
        LastDelta = delta;
        LastPlayer = player;
        LastSource = source;
        __result = Task.CompletedTask;
        return false;
    }
}

public sealed class FamilyQuestCardTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("family-quest-cards");

    [Fact]
    public async Task Family_cards_show_each_keyword_once_and_skip_the_play_rate_explanation()
    {
        CardModel[] cards =
        [
            await AddToHand<CutePet>(),
            await AddToHand<BelovedPartner>(),
            await AddToHand<NextOfKin>(),
        ];

        foreach (CardModel card in cards)
        {
            Assert.True(card.ShouldShowInCardLibrary);
            string description = card.GetDescriptionForPile(PileType.Hand);
            foreach (CardKeyword keyword in card.Keywords)
            {
                HoverTip keywordTip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(keyword));
                string marker = "[gold]" + keywordTip.Title + "[/gold]";
                Assert.Equal(1, description.Split(marker).Length - 1);
            }

            Assert.Contains(
                card.HoverTips.OfType<HoverTip>(),
                tip => tip.Id.Contains("KEYWORD_FAMILY", StringComparison.Ordinal));
            foreach (CardKeyword keyword in card.Keywords)
            {
                string keywordTipId = HoverTipFactory.FromKeyword(keyword).Id;
                Assert.Contains(card.HoverTips, tip => tip.Id == keywordTipId);
            }
            Assert.DoesNotContain(
                card.HoverTips.OfType<HoverTip>(),
                tip => tip.Id.Contains(SquCommonL10n.PlayRateTitleKey, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Only_opted_in_family_cards_render_adjacent_leading_keywords_on_one_line()
    {
        (CardModel Card, CardKeyword[] LeadingKeywords)[] cases =
        [
            (await AddToHand<BelovedPartner>(),
            [SquKeywords.Family, CardKeyword.Sly]),
            (await AddToHand<NextOfKin>(),
            [SquKeywords.Family, CardKeyword.Retain]),
        ];

        foreach ((CardModel card, CardKeyword[] leadingKeywords) in cases)
        {
            string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
            string[] markers = leadingKeywords
                .Select(keyword =>
                {
                    HoverTip tip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(keyword));
                    return "[gold]" + tip.Title + "[/gold]";
                })
                .ToArray();

            string keywordLine = Assert.Single(
                lines.Where(line => markers.Any(marker => line.Contains(marker, StringComparison.Ordinal))));
            Assert.All(
                markers,
                marker => Assert.Contains(marker, keywordLine, StringComparison.Ordinal));
        }

        var cutePet = await AddToHand<CutePet>();
        Assert.IsNotAssignableFrom<IRowKeywordsCard>(cutePet);
        Assert.All(cases, item => Assert.IsAssignableFrom<IRowKeywordsCard>(item.Card));
    }

    [Fact]
    public async Task Cute_pet_is_unplayable_and_ethereal_and_summons_four_hp_when_exhausted()
    {
        var card = await AddToHand<CutePet>();
        int petsBefore = Combat.Creatures.Count(creature => creature.IsPet);

        Assert.Equal(-1, card.EnergyCost.Canonical);
        Assert.Contains(CardKeyword.Unplayable, card.Keywords);
        Assert.Contains(CardKeyword.Ethereal, card.Keywords);
        Assert.Contains(SquKeywords.Family, card.Keywords);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);

        AssertInPile(card, PileType.Exhaust);
        var pet = Assert.Single(Combat.Creatures.Where(creature => creature.IsPet).Skip(petsBefore));
        Assert.Equal(CutePet.SummonAmount, pet.MaxHp);
        Assert.Same(Player, pet.PetOwner);
    }

    [Fact]
    public async Task Beloved_partner_costs_two_and_grants_two_regen_and_three_tinder_when_exhausted()
    {
        var card = await AddToHand<BelovedPartner>();

        Assert.Equal(2, card.EnergyCost.Canonical);
        Assert.Contains(CardKeyword.Sly, card.Keywords);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.Contains(SquKeywords.Family, card.Keywords);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);

        CardTestAssertions.AssertPowerAmount<RegenPower>(Player.Creature, BelovedPartner.RegenAmount);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, BelovedPartner.TinderAmount);
        Assert.Equal(2, BelovedPartner.RegenAmount);
        Assert.Equal(4, BelovedPartner.TinderAmount);
    }

    [Fact]
    public async Task Next_of_kin_costs_one_and_draws_two_without_removing_its_linked_deck_card()
    {
        CardModel deckCard = Player.RunState.CreateCard<NextOfKin>(Player);
        await CardPileCmd.Add(deckCard, PileType.Deck);
        var card = await AddToHand<NextOfKin>();
        card.DeckVersion = deckCard;
        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;

        Assert.Equal(1, card.EnergyCost.Canonical);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.Contains(SquKeywords.Family, card.Keywords);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);

        Assert.Equal(drawBefore - NextOfKin.DrawAmount, PileType.Draw.GetPile(Player).Cards.Count);
        Assert.Contains(deckCard, Player.Deck.Cards);
        Assert.Same(deckCard, card.DeckVersion);
    }

    [Fact]
    public async Task Upgraded_family_cards_strengthen_their_exhaust_effects_once()
    {
        var cutePet = Upgrade(await AddToHand<CutePet>());
        var belovedPartner = Upgrade(await AddToHand<BelovedPartner>());
        var nextOfKin = Upgrade(await AddToHand<NextOfKin>());
        int petsBefore = Combat.Creatures.Count(creature => creature.IsPet);
        int drawBefore = PileType.Draw.GetPile(Player).Cards.Count;

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), cutePet);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), belovedPartner);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), nextOfKin);

        var pet = Assert.Single(Combat.Creatures.Where(creature => creature.IsPet).Skip(petsBefore));
        Assert.Equal(CutePet.UpgradedSummonAmount, pet.MaxHp);
        CardTestAssertions.AssertPowerAmount<RegenPower>(
            Player.Creature,
            BelovedPartner.UpgradedRegenAmount);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, BelovedPartner.TinderAmount);
        Assert.Equal(
            drawBefore - NextOfKin.UpgradedDrawAmount,
            PileType.Draw.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Normal_thinking_degrades_cleanly_without_newsanguo()
    {
        var card = await AddToHand<NormalThinking>();

        Assert.False(NewsanguoPublicApiInterop.IsReady);
        Assert.Equal(CardType.Curse, card.Type);
        Assert.Equal(CardRarity.Curse, card.Rarity);
        Assert.True(card.ShouldShowInCardLibrary);
        Assert.InRange((int)card.Rarity, (int)CardRarity.Event, (int)CardRarity.Quest);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.Contains(CardKeyword.Ethereal, card.Keywords);
        Assert.Contains(CardKeyword.Unplayable, card.Keywords);
        string description = card.GetDescriptionForPile(PileType.Hand);
        Assert.DoesNotContain("[purple]", description);
        foreach (CardKeyword keyword in card.Keywords)
        {
            HoverTip keywordTip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(keyword));
            string marker = "[gold]" + keywordTip.Title + "[/gold]";
            Assert.Equal(1, description.Split(marker).Length - 1);
        }

        await CardCmd.Exhaust(
            new BlockingPlayerChoiceContext(),
            card,
            causedByEthereal: true);

        AssertInPile(card, PileType.Exhaust);
    }

    [Fact]
    public void Normal_thinking_uses_curse_visuals_without_entering_the_random_curse_pool()
    {
        NormalThinking card = ModelDb.Card<NormalThinking>();
        CardPoolModel cursePool = ModelDb.CardPool<CurseCardPool>();
        CardPoolModel tokenPool = ModelDb.CardPool<TokenCardPool>();

        Assert.Contains(card, tokenPool.AllCards);
        Assert.DoesNotContain(card, cursePool.AllCards);
        Assert.Same(cursePool, card.VisualCardPool);
        Assert.Equal("card_frame_curse", card.VisualCardPool.CardFrameMaterialPath);
    }

    [Fact]
    public async Task Normal_thinking_loses_heavens_force_only_when_its_own_ethereal_exhausts_it()
    {
        TestNewsanguoInterop.Faction = TestNewsanguoFaction.CaoWei;
        TestAddHeavensForcePatch.Reset();
        try
        {
            var card = await AddToHand<NormalThinking>();
            var other = await AddToHand<CutePet>();

            await card.AfterCardExhausted(
                new BlockingPlayerChoiceContext(),
                other,
                causedByEthereal: true);
            await card.AfterCardExhausted(
                new BlockingPlayerChoiceContext(),
                card,
                causedByEthereal: false);
            Assert.Equal(0, TestAddHeavensForcePatch.CallCount);

            await card.AfterCardExhausted(
                new BlockingPlayerChoiceContext(),
                card,
                causedByEthereal: true);

            Assert.Equal(1, TestAddHeavensForcePatch.CallCount);
            Assert.Equal(-NormalThinking.HeavensForceLoss, TestAddHeavensForcePatch.LastDelta);
            Assert.Same(Player, TestAddHeavensForcePatch.LastPlayer);
            Assert.Same(card, TestAddHeavensForcePatch.LastSource);
            Assert.Contains("3", card.GetDescriptionForPile(PileType.Hand));
        }
        finally
        {
            TestAddHeavensForcePatch.Reset();
            TestNewsanguoInterop.Faction = TestNewsanguoFaction.None;
        }
    }
}
