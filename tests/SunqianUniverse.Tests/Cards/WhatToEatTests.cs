using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WhatToEatTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>().WithSeed("what-to-eat");

    [Fact]
    public async Task With_only_one_other_card_available_exhausts_it_without_opening_a_selection_prompt()
    {
        foreach (var existing in PileType.Hand.GetPile(Player).Cards.ToArray())
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(existing, PileType.Discard);
        }
        var onlyChoice = await AddToHand<Wound>();

        await Play(await AddToHand<WhatToEat>());

        AssertInPile(onlyChoice, PileType.Exhaust);
    }

    [Fact]
    public async Task Upgrade_exhausts_two_selected_cards_draws_three_and_grants_upgraded_token_to_ally()
    {
        var ally = Players.Single(player => player != Player);
        var first = await AddToHand<Wound>();
        var second = await AddToHand<DefendIronclad>();
        CardModel[] draws =
        [
            await AddToDrawPileTop<StrikeIronclad>(),
            await AddToDrawPileTop<DefendIronclad>(),
            await AddToDrawPileTop<StrikeIronclad>(),
        ];
        var selector = new ScriptedCardSelector().ThenSelect(first, second);
        var card = Upgrade(await AddToHand<WhatToEat>());

        using (UseCardSelector(selector))
        {
            await Play(card);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(first, PileType.Exhaust);
        AssertInPile(second, PileType.Exhaust);
        Assert.All(draws, drawn => AssertInPile(drawn, PileType.Hand));
        var token = Assert.Single(PileType.Hand.GetPile(ally).Cards.OfType<ExactlyWhatToEat>());
        Assert.True(token.IsUpgraded);
        Assert.Equal(ExactlyWhatToEat.UpgradedDraw, token.DynamicVars.Cards.IntValue);
    }
}
