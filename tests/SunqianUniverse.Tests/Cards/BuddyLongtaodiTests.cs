using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BuddyLongtaodiTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("buddy-longtaodi");

    [Fact]
    public async Task Third_card_play_draws_the_upgraded_amount()
    {
        var secondDraw = await AddToDrawPileTop<DefendLongtao>();
        var firstDraw = await AddToDrawPileTop<StrikeLongtao>();
        await Play(Upgrade(await AddToHand<BuddyLongtaodi>()));
        await Play(await AddToHand<SendHorseWithoutHay>());
        await Play(await AddToHand<SendHorseWithoutHay>());

        AssertInPile(firstDraw, PileType.Hand);
        AssertInPile(secondDraw, PileType.Hand);
        CardTestAssertions.AssertPowerAmount<BuddyLongtaodiPower>(Player.Creature, BuddyLongtaodi.UpgradedDraw);
    }
}
