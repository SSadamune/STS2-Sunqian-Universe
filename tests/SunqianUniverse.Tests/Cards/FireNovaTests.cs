using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class FireNovaTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-fire-nova");
    }

    [Fact]
    public async Task FireNova_hits_one_enemy_and_applies_burning_to_all()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int totalHpBefore = first.CurrentHp + second.CurrentHp;
        var card = await AddToHand<FireNova>();

        await Play(card);

        Assert.Equal(
            totalHpBefore - FireNova.DamageAmount,
            first.CurrentHp + second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<BurningPower>(first, FireNova.BurningStacks);
        CardTestAssertions.AssertPowerAmount<BurningPower>(second, FireNova.BurningStacks);
    }
}
