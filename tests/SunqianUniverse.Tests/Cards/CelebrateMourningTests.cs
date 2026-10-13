using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CelebrateMourningTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("celebrate-mourning");

    [Fact]
    public async Task Upgrade_grants_six_vigor_at_zero_cost()
    {
        int energy = Player.PlayerCombatState!.Energy;
        await Play(Upgrade(await AddToHand<CelebrateMourning>()));

        Assert.Equal(energy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, CelebrateMourning.UpgradedVigor);
    }
}
