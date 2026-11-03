using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using Squ.Relics;
using Squ.Audio;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class FenghuangRebirthRelicTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("fenghuang-rebirth");
    private FenghuangRebirthRelic Equip()
    {
        var relic = (FenghuangRebirthRelic)ModelDb.Relic<FenghuangRebirthRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        return relic;
    }

    [Fact]
    public async Task Obtaining_plays_totem_once_and_each_trigger_plays_one_random_character()
    {
        TestSquSfxPlayPatch.BeginCapture();
        try
        {
            // TestMode has no shop/map UI for RelicCmd.Obtain; invoke its acquisition hook.
            var relic = Equip();
            await relic.AfterObtained();
            Assert.Equal(new[] { SquSfx.FenghuangRebirthObtainedEvent }, TestSquSfxPlayPatch.Events);
            await relic.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
            Assert.Single(TestSquSfxPlayPatch.Events);
            await relic.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
            await relic.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
            Assert.Equal(3, TestSquSfxPlayPatch.Events.Count);
            foreach (string path in TestSquSfxPlayPatch.Events.Skip(1))
                Assert.Contains(path, new[] { SquSfx.FenghuangRebirthLubuEvent, SquSfx.FenghuangRebirthDiaochanEvent });
        }
        finally { TestSquSfxPlayPatch.EndCapture(); }
    }

    [Fact]
    public async Task Neutral_shop_relic_loses_hp_through_block_and_gains_fixed_block_each_turn()
    {
        var relic = Equip();
        Assert.Equal(RelicRarity.Shop, relic.Rarity);
        Assert.True(relic.IsAllowedInShops);
        Assert.Contains(ModelDb.RelicPool<SharedRelicPool>().AllRelics, item => item is FenghuangRebirthRelic);
        Assert.Equal("在你的回合开始时，失去[blue]1[/blue]点生命并获得[blue]7[/blue]点格挡。", relic.DynamicDescription.GetFormattedText());
        await CreatureCmd.GainBlock(Player.Creature, 5, ValueProp.Unpowered, null);
        await PowerCmd.Apply<DexterityPower>(new BlockingPlayerChoiceContext(), Player.Creature, 10, Player.Creature, null);
        var pet = (await CompanionSummons.SummonSpecific(new BlockingPlayerChoiceContext(), Player,
            CompanionSummons.ThirdBrotherId, 8, null)).Creature!;
        int hp = Player.Creature.CurrentHp;
        await relic.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        Assert.Equal(hp - 1, Player.Creature.CurrentHp);
        Assert.Equal(12, Player.Creature.Block);
        Assert.Equal(8, pet.CurrentHp);
        Assert.Equal(0, LifeContractSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        await relic.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        Assert.Equal(hp - 2, Player.Creature.CurrentHp);
        Assert.Equal(19, Player.Creature.Block);
    }

    [Fact]
    public async Task Side_start_does_not_trigger_before_player_start()
    {
        var relic = Equip();
        int hp = Player.Creature.CurrentHp;
        await relic.AfterSideTurnStart(CombatSide.Enemy, [EnemyAt(0)], Combat);
        await relic.AfterSideTurnStart(CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(0, Player.Creature.Block);
    }

    [Fact]
    public async Task Preventing_hp_loss_still_grants_block()
    {
        var relic = Equip();
        await PowerCmd.Apply<BufferPower>(new BlockingPlayerChoiceContext(), Player.Creature, 1, Player.Creature, null);
        int hp = Player.Creature.CurrentHp;
        await relic.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), Player);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(7, Player.Creature.Block);
    }

    [Fact]
    public async Task Real_turn_start_triggers_relic_after_old_block_is_cleared()
    {
        Equip();
        int hp = Player.Creature.CurrentHp;
        await CreatureCmd.GainBlock(Player.Creature, 10, ValueProp.Unpowered, null);
        await EndTurn();
        Assert.Equal(hp - 1, Player.Creature.CurrentHp);
        Assert.Equal(7, Player.Creature.Block);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Matches_vanilla_crimson_mantle_with_and_without_buffer(bool buffered)
    {
        var context = new BlockingPlayerChoiceContext();
        var mantle = await PowerCmd.Apply<CrimsonMantlePower>(context, Player.Creature, 7, Player.Creature, null);
        mantle!.IncrementSelfDamage();
        await CreatureCmd.GainBlock(Player.Creature, 5, ValueProp.Unpowered, null);
        await PowerCmd.Apply<DexterityPower>(context, Player.Creature, 10, Player.Creature, null);
        if (buffered) await PowerCmd.Apply<BufferPower>(context, Player.Creature, 1, Player.Creature, null);
        int startingHp = Player.Creature.CurrentHp;
        await mantle.AfterPlayerTurnStart(context, Player);
        int vanillaHp = Player.Creature.CurrentHp;
        int vanillaBlock = Player.Creature.Block;
        await CreatureCmd.SetCurrentHp(Player.Creature, startingHp);
        await CreatureCmd.LoseBlock(context, Player.Creature, Player.Creature.Block - 5, null);
        if (buffered) await PowerCmd.Apply<BufferPower>(context, Player.Creature, 1, Player.Creature, null);
        await Equip().AfterPlayerTurnStart(context, Player);
        Assert.Equal(vanillaHp, Player.Creature.CurrentHp);
        Assert.Equal(vanillaBlock, Player.Creature.Block);
    }

}
