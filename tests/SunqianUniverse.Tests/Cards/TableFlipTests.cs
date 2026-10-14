using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TableFlipTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("table-flip");

    [Fact]
    public async Task Upgrade_reduces_cost_then_damages_and_weakens_all_enemies()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;
        var card = Upgrade(await AddToHand<TableFlip>());
        Assert.Equal(2, card.EnergyCost.GetResolved());

        await Play(card);

        Assert.Equal(firstHp - TableFlip.DamageAmount, first.CurrentHp);
        Assert.Equal(secondHp - TableFlip.DamageAmount, second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<WeakPower>(first, TableFlip.WeakAmount);
        CardTestAssertions.AssertPowerAmount<WeakPower>(second, TableFlip.WeakAmount);
    }
}
