using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CatInTheBoxTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("cat-in-the-box");

    [Fact]
    public async Task Upgraded_roll_applies_doom_inside_declared_range()
    {
        var enemy = EnemyAt(0);
        await Play(Upgrade(await AddToHand<CatInTheBox>()), enemy);

        int doom = enemy.GetPowerAmount<DoomPower>();
        Assert.InRange(doom, CatInTheBox.UpgradedMinDoom, CatInTheBox.UpgradedMaxDoom);
    }
}
