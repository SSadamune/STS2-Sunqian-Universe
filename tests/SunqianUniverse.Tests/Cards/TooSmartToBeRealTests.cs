using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TooSmartToBeRealTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("too-smart-to-be-real");

    [Fact]
    public async Task Exhaust_card_played_after_the_power_increases_next_turn_hand_draw()
    {
        await Play(Upgrade(await AddToHand<TooSmartToBeReal>()));
        await Play(await AddToHand<TheGrievingPrevail>(), EnemyAt(0));

        await EndTurn();

        Assert.Equal(5 + TooSmartToBeReal.UpgradedDraw, PileType.Hand.GetPile(Player).Cards.Count);
    }

    [Fact]
    public async Task Upgrade_applies_two_points_of_conditional_draw_power()
    {
        await Play(Upgrade(await AddToHand<TooSmartToBeReal>()));
        CardTestAssertions.AssertPowerAmount<TooSmartToBeRealPower>(Player.Creature, TooSmartToBeReal.UpgradedDraw);
    }
}
