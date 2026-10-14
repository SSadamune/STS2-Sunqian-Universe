using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HeadsmanScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-headsman-script");
    }

    [Fact]
    public async Task HeadsmanScript_applies_vigor_vulnerable_and_script_power()
    {
        var enemy = EnemyAt(0);
        var card = await AddToHand<HeadsmanScript>();

        await Play(card, enemy);

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            HeadsmanScript.BaseVigor);
        CardTestAssertions.AssertPowerAmount<VulnerablePower>(
            enemy,
            HeadsmanScript.VulnerableAmount);
        AssertOnlyScript<ScriptHeadsmanPower>();
    }
}
