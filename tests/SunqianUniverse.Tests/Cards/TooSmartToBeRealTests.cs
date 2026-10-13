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
    public async Task Upgrade_applies_two_points_of_conditional_draw_power()
    {
        await Play(Upgrade(await AddToHand<TooSmartToBeReal>()));
        CardTestAssertions.AssertPowerAmount<TooSmartToBeRealPower>(Player.Creature, TooSmartToBeReal.UpgradedDraw);
    }
}
