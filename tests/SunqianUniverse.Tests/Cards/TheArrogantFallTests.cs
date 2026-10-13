using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TheArrogantFallTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("the-arrogant-fall");

    [Fact]
    public async Task Buffs_target_now_and_schedules_both_debuffs()
    {
        var enemy = EnemyAt(0);
        var card = Upgrade(await AddToHand<TheArrogantFall>());
        await Play(card, enemy);

        Assert.Equal(TheArrogantFall.BuffAmount, enemy.Block);
        CardTestAssertions.AssertPowerAmount<VigorPower>(enemy, TheArrogantFall.BuffAmount);
        CardTestAssertions.AssertPowerAmount<TheArrogantFallPower>(enemy, TheArrogantFallPower.DebuffAmountPerStack);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
    }
}
