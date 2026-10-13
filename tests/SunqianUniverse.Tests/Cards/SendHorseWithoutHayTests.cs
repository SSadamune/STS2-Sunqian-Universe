using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SendHorseWithoutHayTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-send-horse-without-hay");
    }

    [Fact]
    public async Task SendHorseWithoutHay_draws_three_after_three_energy_was_spent()
    {
        var drawnThird = await AddToDrawPileTop<DefendLongtao>();
        var drawnSecond = await AddToDrawPileTop<DefendLongtao>();
        var drawnFirst = await AddToDrawPileTop<DefendLongtao>();
        var horse = await AddToHand<SendHorseWithoutHay>();
        var threeCostCard = await AddToHand<TableFlip>();

        await Play(threeCostCard);

        Assert.Equal(SendHorseWithoutHay.ChargedDraw, horse.DynamicVars.Cards.IntValue);
        await Play(horse);

        AssertInPile(drawnFirst, PileType.Hand);
        AssertInPile(drawnSecond, PileType.Hand);
        AssertInPile(drawnThird, PileType.Hand);
        Assert.Equal(SendHorseWithoutHay.CanonicalDraw, horse.DynamicVars.Cards.IntValue);
    }
}
