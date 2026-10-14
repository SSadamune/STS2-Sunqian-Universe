using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WaterFireInvincibleTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("water-fire-invincible");

    [Fact]
    public async Task Upgrade_uses_total_damage_for_burning_and_block_while_block_reduces_hp_loss()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.GainBlock(enemy, 5, ValueProp.Unpowered, null);
        int hp = enemy.CurrentHp;

        await Play(Upgrade(await AddToHand<WaterFireInvincible>()), enemy);

        int hpLoss = WaterFireInvincible.UpgradedDamage - 5;
        Assert.Equal(hp - hpLoss, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, WaterFireInvincible.UpgradedDamage);
        Assert.Equal(WaterFireInvincible.UpgradedDamage, Player.Creature.Block);
    }

    [Fact]
    public async Task Fully_blocked_attack_still_deals_damage_for_burning_and_block_effects()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.GainBlock(enemy, 100, ValueProp.Unpowered, null);
        int hp = enemy.CurrentHp;

        await Play(await AddToHand<WaterFireInvincible>(), enemy);

        Assert.Equal(hp, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, WaterFireInvincible.BaseDamage);
        Assert.Equal(WaterFireInvincible.BaseDamage, Player.Creature.Block);
    }
}
