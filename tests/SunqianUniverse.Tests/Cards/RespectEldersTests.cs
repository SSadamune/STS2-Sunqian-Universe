using MegaCrit.Sts2.Core.Commands;
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
    public async Task Upgraded_card_hits_three_times_at_exactly_half_health()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.SetMaxAndCurrentHp(enemy, 100);
        await CreatureCmd.SetCurrentHp(enemy, 50);
        int hp = enemy.CurrentHp;

        await Play(Upgrade(await AddToHand<RespectElders>()), enemy);

        Assert.Equal(
            hp - RespectElders.DamageAmount * RespectElders.UpgradedHitCount,
            enemy.CurrentHp);
    }

    [Fact]
    public async Task Full_health_target_is_outside_range_and_only_takes_one_hit()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<RespectElders>()), enemy);

        Assert.Equal(hp - RespectElders.DamageAmount, enemy.CurrentHp);
    }
}
