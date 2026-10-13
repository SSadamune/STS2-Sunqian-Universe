using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ExecutionCommanderScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-execution-commander-script");
    }

    [Fact]
    public async Task ExecutionCommanderScript_generates_two_salvos_and_applies_bonus()
    {
        var card = await AddToHand<ExecutionCommanderScript>();

        await Play(card);

        Assert.Equal(
            ExecutionCommanderScript.GeneratedStrikeCount,
            PileType.Hand.GetPile(Player).Cards.OfType<SalvoStrike>().Count());
        CardTestAssertions.AssertPowerAmount<ScriptExecutionCommanderPower>(
            Player.Creature,
            ExecutionCommanderScript.BaseBonusPercent);
        AssertOnlyScript<ScriptExecutionCommanderPower>();
    }
}
