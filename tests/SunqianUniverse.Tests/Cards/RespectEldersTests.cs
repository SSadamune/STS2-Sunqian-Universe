using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RespectEldersTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("respect-elders");

    [Fact]
    public async Task Full_health_target_is_outside_range_and_only_takes_one_hit()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<RespectElders>()), enemy);

        Assert.Equal(hp - RespectElders.DamageAmount, enemy.CurrentHp);
    }
}
