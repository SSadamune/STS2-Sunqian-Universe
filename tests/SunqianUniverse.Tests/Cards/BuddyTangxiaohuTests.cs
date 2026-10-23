using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BuddyTangxiaohuTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("buddy-tangxiaohu");

    [Fact]
    public async Task Script_card_triggers_unpowered_block()
    {
        await Play(Upgrade(await AddToHand<BuddyTangxiaohu>()));
        await PlayAndActivateScript(await AddToHand<BowangParadoxScript>());

        Assert.Equal(BuddyTangxiaohu.UpgradedBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<BuddyTangxiaohuPower>(Player.Creature, BuddyTangxiaohu.UpgradedBlock);
    }
}
