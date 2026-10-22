using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CatInTheBoxTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("cat-in-the-box");

    [Fact]
    public async Task Upgrade_kills_a_target_at_its_maximum_doom_threshold_instead_of_applying_doom()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.SetCurrentHp(enemy, CatInTheBox.UpgradedMaxDoom);

        await Play(Upgrade(await AddToHand<CatInTheBox>()), enemy);

        Assert.True(enemy.IsDead);
        Assert.Null(enemy.GetPower<DoomPower>());
    }

    [Fact]
    public async Task Upgraded_roll_applies_doom_inside_declared_range()
    {
        var enemy = EnemyAt(0);
        await Play(Upgrade(await AddToHand<CatInTheBox>()), enemy);

        int doom = enemy.GetPowerAmount<DoomPower>();
        Assert.InRange(doom, CatInTheBox.UpgradedMinDoom, CatInTheBox.UpgradedMaxDoom);
    }

    [Fact]
    public async Task Kill_threshold_uses_coarse_stacks_and_is_identical_in_both_optional_mod_modes()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.SetCurrentHp(enemy, 30);
        await ApplyPower<PoisonPower>(enemy, 4);
        await ApplyPower<BurningPower>(enemy, 4);
        await ApplyPower<DoomPower>(enemy, 1);
        await ApplyPower<BufferPower>(enemy, 1);
        await ApplyPower<SlipperyPower>(enemy, 1);

        Assert.Equal(21, SquDoomKillThreshold.GetEffectiveGreenHp(enemy));
        Assert.Equal(4, Assert.IsType<PoisonPower>(
            enemy.GetPower<PoisonPower>()).CalculateTotalDamageNextTurn());

        await Play(Upgrade(await AddToHand<CatInTheBox>()), enemy);

        Assert.True(enemy.IsDead);
    }
}
