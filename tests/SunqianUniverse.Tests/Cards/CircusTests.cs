using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CircusTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-circus");
    }

    [Fact]
    public async Task Circus_deals_damage_and_grants_temporary_dexterity()
    {
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Circus>();

        await Play(card, enemy);

        Assert.Equal(hpBefore - Circus.CanonicalDamage, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(
            Player.Creature,
            Circus.CanonicalDexterity);
    }
}
