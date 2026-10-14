using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ThunderOnStillLakeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("thunder-on-still-lake");

    [Fact]
    public async Task Playing_charged_card_hits_twice_then_resets_damage_and_cost()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        var thunder = Upgrade(await AddToHand<ThunderOnStillLake>());
        await ApplyPower<VigorPower>(Player.Creature, 3);
        await Play(await AddToHand<StrikeLongtao>(), enemy);
        int hpAfterChargingAttack = enemy.CurrentHp;
        int chargedDamage = thunder.DynamicVars.Damage.IntValue;
        await PlayerCmd.GainEnergy(ThunderOnStillLake.UpgradedCost, Player);

        await Play(thunder);

        Assert.True(hpAfterChargingAttack < hp);
        Assert.Equal(
            hpAfterChargingAttack - chargedDamage * ThunderOnStillLake.CanonicalHits,
            enemy.CurrentHp);
        Assert.Equal(ThunderOnStillLake.UpgradedDamage, thunder.DynamicVars.Damage.IntValue);
        Assert.Equal(ThunderOnStillLake.UpgradedCost, thunder.EnergyCost.GetResolved());
    }

    [Fact]
    public async Task Spent_vigor_charges_damage_while_the_card_waits_in_hand()
    {
        var thunder = Upgrade(await AddToHand<ThunderOnStillLake>());
        await ApplyPower<VigorPower>(Player.Creature, 3);

        await Play(await AddToHand<StrikeLongtao>(), EnemyAt(0));

        Assert.Equal(ThunderOnStillLake.UpgradedCost, thunder.EnergyCost.GetResolved());
        Assert.Equal(ThunderOnStillLake.UpgradedDamage + 3, thunder.DynamicVars.Damage.IntValue);
    }
}
