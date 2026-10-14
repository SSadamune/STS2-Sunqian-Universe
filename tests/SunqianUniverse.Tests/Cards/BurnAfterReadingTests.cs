using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BurnAfterReadingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("burn-after-reading");

    [Fact]
    public async Task Playing_an_exhaust_card_grants_tinder()
    {
        await Play(Upgrade(await AddToHand<BurnAfterReading>()));
        await Play(await AddToHand<StandardBaseVehicle>());

        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, BurnAfterReading.UpgradedTinderStacks);
    }
}
