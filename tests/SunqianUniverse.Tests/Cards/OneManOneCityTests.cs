using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    public async Task Activation_keeps_all_plating_and_grants_no_immediate_block()
    {
        var card = await AddToHand<OneManOneCity>();
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        await PlayAndActivateScript(card);

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.PlatingStacks);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Null(card.Pile);
    }

    [Fact]
    public async Task Active_script_doubles_plating_block_and_plating_decreases_by_two_per_turn()
    {
        await PlayAndActivateScript(await AddToHand<OneManOneCity>());
        var plating = Assert.IsType<PlatingPower>(Player.Creature.GetPower<PlatingPower>());
        var script = Assert.IsType<ScriptOneManOneCityPower>(
            Player.Creature.GetPower<ScriptOneManOneCityPower>());
        var choiceContext = new ThrowingPlayerChoiceContext();

        await plating.BeforeSideTurnEndEarly(
            choiceContext,
            CombatSide.Player,
            [Player.Creature]);
        await script.BeforeSideTurnEndEarly(
            choiceContext,
            CombatSide.Player,
            [Player.Creature]);

        Assert.Equal(OneManOneCity.PlatingStacks * 2, Player.Creature.Block);

        await EndTurn();

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.PlatingStacks - 2);
    }

    [Fact]
    public async Task Upgrade_applies_all_upgraded_plating_and_installs_its_script()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<OneManOneCity>()));

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.UpgradedPlatingStacks);
        Assert.Equal(0, Player.Creature.Block);
        AssertOnlyScript<ScriptOneManOneCityPower>();
    }
}
