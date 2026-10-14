using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Commands;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LoudSecretPlotTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("loud-secret-plot");

    [Fact]
    public async Task Multi_target_attack_resolves_twice_against_a_single_enemy()
    {
        await CreatureCmd.Kill(EnemyAt(1), force: true);
        await Play(Upgrade(await AddToHand<LoudSecretPlot>()));
        CardTestAssertions.AssertPowerAmount<LoudSecretPlotPower>(Player.Creature, 1);
        var attack = await AddToHand<HateTheHeavensStrike>();
        attack.DynamicVars[ScryVar.VarName].BaseValue = 0;
        int hp = EnemyAt(0).CurrentHp;

        await Play(attack);

        Assert.Equal(hp - HateTheHeavensStrike.DamageAmount * 2, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Multi_target_attack_is_not_replayed_while_two_enemies_are_targetable()
    {
        var second = EnemyAt(1);
        await Play(await AddToHand<LoudSecretPlot>());
        var attack = await AddToHand<HateTheHeavensStrike>();
        attack.DynamicVars[ScryVar.VarName].BaseValue = 0;
        int firstHp = EnemyAt(0).CurrentHp;
        int secondHp = second.CurrentHp;

        await Play(attack);

        Assert.Equal(firstHp - HateTheHeavensStrike.DamageAmount, EnemyAt(0).CurrentHp);
        Assert.Equal(secondHp - HateTheHeavensStrike.DamageAmount, second.CurrentHp);
    }
}
