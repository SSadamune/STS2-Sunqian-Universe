using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LittleThirdBigFourthTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("little-third-big-fourth");

    [Fact]
    public async Task Moves_one_card_of_each_cost_to_hand_and_deranges_their_costs()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Discard.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        CardModel[] cards =
        [
            await AddCardToPile<Anger>(PileType.Draw),
            await AddCardToPile<StrikeIronclad>(PileType.Draw),
            await AddCardToPile<Bash>(PileType.Discard),
        ];
        int[] originalCosts = cards.Select(card => card.EnergyCost.GetResolved()).ToArray();
        var source = Upgrade(await AddToHand<LittleThirdBigFourth>());

        await Play(source);

        Assert.All(cards, card => AssertInPile(card, PileType.Hand));
        Assert.Equal([0, 1, 2], cards.Select(card => card.EnergyCost.GetResolved()).Order().ToArray());
        for (int i = 0; i < cards.Length; i++)
        {
            Assert.NotEqual(originalCosts[i], cards[i].EnergyCost.GetResolved());
        }
        AssertInPile(source, PileType.Discard);
    }

    [Fact]
    public async Task Moves_only_one_card_from_each_duplicate_cost_group()
    {
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Discard.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        var first = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var duplicate = await AddCardToPile<DefendIronclad>(PileType.Discard);
        var distinct = await AddCardToPile<Bash>(PileType.Draw);

        await Play(await AddToHand<LittleThirdBigFourth>());

        Assert.Equal(1, new CardModel[] { first, duplicate }
            .Count(card => card.Pile?.Type == PileType.Hand));
        AssertInPile(distinct, PileType.Hand);
    }
}
