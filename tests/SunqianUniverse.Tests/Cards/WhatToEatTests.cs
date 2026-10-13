using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WhatToEatTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>().WithSeed("what-to-eat");

    [Fact]
    public async Task Upgrade_increases_draw_and_preserves_two_card_exhaust_contract()
    {
        var card = Upgrade(await AddToHand<WhatToEat>());
        Assert.Equal(WhatToEat.UpgradedDraw, card.DynamicVars.Cards.IntValue);
        Assert.Equal(2, WhatToEat.ExhaustCount);
    }
}
