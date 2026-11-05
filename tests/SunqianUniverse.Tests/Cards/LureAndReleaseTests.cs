using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LureAndReleaseTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("lure-and-release");

    [Fact]
    public async Task Card_is_a_one_cost_common_skill_and_upgrade_changes_the_candidate_count()
    {
        var card = await AddToHand<LureAndRelease>();

        Assert.Equal(1, card.EnergyCost.Canonical);
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Common, card.Rarity);
        Assert.Contains("随机3张牌", card.GetDescriptionForPile(PileType.Hand));

        Upgrade(card);

        Assert.Contains("随机4张牌", card.GetDescriptionForPile(PileType.Hand));
    }

    [Fact]
    public async Task Base_card_offers_three_random_draw_cards_and_choosing_one_puts_only_it_into_hand()
    {
        await ClearDrawPile();
        var drawCards = await AddDrawCards(5);
        CardModel[]? offered = null;
        CardModel? chosen = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            offered = options.ToArray();
            chosen = options[0];
            return [chosen];
        });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(offered);
        Assert.Equal(LureAndRelease.BaseCandidateCount, offered.Length);
        Assert.All(offered, card => Assert.Contains(drawCards, drawCard => ReferenceEquals(drawCard, card)));
        Assert.NotNull(chosen);
        AssertInPile(chosen, PileType.Hand);
        Assert.All(
            offered.Where(card => !ReferenceEquals(card, chosen)),
            card => AssertInPile(card, PileType.Discard));
        Assert.All(
            drawCards.Where(card => !offered.Any(offeredCard => ReferenceEquals(offeredCard, card))),
            card => AssertInPile(card, PileType.Draw));
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
    }

    [Fact]
    public async Task Upgraded_card_offers_four_random_draw_cards()
    {
        await ClearDrawPile();
        var drawCards = await AddDrawCards(5);
        CardModel[]? offered = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            offered = options.ToArray();
            return [options[0]];
        });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<LureAndRelease>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(offered);
        Assert.Equal(LureAndRelease.UpgradedCandidateCount, offered.Length);
        Assert.All(offered, card => Assert.Contains(drawCards, drawCard => ReferenceEquals(drawCard, card)));
    }

    [Fact]
    public async Task Fewer_draw_cards_than_requested_offers_every_remaining_candidate()
    {
        await ClearDrawPile();
        var first = await AddToDrawPileTop<StrikeIronclad>();
        var second = await AddToDrawPileTop<DefendIronclad>();
        CardModel[]? offered = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            offered = options.ToArray();
            return [options[0]];
        });

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<LureAndRelease>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(offered);
        Assert.Equal(2, offered.Length);
        Assert.Contains(offered, card => ReferenceEquals(card, first));
        Assert.Contains(offered, card => ReferenceEquals(card, second));
        Assert.Single(offered, card => card.Pile?.Type == PileType.Hand);
        Assert.Single(offered, card => card.Pile?.Type == PileType.Discard);
    }

    [Fact]
    public async Task Empty_draw_pile_skips_selection_and_delayed_effect()
    {
        await ClearDrawPile();
        var selector = new ScriptedCardSelector();

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
    }

    [Fact]
    public async Task Sly_auto_play_finishes_before_selection_and_removes_that_candidate()
    {
        await ClearDrawPile();
        var sly = await AddToDrawPileTop<Abrasive>();
        var first = await AddToDrawPileTop<StrikeIronclad>();
        var second = await AddToDrawPileTop<DefendIronclad>();
        CardModel[]? offered = null;
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            offered = options.ToArray();
            return [options[0]];
        });

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        AssertAllSelectionsConsumed(selector);
        CardTestAssertions.AssertPowerAmount<ThornsPower>(Player.Creature, 4);
        Assert.NotNull(offered);
        Assert.Equal(2, offered.Length);
        Assert.DoesNotContain(offered, card => ReferenceEquals(card, sly));
        Assert.Contains(offered, card => ReferenceEquals(card, first));
        Assert.Contains(offered, card => ReferenceEquals(card, second));
        Assert.Null(sly.Pile);
    }

    [Fact]
    public async Task Selection_is_skipped_and_no_delay_is_scheduled_when_sly_moves_every_candidate_out()
    {
        await ClearDrawPile();
        var first = await AddToDrawPileTop<Abrasive>();
        var second = await AddToDrawPileTop<Abrasive>();
        var third = await AddToDrawPileTop<Abrasive>();
        var selector = new ScriptedCardSelector();

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
        Assert.Null(first.Pile);
        Assert.Null(second.Pile);
        Assert.Null(third.Pile);
    }

    [Fact]
    public async Task Skipping_selection_gains_all_candidates_before_the_next_normal_draw()
    {
        await ClearDrawPile();
        var first = await AddToDrawPileTop<StrikeIronclad>();
        var second = await AddToDrawPileTop<DefendIronclad>();
        var third = await AddToDrawPileTop<Bash>();
        var selector = new ScriptedCardSelector().ThenSkip();

        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(Player.Creature.GetPower<LureAndReleasePower>());
        AssertInPile(first, PileType.Discard);
        AssertInPile(second, PileType.Discard);
        AssertInPile(third, PileType.Discard);
        var stillInDraw = await AddToDrawPileTop<Wound>();

        await Hook.BeforeHandDraw(Combat, Player, new BlockingPlayerChoiceContext());

        AssertInPile(first, PileType.Hand);
        AssertInPile(second, PileType.Hand);
        AssertInPile(third, PileType.Hand);
        AssertInPile(stillInDraw, PileType.Draw);
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
    }

    [Fact]
    public async Task Next_turn_gains_only_candidates_still_in_draw_or_discard()
    {
        await ClearDrawPile();
        var movedToHand = await AddToDrawPileTop<StrikeIronclad>();
        var movedToExhaust = await AddToDrawPileTop<DefendIronclad>();
        var movedToDraw = await AddToDrawPileTop<Bash>();
        var stillInDiscard = await AddToDrawPileTop<Anger>();
        var selector = new ScriptedCardSelector().ThenSkip();

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<LureAndRelease>()));
        }

        AssertAllSelectionsConsumed(selector);
        Assert.NotNull(Player.Creature.GetPower<LureAndReleasePower>());
        await CardPileCmd.Add(movedToHand, PileType.Hand);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), movedToExhaust);
        await CardPileCmd.Add(movedToDraw, PileType.Draw, CardPilePosition.Top);
        AssertInPile(stillInDiscard, PileType.Discard);

        await Hook.BeforeHandDraw(Combat, Player, new BlockingPlayerChoiceContext());

        AssertInPile(movedToHand, PileType.Hand);
        AssertInPile(movedToExhaust, PileType.Exhaust);
        AssertInPile(movedToDraw, PileType.Hand);
        AssertInPile(stillInDiscard, PileType.Hand);
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
    }

    [Fact]
    public async Task Next_turn_with_no_candidates_left_in_draw_or_discard_removes_power_without_a_prompt()
    {
        await ClearDrawPile();
        var first = await AddToDrawPileTop<StrikeIronclad>();
        var second = await AddToDrawPileTop<DefendIronclad>();
        var third = await AddToDrawPileTop<Bash>();
        var firstSelector = new ScriptedCardSelector().ThenSkip();

        using (UseCardSelector(firstSelector))
        {
            await Play(await AddToHand<LureAndRelease>());
        }

        AssertAllSelectionsConsumed(firstSelector);
        Assert.NotNull(Player.Creature.GetPower<LureAndReleasePower>());
        var context = new BlockingPlayerChoiceContext();
        await CardCmd.Exhaust(context, first);
        await CardCmd.Exhaust(context, second);
        await CardCmd.Exhaust(context, third);
        var selector = new ScriptedCardSelector();

        using (UseCardSelector(selector))
        {
            await Hook.BeforeHandDraw(Combat, Player, context);
        }

        Assert.Equal(0, selector.SelectionRequestCount);
        AssertInPile(first, PileType.Exhaust);
        AssertInPile(second, PileType.Exhaust);
        AssertInPile(third, PileType.Exhaust);
        Assert.Null(Player.Creature.GetPower<LureAndReleasePower>());
    }

    private async Task ClearDrawPile()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Discard);
    }

    private async Task<CardModel[]> AddDrawCards(int count)
    {
        var cards = new List<CardModel>();
        for (var index = 0; index < count; index++)
        {
            cards.Add(await AddToDrawPileTop<StrikeIronclad>());
        }

        return cards.ToArray();
    }
}
