using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Monsters;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MutatedCentaurTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("mutated-centaur");

    [Fact]
    public async Task Retired_upgraded_card_summons_bound_mount_that_relays_hp_loss_at_150_percent()
    {
        var boundEnemy = EnemyAt(0);
        int enemyHp = boundEnemy.CurrentHp;

        await Play(Upgrade(await AddToHand<MutatedCentaur>()), boundEnemy);

        var mount = Assert.Single(boundEnemy.CombatState!.Enemies.Where(creature =>
            creature.Monster is MutatedCentaurMount));
        Assert.Equal((int)(boundEnemy.MaxHp * MutatedCentaur.MountHpRatio), mount.MaxHp);
        CardTestAssertions.AssertPowerAmount<MinionPower>(mount, 1);
        Assert.NotNull(mount.GetPower<MutatedCentaurLinkPower>());

        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            mount,
            4,
            ValueProp.Unpowered,
            null,
            null);

        Assert.Equal(enemyHp - 6, boundEnemy.CurrentHp);
    }

    [Fact]
    public async Task Mount_block_prevents_hp_loss_and_only_unblocked_hp_loss_is_relayed()
    {
        var boundEnemy = EnemyAt(0);
        int enemyHp = boundEnemy.CurrentHp;
        await Play(Upgrade(await AddToHand<MutatedCentaur>()), boundEnemy);
        var mount = Assert.Single(boundEnemy.CombatState!.Enemies.Where(creature =>
            creature.Monster is MutatedCentaurMount));
        await CreatureCmd.GainBlock(mount, 3, ValueProp.Unpowered, null);

        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            mount,
            4,
            ValueProp.Unpowered,
            null,
            null);

        Assert.Equal(mount.MaxHp - 1, mount.CurrentHp);
        Assert.Equal(enemyHp - 1, boundEnemy.CurrentHp);
    }
}
