using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
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

    [Fact]
    public async Task Wrap_asks_which_script_to_end_when_multiple_scripts_are_active()
    {
        var boxLunch = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(boxLunch, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await Play(await AddToHand<ExecutionCommanderScript>());
        await Play(await AddToHand<NeutralAmbushScript>());
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<SunqianScript>());
        Assert.Equal(2, Player.Creature.Powers.OfType<ScriptPowerTemplate>().Count());
        Assert.NotNull(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());

        await PlayerCmd.SetEnergy(0, Player);
        var selector = new ScriptedCardSelector()
            .ThenSelectWhere(card => card is ServantScriptRecord);
        using (UseCardSelector(selector))
        {
            await Play(await AddToHand<WrapUp>());
        }
        AssertAllSelectionsConsumed(selector);

        Assert.Null(Player.Creature.GetPower<ScriptServantPower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSunqianPower>());
        Assert.Single(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        Assert.Equal(1, Player.PlayerCombatState!.Energy);
    }
}
