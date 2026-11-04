using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class SwordsmanshipReviewTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().WithSeed("swordsmanship-review");
    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 10)]
    public async Task Gains_block_and_upgrades_one_discarded_strike_then_puts_it_on_top(bool upgraded, int block)
    {
        var strike = await AddCardToPile<StrikeIronclad>(PileType.Discard);
        var nonStrike = await AddCardToPile<DefendLiubei>(PileType.Discard);
        var card = await AddToHand<SwordsmanshipReview>();
        if (upgraded) Upgrade(card);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Common, card.Rarity);
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Contains(strike, options);
            Assert.DoesNotContain(nonStrike, options);
            return [strike];
        });
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(block, Player.Creature.Block);
        Assert.True(strike.IsUpgraded);
        Assert.Same(strike, Player.PlayerCombatState!.DrawPile.Cards.First());
        AssertInPile(nonStrike, PileType.Discard);
    }
    [Fact]
    public async Task Already_upgraded_strikes_can_still_be_put_on_top()
    {
        var strike = await AddCardToPile<StrikeLiubei>(PileType.Discard);
        Upgrade(strike);
        var selector = new ScriptedCardSelector().ThenSelect(strike);
        using (UseCardSelector(selector)) await Play(await AddToHand<SwordsmanshipReview>());
        Assert.True(strike.IsUpgraded);
        Assert.Same(strike, Player.PlayerCombatState!.DrawPile.Cards.First());
    }
    [Fact]
    public async Task Empty_eligible_pile_still_gains_block_without_requesting_a_choice()
    {
        foreach (var card in Player.PlayerCombatState!.DiscardPile.Cards.ToArray()) await CardPileCmd.Add(card, PileType.Draw);
        await AddCardToPile<DefendLiubei>(PileType.Discard);
        var selector = new ScriptedCardSelector();
        using (UseCardSelector(selector)) await Play(await AddToHand<SwordsmanshipReview>());
        Assert.Equal(0, selector.SelectionRequestCount);
        Assert.Equal(7, Player.Creature.Block);
    }
}
