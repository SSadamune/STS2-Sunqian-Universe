using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CrossTheRiverTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("cross-the-river");

    [Fact]
    public async Task Nonattacking_enemy_still_grants_upgraded_vigor_and_vulnerable_without_duration_power()
    {
        await Play(Upgrade(await AddToHand<CrossTheRiver>()));

        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, CrossTheRiver.UpgradedVigor);
        CardTestAssertions.AssertPowerAmount<VulnerablePower>(Player.Creature, CrossTheRiver.VulnerableAmount);
        Assert.Null(Player.Creature.GetPower<CrossTheRiverPower>());
    }

    [Fact]
    public async Task Active_power_duplicates_current_vigor_and_strength_for_attack_damage()
    {
        await ApplyPower<VigorPower>(Player.Creature, 3);
        await ApplyPower<StrengthPower>(Player.Creature, 2);
        await ApplyPower<CrossTheRiverPower>(Player.Creature, 1);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(hp - 16, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Duration_ticks_only_at_owners_side_end_and_expires_at_zero()
    {
        await ApplyPower<CrossTheRiverPower>(Player.Creature, 2);
        var power = Assert.IsType<CrossTheRiverPower>(Player.Creature.GetPower<CrossTheRiverPower>());

        await power.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Enemy,
            [EnemyAt(0)]);
        Assert.Equal(2, power.Amount);

        await power.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);
        Assert.Equal(1, power.Amount);

        await power.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);
        Assert.Null(Player.Creature.GetPower<CrossTheRiverPower>());
    }
}
