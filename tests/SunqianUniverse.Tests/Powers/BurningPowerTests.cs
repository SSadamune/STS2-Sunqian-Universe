using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Api;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Powers;

public sealed class BurningPowerTests : SunqianCombatTestSuite
{
    private const int BurningAmount = 4;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("burning-trigger-timing");

    [Fact]
    public void Unowned_preview_uses_generic_enemy_timing_without_throwing()
    {
        var preview = (BurningPower)ModelDb.Power<BurningPower>().ToMutable();

        Assert.EndsWith(".smartDescription", preview.SmartDescription.LocEntryKey);
    }

    [Fact]
    public async Task Enemy_triggers_at_turn_start_then_resolves_extinguish_state()
    {
        Creature enemy = EnemyAt(0);
        BurningPower burning = await ApplyProtectedBurning(enemy);
        int hpBefore = enemy.CurrentHp;

        Assert.EndsWith(".smartDescription", burning.SmartDescription.LocEntryKey);

        await burning.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Enemy,
            [enemy]);
        Assert.Equal(hpBefore, enemy.CurrentHp);
        AssertClearChance(burning, 25);

        await burning.AfterSideTurnStart(
            CombatSide.Enemy,
            [enemy],
            enemy.CombatState!);

        Assert.Equal(hpBefore - BurningAmount, enemy.CurrentHp);
        AssertClearChance(burning, 50);
        Assert.Null(enemy.GetPower<UnextinguishedPower>());
    }

    [Fact]
    public async Task Player_triggers_only_at_player_turn_end()
    {
        Creature player = Player.Creature;
        BurningPower burning = await ApplyProtectedBurning(player);
        int hpBefore = player.CurrentHp;

        Assert.EndsWith(".playerSmartDescription", burning.SmartDescription.LocEntryKey);

        await burning.AfterSideTurnStart(
            CombatSide.Player,
            [player],
            player.CombatState!);
        Assert.Equal(hpBefore, player.CurrentHp);

        await burning.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [player]);

        Assert.Equal(hpBefore - BurningAmount, player.CurrentHp);
        AssertClearChance(burning, 50);
    }

    [Fact]
    public async Task Pet_triggers_at_its_owners_turn_end()
    {
        var summon = await OstyCmd.Summon(
            new BlockingPlayerChoiceContext(),
            Player,
            12m,
            null);
        Creature pet = summon.Creature
            ?? throw new InvalidOperationException("Osty was not summoned.");
        BurningPower burning = await ApplyProtectedBurning(pet);
        int hpBefore = pet.CurrentHp;

        Assert.True(pet.IsPet);
        Assert.Same(Player, pet.PetOwner);
        Assert.EndsWith(".playerSmartDescription", burning.SmartDescription.LocEntryKey);

        await burning.AfterSideTurnStart(
            CombatSide.Player,
            [pet],
            pet.CombatState!);
        Assert.Equal(hpBefore, pet.CurrentHp);

        await burning.AfterSideTurnEnd(
            new BlockingPlayerChoiceContext(),
            CombatSide.Player,
            [Player.Creature]);

        Assert.Equal(hpBefore - BurningAmount, pet.CurrentHp);
        AssertClearChance(burning, 50);
    }

    [Fact]
    public async Task Public_api_trigger_resolves_damage_and_post_trigger_state_then_stack_gain_resets_chance()
    {
        Creature enemy = EnemyAt(0);
        BurningPower burning = await ApplyProtectedBurning(enemy);
        int hpBefore = enemy.CurrentHp;

        bool triggered = await SunqianUniversePublicApi.TriggerBurning(
            new BlockingPlayerChoiceContext(),
            enemy);

        Assert.True(triggered);
        Assert.Equal(hpBefore - BurningAmount, enemy.CurrentHp);
        AssertClearChance(burning, 50);
        Assert.Null(enemy.GetPower<UnextinguishedPower>());

        await ApplyPower<BurningPower>(enemy, 1);

        Assert.Equal(BurningAmount + 1, burning.Amount);
        AssertClearChance(burning, 25);
    }

    [Fact]
    public async Task Burning_damage_is_absorbed_by_block_before_hp()
    {
        Creature enemy = EnemyAt(0);
        BurningPower burning = await ApplyProtectedBurning(enemy);
        await CreatureCmd.GainBlock(enemy, 3, ValueProp.Unpowered, null);
        int hpBefore = enemy.CurrentHp;

        bool triggered = await SunqianUniversePublicApi.TriggerBurning(
            new BlockingPlayerChoiceContext(),
            enemy);

        Assert.True(triggered);
        Assert.Equal(0, enemy.Block);
        Assert.Equal(hpBefore - 1, enemy.CurrentHp);
        AssertClearChance(burning, 50);
    }

    [Fact]
    public async Task Public_api_can_skip_extinguish_and_apply_a_negative_chance_increment()
    {
        Creature enemy = EnemyAt(0);
        BurningPower burning = await ApplyProtectedBurning(enemy);
        int hpBefore = enemy.CurrentHp;

        bool triggered = await SunqianUniversePublicApi.TriggerBurning(
            new BlockingPlayerChoiceContext(),
            enemy,
            shouldCheckExtinguish: false,
            clearChanceIncrement: -0.1f);

        Assert.True(triggered);
        Assert.Equal(hpBefore - BurningAmount, enemy.CurrentHp);
        Assert.Same(burning, enemy.GetPower<BurningPower>());
        Assert.Equal(1, enemy.GetPower<UnextinguishedPower>()?.Amount);
        AssertClearChance(burning, 15);
    }

    [Fact]
    public async Task Iron_chain_copies_full_burning_damage_despite_block()
    {
        Creature first = EnemyAt(0);
        Creature second = EnemyAt(1);
        await ApplyProtectedBurning(first);
        await ApplyPower<IronChainPower>(first, 1);
        await CreatureCmd.GainBlock(first, 3, ValueProp.Unpowered, null);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;

        bool triggered = await SunqianUniversePublicApi.TriggerBurning(
            new BlockingPlayerChoiceContext(),
            first);

        Assert.True(triggered);
        Assert.Equal(0, first.Block);
        Assert.Equal(firstHpBefore - 5, first.CurrentHp);
        Assert.Equal(secondHpBefore - BurningAmount, second.CurrentHp);
    }

    private async Task<BurningPower> ApplyProtectedBurning(Creature target)
    {
        await ApplyPower<BurningPower>(target, BurningAmount);
        await ApplyPower<UnextinguishedPower>(target, 1);
        return Assert.IsType<BurningPower>(target.GetPower<BurningPower>());
    }

    private static void AssertClearChance(BurningPower burning, int expected) =>
        Assert.Equal(
            expected,
            burning.DynamicVars[BurningPower.ClearChanceVarName].IntValue);
}
