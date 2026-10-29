using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Combat;

public sealed class VigorRefundStackingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("vigor-refund-stacking");

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(false, true, true, true)]
    [InlineData(true, true, true, true)]
    public async Task Each_effect_independently_refunds_the_spent_vigor_on_a_kill(
        bool dancing, bool grunt, bool headsman, bool headsmanFirst)
    {
        await Prepare(dancing, headsman, headsmanFirst);
        var victim = EnemyAt(0);
        await CreatureCmd.SetCurrentHp(victim, 1);
        if (grunt) await Play(await AddToHand<GruntScript>(), victim);
        else await Play(await AddToHand<StrikeIronclad>(), victim);
        int refunds = (dancing ? 1 : 0) + (grunt ? 1 : 0) + (headsman ? 1 : 0);
        Assert.True(victim.IsDead);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 4 * refunds);
        if (dancing) Assert.Null(Player.Creature.GetPower<KeepVigorPower>());
        Assert.Equal(headsman ? 10 : 10 - (grunt ? 2 : 1), Player.PlayerCombatState!.Energy);
    }

    [Fact]
    public async Task Headsman_does_not_add_a_refund_without_a_kill()
    {
        await Prepare(dancing: true, headsman: true, headsmanFirst: true);
        await Play(await AddToHand<GruntScript>(), EnemyAt(0));
        Assert.True(EnemyAt(0).IsAlive);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 8);
        Assert.Equal(8, Player.PlayerCombatState!.Energy);
    }

    private async Task Prepare(bool dancing, bool headsman, bool headsmanFirst)
    {
        await PlayerCmd.SetEnergy(20, Player);
        if (headsman && headsmanFirst) await Play(await AddToHand<HeadsmanScript>(), EnemyAt(1));
        if (dancing) await Play(await AddToHand<KeepPlayingKeepDancing>());
        if (headsman && !headsmanFirst) await Play(await AddToHand<HeadsmanScript>(), EnemyAt(1));
        await PowerCmd.Remove(Player.Creature.GetPower<VigorPower>()!);
        await ApplyPower<VigorPower>(Player.Creature, 4);
        await PlayerCmd.SetEnergy(10, Player);
    }
}
