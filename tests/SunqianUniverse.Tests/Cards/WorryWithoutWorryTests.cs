using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WorryWithoutWorryTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-worry-without-worry");
    }

    [Fact]
    public async Task UpgradedWorryWithoutWorry_grants_both_block_windows_and_blur()
    {
        var card = Upgrade(await AddToHand<WorryWithoutWorry>());

        await Play(card);

        Assert.Equal(WorryWithoutWorry.ImmediateBlock, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<BlockNextTurnPower>(
            Player.Creature,
            WorryWithoutWorry.NextTurnBlock);
        CardTestAssertions.AssertPowerAmount<BlurPower>(
            Player.Creature,
            WorryWithoutWorry.BlurAmount);
    }
}
