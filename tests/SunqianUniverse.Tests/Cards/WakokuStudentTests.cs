using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WakokuStudentTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddRemotePlayer<LiubeiCharacter>()
            .AddEnemy<BigDummy>().WithSeed("wakoku-student");

    private Task Lose(int amount) => CreatureCmd.Damage(new BlockingPlayerChoiceContext(),
        Player.Creature, amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    private int Strength => Player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
    private int Dexterity => Player.Creature.GetPower<DexterityPower>()?.Amount ?? 0;

    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 7)]
    public async Task Thresholds_carry_remainder_and_healing_does_not_erase_progress(bool upgraded, int threshold)
    {
        var card = await AddToHand<WakokuStudent>();
        if (upgraded) Upgrade(card);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(CardType.Power, card.Type);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Contains(ModelDb.CardPool<LiubeiCardPool>().AllCards, c => c.Id == card.Id);
        await Play(card);
        var power = Player.Creature.GetPower<WakokuStudentPower>()!;
        Assert.Contains(threshold.ToString(), power.SmartDescription.GetFormattedText());
        Assert.DoesNotContain("{HpThreshold}", power.SmartDescription.GetFormattedText());
        await Lose(threshold - 1);
        Assert.Equal(0, Strength);
        await CreatureCmd.Heal(Player.Creature, 3);
        await Lose(threshold + 2);
        Assert.Equal(2, Strength);
        Assert.Equal(2, Dexterity);
        await Lose(threshold - 1);
        Assert.Equal(3, Strength);
        Assert.Equal(3, Dexterity);
    }

    [Fact]
    public async Task Counts_temporary_hp_but_ignores_blocked_damage_and_other_creatures()
    {
        await Play(await AddToHand<WakokuStudent>());
        await CreatureCmd.GainBlock(Player.Creature, 10, ValueProp.Move, null);
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Player.Creature,
            10, ValueProp.Move, null, null);
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), EnemyAt(0),
            9, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Combat.Players[1].Creature,
            9, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        Assert.Equal(0, Strength);
        await ApplyPower<TemporaryHealthPower>(Player.Creature, 9);
        await Lose(9);
        Assert.Equal(1, Strength);
        Assert.Equal(1, Dexterity);
    }

    [Fact]
    public async Task Doubling_uses_historical_gains_even_if_stats_were_later_removed()
    {
        var relic = ModelDb.Relic<DeathDeniedRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await CreatureCmd.SetCurrentHp(Player.Creature, 27);
        await Play(await AddToHand<WakokuStudent>());
        await Lose(9);
        await PowerCmd.ModifyAmount(new BlockingPlayerChoiceContext(),
            Player.Creature.GetPower<StrengthPower>()!, -1, null, null);
        await PowerCmd.ModifyAmount(new BlockingPlayerChoiceContext(),
            Player.Creature.GetPower<DexterityPower>()!, -1, null, null);
        await Lose(18);
        Assert.True(Player.Creature.IsAlive);
        Assert.Equal(5, Strength);
        Assert.Equal(5, Dexterity);
    }

    [Fact]
    public async Task Multiple_plays_have_independent_thresholds_and_start_times()
    {
        await Play(await AddToHand<WakokuStudent>());
        await Lose(8);
        var second = await AddToHand<WakokuStudent>();
        Upgrade(second);
        await Play(second);
        Assert.Equal(2, Player.Creature.Powers.OfType<WakokuStudentPower>().Count());
        await Lose(1);
        Assert.Equal(1, Strength);
        await Lose(6);
        Assert.Equal(2, Strength);
        Assert.Equal(2, Dexterity);
    }

    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 7)]
    public async Task First_lethal_loss_grants_threshold_stats_before_doubling(bool upgraded, int threshold)
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, threshold);
        Player.AddRelicInternal(ModelDb.Relic<DeathDeniedRelic>().ToMutable(), silent: true);
        var card = await AddToHand<WakokuStudent>();
        if (upgraded) Upgrade(card);
        string description = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains("你每损失", description);
        Assert.Contains("你受到致命的生命损失后，因此积累的属性翻倍。", description);
        await Play(card);
        await Lose(threshold);
        Assert.True(Player.Creature.IsAlive);
        Assert.Equal(2, Strength);
        Assert.Equal(2, Dexterity);
    }

    [Fact]
    public async Task Lethal_loss_doubles_only_own_history_and_can_double_again_after_revival()
    {
        var relic = ModelDb.Relic<DeathDeniedRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await ApplyPower<StrengthPower>(Player.Creature, 10);
        await ApplyPower<DexterityPower>(Player.Creature, 5);
        await Play(await AddToHand<WakokuStudent>());
        int cumulative = 0;
        int remainder = 0;
        for (int i = 0; i < 2; i++)
        {
            // Re-arm the test's revival source so both lethal events complete.
            await relic.BeforeCombatStart();
            await Lose(9);
            remainder += 9;
            cumulative += remainder / 9;
            remainder %= 9;
            int hp = Player.Creature.CurrentHp;
            remainder += hp;
            cumulative += remainder / 9;
            remainder %= 9;
            cumulative *= 2;
            await Lose(hp + 100);
            Assert.True(Player.Creature.IsAlive);
            Assert.Equal(10 + cumulative, Strength);
            Assert.Equal(5 + cumulative, Dexterity);
            await CreatureCmd.Heal(Player.Creature, 30);
        }
    }

    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 7)]
    public async Task Lethal_loss_below_threshold_still_doubles_accumulated_stats(bool upgraded, int threshold)
    {
        await CreatureCmd.SetCurrentHp(Player.Creature, threshold + 3);
        Player.AddRelicInternal(ModelDb.Relic<DeathDeniedRelic>().ToMutable(), silent: true);
        var card = await AddToHand<WakokuStudent>();
        if (upgraded) Upgrade(card);
        await Play(card);
        await Lose(threshold);
        Assert.Equal(1, Strength);
        Assert.Equal(1, Dexterity);
        await Lose(3);
        Assert.True(Player.Creature.IsAlive);
        Assert.Equal(2, Strength);
        Assert.Equal(2, Dexterity);
    }
}
