using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TwoWordPoetScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("two-word-poet-script");

    [Fact]
    public async Task Upgraded_activation_hits_every_enemy_twice_with_snapshotted_strength_and_vigor()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        const int strength = 2;
        const int vigor = 3;
        await ApplyPower<StrengthPower>(Player.Creature, strength);
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        await PlayAndActivateScript(Upgrade(await AddToHand<TwoWordPoetScript>()));

        int snapshot = TwoWordPoetScript.CanonicalDamage + strength + vigor;
        int dealt = snapshot * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp - dealt, second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(Player.Creature, snapshot);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }
}
