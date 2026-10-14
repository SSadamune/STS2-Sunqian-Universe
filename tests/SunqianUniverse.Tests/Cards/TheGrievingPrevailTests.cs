using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TheGrievingPrevailTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("the-grieving-prevail");

    [Fact]
    public async Task Applies_strength_loss_and_ritual_to_any_target()
    {
        var enemy = EnemyAt(0);
        var card = Upgrade(await AddToHand<TheGrievingPrevail>());
        await Play(card, enemy);

        CardTestAssertions.AssertPowerAmount<StrengthPower>(enemy, TheGrievingPrevail.StrengthLoss);
        CardTestAssertions.AssertPowerAmount<RitualPower>(enemy, TheGrievingPrevail.RitualAmount);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
    }
}
