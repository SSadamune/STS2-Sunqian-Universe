using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CanIStopRunningTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("stop-running");
    private BlockingPlayerChoiceContext Context() => new();
    private Task Damage(int amount, ValueProp props = ValueProp.Unblockable | ValueProp.Unpowered) =>
        CreatureCmd.Damage(Context(), Player.Creature, amount, props, null, null);
    private int Temporary => Player.Creature.GetPower<TemporaryHealthPower>()?.Amount ?? 0;
    private async Task<StopRunningPower> PlayBuff(bool upgraded = false)
    {
        var card = await AddToHand<CanIStopRunning>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        await Play(card);
        AssertInPile(card, PileType.Discard);
        return Player.Creature.GetPower<StopRunningPower>()!;
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task Lasts_exactly_one_or_two_enemy_turns(bool upgraded, int turns)
    {
        var power = await PlayBuff(upgraded);
        Assert.Equal(turns, power.Amount);
        await power.AfterSideTurnEndLate(Context(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(turns, power.Amount);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        for (int left = turns; left > 0; left--)
        {
            if (Temporary > 0)
            {
                ((CombatState)Combat).CurrentSide = CombatSide.Player;
                await Player.Creature.GetPower<TemporaryHealthPower>()!.BeforeSideTurnStart(Context(), CombatSide.Player, [Player.Creature], Combat);
                ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
            }
            await Damage(4);
            Assert.True(Temporary > 0);
            await power.AfterSideTurnEndLate(Context(), CombatSide.Enemy, [EnemyAt(0)]);
            Assert.Equal(left - 1, Player.Creature.GetPower<StopRunningPower>()?.Amount ?? 0);
        }
        if (Temporary > 0)
            await Player.Creature.GetPower<TemporaryHealthPower>()!.LoseTemporaryHealth(Context(), Temporary);
        await Damage(3);
        Assert.Equal(0, Temporary);
    }

    [Theory]
    [InlineData(0, 6, 6)]
    [InlineData(10, 6, 4)]
    [InlineData(4, 10, 6)]
    public async Task Only_real_hp_loss_is_replaced_and_each_hit_is_resolved_separately(int existing, int loss, int expected)
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, 40);
        await PlayBuff();
        if (existing > 0) await PowerCmd.Apply<TemporaryHealthPower>(Context(), Player.Creature, existing, Player.Creature, null);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        await Damage(loss);
        Assert.Equal(expected, Temporary);
        Assert.Equal(40 - Math.Max(0, loss - existing), Player.Creature.CurrentHp - Temporary);
        int hp = Player.Creature.CurrentHp;
        await Damage(2);
        Assert.Equal(expected - 2, Temporary);
        Assert.Equal(hp - 2, Player.Creature.CurrentHp);
    }

    [Fact]
    public async Task Player_turn_loss_and_temporary_decay_do_not_trigger()
    {
        await PlayBuff();
        await Damage(5);
        Assert.Equal(0, Temporary);
        await PowerCmd.Apply<TemporaryHealthPower>(Context(), Player.Creature, 4, Player.Creature, null);
        await Player.Creature.GetPower<TemporaryHealthPower>()!.BeforeSideTurnStart(Context(), CombatSide.Player, [Player.Creature], Combat);
        Assert.Equal(0, Temporary);
    }

    [Fact]
    public async Task Block_and_tungsten_rod_reduce_the_amount_before_conversion()
    {
        await PlayBuff();
        Player.AddRelicInternal(ModelDb.Relic<TungstenRod>().ToMutable(), silent: true);
        ((CombatState)Combat).CurrentSide = CombatSide.Enemy;
        Player.Creature.GainBlockInternal(3);
        await Damage(10, ValueProp.Unpowered);
        Assert.Equal(6, Temporary);
    }

    [Fact]
    public async Task Repeated_plays_add_enemy_turn_duration()
    {
        await PlayBuff();
        await PlayBuff(true);
        Assert.Equal(3, Player.Creature.GetPower<StopRunningPower>()!.Amount);
    }
}
