using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WatchFireFromShoreTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-watch-fire-from-shore");
    }

    [Fact]
    public async Task WatchFireFromShore_blocks_and_burns_two_distinct_enemies()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        var card = await AddToHand<WatchFireFromShore>();

        await Play(card);

        Assert.Equal(WatchFireFromShore.BaseBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<BurningPower>(first, WatchFireFromShore.BaseBurning);
        CardTestAssertions.AssertPowerAmount<BurningPower>(second, WatchFireFromShore.BaseBurning);
    }
}
