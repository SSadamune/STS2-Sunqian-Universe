using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BuddyHuTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("buddy-hu");

    [Fact]
    public async Task Converts_one_point_of_vigor_gain_to_strength()
    {
        await Play(await AddToHand<BuddyHu>());
        await ApplyPower<VigorPower>(Player.Creature, 4);

        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 3);
        CardTestAssertions.AssertPowerAmount<StrengthPower>(Player.Creature, 1);
    }

    [Fact]
    public async Task Upgrade_adds_innate()
    {
        var card = Upgrade(await AddToHand<BuddyHu>());
        Assert.Contains(MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Innate, card.Keywords);
    }
}
