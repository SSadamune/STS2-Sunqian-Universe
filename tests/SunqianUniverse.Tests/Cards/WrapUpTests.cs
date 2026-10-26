using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WrapUpTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("wrap-up");

    [Fact]
    public async Task Wrap_deactivates_scripts_and_grants_one_energy_instead_of_extra_vigor()
    {
        await ApplyPower<ScriptOneManOneCityPower>(Player.Creature, 1);
        await PlayerCmd.SetEnergy(0, Player);

        await Play(await AddToHand<WrapUp>());

        Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            WrapUp.BaseVigor);
    }

    [Fact]
    public async Task Upgrade_grants_five_vigor()
    {
        Assert.Equal(5, WrapUp.UpgradedVigor);

        await Play(Upgrade(await AddToHand<WrapUp>()));

        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            WrapUp.UpgradedVigor);
    }
}
