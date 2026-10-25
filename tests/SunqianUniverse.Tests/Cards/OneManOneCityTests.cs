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
    public async Task Activation_spends_one_plating_and_immediately_triggers_the_remaining_amount()
    {
        var card = await AddToHand<OneManOneCity>();
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        await PlayAndActivateScript(card);
        var plating = Assert.IsType<PlatingPower>(Player.Creature.GetPower<PlatingPower>());

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.PlatingStacks - 1);
        Assert.Equal(OneManOneCity.PlatingStacks - 1, Player.Creature.Block);
        Assert.Null(card.Pile);
    }

    [Fact]
    public async Task Upgrade_applies_plating_and_installs_its_script()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<OneManOneCity>()));

        CardTestAssertions.AssertPowerAmount<PlatingPower>(
            Player.Creature,
            OneManOneCity.UpgradedPlatingStacks - 1);
        Assert.Equal(OneManOneCity.UpgradedPlatingStacks - 1, Player.Creature.Block);
        AssertOnlyScript<ScriptOneManOneCityPower>();
    }
}
