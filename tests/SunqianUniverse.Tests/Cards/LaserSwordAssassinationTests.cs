using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LaserSwordAssassinationTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-laser-sword-assassination");
    }

    [Fact]
    public async Task LaserSwordAssassination_uses_intent_based_damage_multiplier()
    {
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        bool amplified = !SquEnemyIntent.IntendsToAttack(enemy);
        var card = await AddToHand<LaserSwordAssassination>();

        await Play(card, enemy);

        int multiplier = amplified
            ? LaserSwordAssassination.CanonicalDamageMultiplier
            : 1;
        Assert.Equal(
            hpBefore - LaserSwordAssassination.CanonicalDamage * multiplier,
            enemy.CurrentHp);
    }
}
