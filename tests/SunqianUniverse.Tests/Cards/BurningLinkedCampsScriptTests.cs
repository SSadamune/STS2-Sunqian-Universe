using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BurningLinkedCampsScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("burning-linked-camps");

    [Fact]
    public async Task Replacing_upgraded_script_generates_burn_and_upgraded_shangfanggu_sigh()
    {
        await Play(Upgrade(await AddToHand<BurningLinkedCampsScript>()));
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            BurningLinkedCampsScript.UpgradedTinderStacks);

        await Play(await AddToHand<ServantScript>());

        Assert.Contains(PileType.Hand.GetPile(Player).Cards, card => card is Burn);
        Assert.Contains(
            PileType.Draw.GetPile(Player).Cards,
            card => card is ShangfangguSigh { IsUpgraded: true });
        AssertOnlyScript<ScriptServantPower>();
    }

    [Fact]
    public async Task Tinder_amplifies_all_burning_from_one_water_fire_play_and_its_block_uses_applied_amount()
    {
        await Play(Upgrade(await AddToHand<BurningLinkedCampsScript>()));
        var enemy = EnemyAt(0);

        await Play(await AddToHand<WaterFireInvincible>(), enemy);

        int expected = WaterFireInvincible.BaseDamage
            + BurningLinkedCampsScript.UpgradedTinderStacks;
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, expected);
        Assert.Equal(expected, Player.Creature.Block);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }
}
