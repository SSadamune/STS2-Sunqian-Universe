using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class FireMeteorShowerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("fire-meteor-shower");

    [Fact]
    public async Task Energy_X_controls_hit_count_and_each_hit_burns_every_enemy()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int hp = first.CurrentHp + second.CurrentHp;
        int x = Player.PlayerCombatState!.Energy;

        await Play(await AddToHand<FireMeteorShower>());

        int actualTargets = Math.Min(x, 2);
        Assert.Equal(hp - FireMeteorShower.DamageAmount * actualTargets, first.CurrentHp + second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(first, FireMeteorShower.BurningStacks * actualTargets);
        CardTestAssertions.AssertPowerAmount<BurningPower>(second, FireMeteorShower.BurningStacks * actualTargets);
    }
}
