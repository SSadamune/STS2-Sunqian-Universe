using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class FingerSnapStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-finger-snap-strike");
    }

    [Fact]
    public async Task FingerSnapStrike_deals_its_base_damage()
    {
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        var card = await AddToHand<FingerSnapStrike>();

        await Play(card, enemy);

        Assert.Equal(hpBefore - FingerSnapStrike.BaseDamage, enemy.CurrentHp);
    }
}
