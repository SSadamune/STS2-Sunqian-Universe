using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GrandGobletTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("grand-goblet");

    [Fact]
    public async Task Upgraded_power_grants_energy_and_vigor_at_the_next_side_start()
    {
        await Play(Upgrade(await AddToHand<GrandGoblet>()));
        var power = Assert.IsType<GrandGobletPower>(Player.Creature.GetPower<GrandGobletPower>());
        int energy = Player.PlayerCombatState!.Energy;

        await power.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Player.Creature.CombatState!);

        Assert.Equal(energy + GrandGoblet.EnergyGain, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, GrandGoblet.UpgradedVigor);
    }

    [Fact]
    public async Task Each_play_adds_one_future_energy_while_vigor_amounts_stack()
    {
        await PlayerCmd.SetEnergy(4, Player);
        await Play(await AddToHand<GrandGoblet>());
        await Play(Upgrade(await AddToHand<GrandGoblet>()));
        var power = Assert.IsType<GrandGobletPower>(Player.Creature.GetPower<GrandGobletPower>());
        int energy = Player.PlayerCombatState!.Energy;

        await power.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Player.Creature.CombatState!);

        Assert.Equal(energy + 2, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            GrandGoblet.BaseVigor + GrandGoblet.UpgradedVigor);
    }
}
