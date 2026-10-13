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
    public async Task Spent_vigor_charges_damage_while_the_card_waits_in_hand()
    {
        var thunder = Upgrade(await AddToHand<ThunderOnStillLake>());
        await ApplyPower<VigorPower>(Player.Creature, 3);

        await Play(await AddToHand<StrikeLongtao>(), EnemyAt(0));

        Assert.Equal(ThunderOnStillLake.UpgradedCost, thunder.EnergyCost.GetResolved());
        Assert.Equal(ThunderOnStillLake.UpgradedDamage + 3, thunder.DynamicVars.Damage.IntValue);
    }
}
