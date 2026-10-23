using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    public async Task Script_spends_an_extra_plating_stack_and_grants_a_second_block_instance_at_turn_end()
    {
        await PlayAndActivateScript(await AddToHand<OneManOneCity>());
        var plating = Assert.IsType<PlatingPower>(Player.Creature.GetPower<PlatingPower>());
        var script = Assert.IsType<ScriptOneManOneCityPower>(
            Player.Creature.GetPower<ScriptOneManOneCityPower>());
        var context = new BlockingPlayerChoiceContext();

        await plating.BeforeSideTurnEndEarly(
            context,
            CombatSide.Player,
            [Player.Creature]);
        await script.BeforeSideTurnEnd(
            context,
            CombatSide.Player,
            [Player.Creature]);

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.PlatingStacks - 1);
        Assert.Equal(
            OneManOneCity.PlatingStacks + OneManOneCity.PlatingStacks - 1,
            Player.Creature.Block);
    }

    [Fact]
    public async Task Upgrade_applies_plating_and_installs_its_script()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<OneManOneCity>()));

        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, OneManOneCity.UpgradedPlatingStacks);
        AssertOnlyScript<ScriptOneManOneCityPower>();
    }
}
