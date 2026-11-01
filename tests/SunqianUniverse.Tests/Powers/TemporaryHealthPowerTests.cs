using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Powers;

public sealed class TemporaryHealthPowerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("temporary-health");

    [Fact]
    public async Task Gain_and_stacking_raise_current_hp_above_max_without_changing_max()
    {
        Creature owner = Player.Creature;
        int max = owner.MaxHp;
        await CreatureCmd.SetCurrentHp(owner, max);
        await Gain(owner, 10);
        await Gain(owner, 7);
        Assert.Equal(max + 17, owner.CurrentHp);
        Assert.Equal(max, owner.MaxHp);
        Assert.Equal(17, owner.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Theory]
    [InlineData(4, 6, 40)]
    [InlineData(10, 0, 40)]
    [InlineData(13, 0, 37)]
    public async Task Damage_consumes_temporary_hp_before_real_hp(int damage, int temporary, int real)
    {
        Creature owner = Player.Creature;
        await CreatureCmd.SetCurrentHp(owner, 40);
        await Gain(owner, 10);
        var results = await Damage(owner, damage);
        Assert.Equal(temporary, owner.GetPower<TemporaryHealthPower>()?.Amount ?? 0);
        Assert.Equal(real + temporary, owner.CurrentHp);
        Assert.Equal(damage, Assert.Single(results).UnblockedDamage);
    }

    [Fact]
    public async Task Block_is_consumed_before_temporary_hp()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        await Gain(owner, 10);
        await CreatureCmd.GainBlock(owner, 8, ValueProp.Unpowered, null);
        await Damage(owner, 5, ValueProp.Unpowered);
        Assert.Equal(10, owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(3, owner.Block);
        await Damage(owner, 7, ValueProp.Unpowered);
        Assert.Equal(6, owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(real + 6, owner.CurrentHp);
    }

    [Fact]
    public async Task Tungsten_rod_reduces_damage_and_turn_start_decay_once_each()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        AddRelic<TungstenRod>();
        await Gain(owner, 20);
        await Damage(owner, 6);
        Assert.Equal(15, owner.GetPower<TemporaryHealthPower>()!.Amount);
        await Decay(owner);
        Assert.Equal(10, owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(real + 10, owner.CurrentHp);
    }

    [Theory]
    [InlineData(false, 14)]
    [InlineData(true, 19)]
    public async Task Intangible_also_reduces_decay(bool intangible, int remaining)
    {
        Creature owner = Player.Creature;
        await Gain(owner, 20);
        if (intangible) await ApplyPower<IntangiblePower>(owner, 1);
        await Decay(owner);
        Assert.Equal(remaining, owner.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Fact]
    public async Task Buffer_prevents_decay_and_is_spent_without_losing_hp()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        await Gain(owner, 10);
        await ApplyPower<BufferPower>(owner, 1);
        await Decay(owner);
        Assert.Equal(10, owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(real + 10, owner.CurrentHp);
        Assert.Null(owner.GetPower<BufferPower>());
    }

    [Fact]
    public async Task Decay_is_capped_to_remaining_temporary_hp_and_ignores_block()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        await Gain(owner, 3);
        await CreatureCmd.GainBlock(owner, 20, ValueProp.Unpowered, null);
        await Decay(owner);
        Assert.Null(owner.GetPower<TemporaryHealthPower>());
        Assert.Equal(real, owner.CurrentHp);
        Assert.Equal(20, owner.Block);
    }

    [Fact]
    public async Task Own_turn_decay_not_other_sides_or_non_participating_turns()
    {
        Creature owner = Player.Creature;
        await Gain(owner, 20);
        var power = owner.GetPower<TemporaryHealthPower>()!;
        await power.BeforeSideTurnStart(new BlockingPlayerChoiceContext(), CombatSide.Enemy, [owner], Combat);
        await power.BeforeSideTurnStart(new BlockingPlayerChoiceContext(), CombatSide.Player, [], Combat);
        Assert.Equal(20, power.Amount);
        await Decay(owner);
        Assert.Equal(14, power.Amount);
    }

    [Fact]
    public async Task Monster_hp_can_exceed_max_and_decays_on_monster_turn()
    {
        Creature enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Gain(enemy, 10);
        Assert.Equal(hp + 10, enemy.CurrentHp);
        await Decay(enemy);
        Assert.Equal(hp + 4, enemy.CurrentHp);
        Assert.Equal(4, enemy.GetPower<TemporaryHealthPower>()!.Amount);
        await Damage(enemy, 8);
        Assert.Null(enemy.GetPower<TemporaryHealthPower>());
        Assert.Equal(hp - 4, enemy.CurrentHp);
    }

    [Fact]
    public async Task Healing_restores_only_real_hp_and_preserves_temporary_stacks()
    {
        Creature owner = Player.Creature;
        int max = owner.MaxHp;
        await CreatureCmd.SetCurrentHp(owner, max - 5);
        await Gain(owner, 10);
        await CreatureCmd.Heal(owner, 100, playAnim: false);
        Assert.Equal(max + 10, owner.CurrentHp);
        Assert.Equal(10, owner.GetPower<TemporaryHealthPower>()!.Amount);
        await CreatureCmd.Heal(owner, owner.MaxHp - owner.CurrentHp, playAnim: false);
        Assert.Equal(max + 10, owner.CurrentHp);
        await Damage(owner, 4);
        await CreatureCmd.Heal(owner, 100, playAnim: false);
        Assert.Equal(max + 6, owner.CurrentHp);
        Assert.Equal(6, owner.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Fact]
    public async Task Max_hp_change_keeps_temporary_hp_separate()
    {
        Creature owner = Player.Creature;
        await CreatureCmd.SetCurrentHp(owner, owner.MaxHp);
        await Gain(owner, 10);
        await CreatureCmd.SetMaxHp(owner, 30);
        Assert.Equal(40, owner.CurrentHp);
        Assert.Equal(10, owner.GetPower<TemporaryHealthPower>()!.Amount);
        await PowerCmd.Remove<TemporaryHealthPower>(owner);
        Assert.Equal(30, owner.CurrentHp);
    }

    [Fact]
    public async Task Hp_loss_triggers_starter_relic_and_rupture_even_when_real_hp_is_unchanged()
    {
        Creature owner = Player.Creature;
        var relic = AddRelic<DeathDeniedRelic>();
        await relic.BeforeCombatStart();
        ((CombatState)Combat).CurrentSide = CombatSide.Player;
        int real = owner.CurrentHp;
        await Gain(owner, 20);
        await ApplyPower<RupturePower>(owner, 2);
        await Damage(owner, 4);
        Assert.Equal(4, relic.HpLostThisCombat);
        Assert.Equal(2, owner.GetPower<StrengthPower>()!.Amount);
        await Decay(owner);
        Assert.Equal(10, relic.HpLostThisCombat);
        Assert.Equal(4, owner.GetPower<StrengthPower>()!.Amount);
        Assert.Equal(real + 10, owner.CurrentHp);
    }

    [Fact]
    public async Task Zero_actual_loss_triggers_no_starter_relic_recovery()
    {
        Creature owner = Player.Creature;
        var relic = AddRelic<DeathDeniedRelic>();
        await relic.BeforeCombatStart();
        AddRelic<TungstenRod>();
        await Gain(owner, 1);
        await Decay(owner);
        Assert.Equal(0, relic.HpLostThisCombat);
        Assert.Equal(1, owner.GetPower<TemporaryHealthPower>()!.Amount);
    }

    [Fact]
    public async Task Stack_reduction_uses_hp_loss_defenses_without_double_consumption()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        AddRelic<TungstenRod>();
        await Gain(owner, 10);
        await PowerCmd.ModifyAmount(new BlockingPlayerChoiceContext(),
            owner.GetPower<TemporaryHealthPower>()!, -6, null, null);
        Assert.Equal(5, owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.Equal(real + 5, owner.CurrentHp);
    }

    [Fact]
    public async Task Cleanup_discards_remaining_temporary_hp_even_below_max()
    {
        Creature owner = Player.Creature;
        await CreatureCmd.SetCurrentHp(owner, 20);
        await Gain(owner, 10);
        owner.Reset();
        Assert.Equal(20, owner.CurrentHp);
        Assert.Null(owner.GetPower<TemporaryHealthPower>());
    }

    [Fact]
    public async Task Native_hp_loss_card_uses_temporary_hp_and_triggers_hp_loss_effects()
    {
        Creature owner = Player.Creature;
        int real = owner.CurrentHp;
        await Gain(owner, 20);
        var card = await AddToHand<Bloodletting>();
        await Play(card);
        Assert.Equal(real, owner.CurrentHp - owner.GetPower<TemporaryHealthPower>()!.Amount);
        Assert.True(owner.GetPower<TemporaryHealthPower>()!.Amount < 20);
    }

    private T AddRelic<T>() where T : RelicModel
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        return relic;
    }

    private Task<TemporaryHealthPower?> Gain(Creature creature, int amount) =>
        PowerCmd.Apply<TemporaryHealthPower>(new BlockingPlayerChoiceContext(), creature,
            amount, Player.Creature, null);

    private Task Decay(Creature creature) =>
        creature.GetPower<TemporaryHealthPower>()!.BeforeSideTurnStart(
            new BlockingPlayerChoiceContext(), creature.Side, [creature], Combat);

    private Task<IEnumerable<DamageResult>> Damage(Creature creature, int amount,
        ValueProp props = ValueProp.Unblockable | ValueProp.Unpowered) =>
        CreatureCmd.Damage(new BlockingPlayerChoiceContext(), creature, amount, props, null, null);
}
