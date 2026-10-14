using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class AllInTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("all-in");

    [Fact]
    public async Task Upgraded_power_amplifies_damage_from_the_card_that_spends_the_last_energy()
    {
        await Play(Upgrade(await AddToHand<AllIn>()));
        CardTestAssertions.AssertPowerAmount<AllInPower>(
            Player.Creature,
            (int)AllInPower.UpgradedBonusPercent);
        await PlayerCmd.SetEnergy(1, Player);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<FlameStrike>(), EnemyAt(0));

        Assert.Equal(hp - 9, EnemyAt(0).CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0), 8);
    }

    [Fact]
    public async Task Card_that_does_not_spend_the_last_energy_gets_no_damage_or_burning_bonus()
    {
        await Play(Upgrade(await AddToHand<AllIn>()));
        await PlayerCmd.SetEnergy(2, Player);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<FlameStrike>(), EnemyAt(0));

        Assert.Equal(hp - FlameStrike.BaseDamage, EnemyAt(0).CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(
            EnemyAt(0),
            FlameStrike.BaseBurning * 2);
    }
}
