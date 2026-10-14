using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class DigRaidTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("dig-raid");

    [Fact]
    public async Task Upgraded_card_routes_tinder_to_damage_and_vigor_to_burning()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await ApplyPower<TinderPower>(Player.Creature, 3);

        await Play(Upgrade(await AddToHand<DigRaid>()), enemy);

        Assert.Equal(hp - DigRaid.UpgradedDamage - 3 - 4, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, DigRaid.UpgradedBurning + 4 + 3);
    }
}
