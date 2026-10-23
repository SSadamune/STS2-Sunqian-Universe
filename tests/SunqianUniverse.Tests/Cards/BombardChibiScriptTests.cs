using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Commands;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BombardChibiScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("bombard-chibi-script");

    [Fact]
    public async Task Upgrade_burns_and_temporarily_weakens_every_enemy_and_becomes_the_script()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<BombardChibiScript>()));

        foreach (var enemy in new[] { EnemyAt(0), EnemyAt(1) })
        {
            CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, BombardChibiScript.UpgradedBurning);
            CardTestAssertions.AssertPowerAmount<BombardChibiStrengthDownPower>(enemy, BombardChibiScript.UpgradedBurning);
        }
        AssertOnlyScript<ScriptBombardChibiPower>();
    }

    [Fact]
    public async Task Replacing_bombard_script_refunds_one_energy()
    {
        await PlayAndActivateScript(await AddToHand<BombardChibiScript>());
        await PlayerCmd.SetEnergy(2, Player);

        await PlayAndActivateScript(await AddToHand<ServantScript>());

        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        AssertOnlyScript<ScriptServantPower>();
    }
}
