using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HuaguMianzhangTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("huagu-mianzhang");

    [Fact]
    public async Task Damage_dealt_including_blocked_damage_becomes_doom_at_target_turn_start()
    {
        var enemy = EnemyAt(0);
        await CreatureCmd.GainBlock(enemy, 5, ValueProp.Unpowered, null);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<HuaguMianzhang>()), enemy);
        Assert.Equal(hp - (HuaguMianzhang.UpgradedDamage - 5), enemy.CurrentHp);
        var delayed = Assert.IsType<HuaguMianzhangPower>(enemy.GetPower<HuaguMianzhangPower>());

        await delayed.AfterSideTurnStart(CombatSide.Enemy, [enemy], enemy.CombatState!);

        CardTestAssertions.AssertPowerAmount<DoomPower>(enemy, HuaguMianzhang.UpgradedDamage);
    }
}
