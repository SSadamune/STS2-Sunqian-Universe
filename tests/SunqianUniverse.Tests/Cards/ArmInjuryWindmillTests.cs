using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ArmInjuryWindmillTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("arm-injury-windmill");

    [Fact]
    public async Task Upgraded_card_draws_three_and_adds_stats_and_wound()
    {
        CardModel[] draws = [await AddToDrawPileTop<DefendLongtao>(), await AddToDrawPileTop<StrikeLongtao>(), await AddToDrawPileTop<DefendLongtao>()];
        var card = Upgrade(await AddToHand<ArmInjuryWindmill>());

        await Play(card);

        Assert.All(draws, drawn => AssertInPile(drawn, PileType.Hand));
        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<Wound>());
        CardTestAssertions.AssertPowerAmount<TempDexPower>(Player.Creature, ArmInjuryWindmill.StatAmount);
        CardTestAssertions.AssertPowerAmount<TempStrFromArmInjuryWindmillPower>(Player.Creature, ArmInjuryWindmill.StatAmount);
    }
}
