using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class OneManOneCityTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("one-man-one-city");

    [Fact]
    public async Task Upgrade_applies_plating_and_installs_its_script()
    {
        await Play(Upgrade(await AddToHand<OneManOneCity>()));

        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, OneManOneCity.UpgradedPlatingStacks);
        AssertOnlyScript<ScriptOneManOneCityPower>();
    }
}
