using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PhasingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("phasing");

    [Fact]
    public async Task Upgrade_rolls_temporary_dexterity_and_persists_upgraded_bounds()
    {
        await Play(Upgrade(await AddToHand<Phasing>()));

        var power = Assert.IsType<PhasingPower>(Player.Creature.GetPower<PhasingPower>());
        Assert.Equal(Phasing.BaseMinDexterity, power.MinBound);
        Assert.Equal(Phasing.UpgradedMaxDexterity, power.MaxBound);
        int dexterity = Player.Creature.GetPowerAmount<DexterityPower>();
        Assert.InRange(dexterity, Phasing.BaseMinDexterity, Phasing.UpgradedMaxDexterity);
    }
}
