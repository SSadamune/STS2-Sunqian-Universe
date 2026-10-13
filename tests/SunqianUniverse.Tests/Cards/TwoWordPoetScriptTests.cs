using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TwoWordPoetScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("two-word-poet-script");

    [Fact]
    public async Task Upgraded_script_hits_every_enemy_twice_and_snapshots_damage()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        await Play(Upgrade(await AddToHand<TwoWordPoetScript>()));

        int dealt = TwoWordPoetScript.CanonicalDamage * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp - dealt, second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(Player.Creature, TwoWordPoetScript.CanonicalDamage);
    }
}
