using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class FlameStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-flame-strike-smoke");
    }

    [Fact]
    public async Task FlameStrike_deals_damage_and_applies_burning_twice()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<FlameStrike>();

        await Play(card, enemy);

        Assert.Equal(hpBefore - FlameStrike.BaseDamage, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(
            enemy,
            FlameStrike.BaseBurning * 2);
    }

    [Fact]
    public async Task Upgraded_FlameStrike_uses_both_upgraded_values()
    {
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        var card = Upgrade(await AddToHand<FlameStrike>());

        await Play(card, enemy);

        Assert.Equal(hpBefore - FlameStrike.UpgradedDamage, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, FlameStrike.UpgradedBurning * 2);
    }
}
