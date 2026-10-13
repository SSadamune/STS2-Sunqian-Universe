using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NotAfraidOfAcidTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("not-afraid-of-acid");

    [Fact]
    public async Task Upgrade_deals_damage_and_extends_acid_resistance()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<NotAfraidOfAcid>()), enemy);

        Assert.Equal(hp - NotAfraidOfAcid.UpgradedDamage, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<AcidEtchResistancePower>(Player.Creature, NotAfraidOfAcid.UpgradedDuration);
    }
}
