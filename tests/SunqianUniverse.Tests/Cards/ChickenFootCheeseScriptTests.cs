using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ChickenFootCheeseScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-chicken-foot-cheese-script");
    }

    [Fact]
    public async Task ChickenFootCheeseScript_hits_two_enemies_twice_and_becomes_active()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var card = await AddToHand<ChickenFootCheeseScript>();

        await Play(card);

        int expectedDamage = ChickenFootCheeseScript.BaseDamage
            * ScriptChickenFootCheesePower.BaseHitCount;
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
        AssertOnlyScript<ScriptChickenFootCheesePower>();
    }
}
