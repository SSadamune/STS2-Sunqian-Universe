using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LoudSecretPlotTests : SunqianCombatTestSuite
{
    [Theory]
    [InlineData(typeof(GlimpseBeyond))]
    [InlineData(typeof(EnergySurge))]
    [InlineData(typeof(HuddleUp))]
    public async Task All_player_skills_replay_for_one_living_player_regardless_of_enemy_count(Type cardType)
    {
        var plot = await AddToHand<LoudSecretPlot>();
        Assert.DoesNotContain(CardKeyword.Ethereal, plot.Keywords);
        Assert.Equal(2, plot.EnergyCost.GetResolved());
        Upgrade(plot);
        Assert.Equal(2, plot.EnergyCost.GetResolved());
        Assert.DoesNotContain(CardKeyword.Ethereal, plot.Keywords);
        await Play(plot);
        var card = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(cardType)).ToMutable();
        card.Owner = Player;
        Assert.Equal(TargetType.AllAllies, card.TargetType);
        var power = Player.Creature.GetPower<LoudSecretPlotPower>()!;
        Assert.Equal(3, power.ModifyCardPlayCount(card, null, 1));
        await CreatureCmd.Kill(EnemyAt(1), force: true);
        Assert.Equal(3, power.ModifyCardPlayCount(card, null, 1));
    }

    [Fact]
    public async Task Zero_legal_enemy_targets_still_qualify()
    {
        await Play(await AddToHand<LoudSecretPlot>());
        var card = await AddToHand<HateTheHeavensStrike>();
        var power = Player.Creature.GetPower<LoudSecretPlotPower>()!;
        int firstHp = EnemyAt(0).CurrentHp;
        int secondHp = EnemyAt(1).CurrentHp;
        EnemyAt(0).SetCurrentHpInternal(0);
        EnemyAt(1).SetCurrentHpInternal(0);
        Assert.Equal(2, power.ModifyCardPlayCount(card, null, 1));
        EnemyAt(0).SetCurrentHpInternal(firstHp);
        EnemyAt(1).SetCurrentHpInternal(secondHp);
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("loud-secret-plot");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multi_target_attack_repeats_against_a_single_enemy(bool upgraded)
    {
        await CreatureCmd.Kill(EnemyAt(1), force: true);
        var plot = await AddToHand<LoudSecretPlot>();
        if (upgraded) Upgrade(plot);
        await Play(plot);
        CardTestAssertions.AssertPowerAmount<LoudSecretPlotPower>(Player.Creature, upgraded ? 2 : 1);
        var attack = await AddToHand<HateTheHeavensStrike>();
        attack.DynamicVars[ScryVar.VarName].BaseValue = 0;
        int hp = EnemyAt(0).CurrentHp;

        await Play(attack);

        Assert.Equal(hp - HateTheHeavensStrike.DamageAmount * (upgraded ? 3 : 2), EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Multi_target_attack_is_not_replayed_while_two_enemies_are_targetable()
    {
        var second = EnemyAt(1);
        await Play(await AddToHand<LoudSecretPlot>());
        var attack = await AddToHand<HateTheHeavensStrike>();
        attack.DynamicVars[ScryVar.VarName].BaseValue = 0;
        int firstHp = EnemyAt(0).CurrentHp;
        int secondHp = second.CurrentHp;

        await Play(attack);

        Assert.Equal(firstHp - HateTheHeavensStrike.DamageAmount, EnemyAt(0).CurrentHp);
        Assert.Equal(secondHp - HateTheHeavensStrike.DamageAmount, second.CurrentHp);
    }
}

public sealed class LoudSecretPlotMultiplayerTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>(2)
            .AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("loud-secret-plot-allies");

    [Theory]
    [InlineData(typeof(GlimpseBeyond), false)]
    [InlineData(typeof(EnergySurge), false)]
    [InlineData(typeof(HuddleUp), false)]
    [InlineData(typeof(GlimpseBeyond), true)]
    [InlineData(typeof(EnergySurge), true)]
    [InlineData(typeof(HuddleUp), true)]
    public async Task All_player_skills_count_living_players_including_self(Type cardType, bool singleEnemy)
    {
        if (singleEnemy) await CreatureCmd.Kill(EnemyAt(1), force: true);
        await Play(await AddToHand<LoudSecretPlot>());
        var card = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(cardType)).ToMutable();
        card.Owner = Player;
        var power = Player.Creature.GetPower<LoudSecretPlotPower>()!;
        Assert.Equal(1, power.ModifyCardPlayCount(card, null, 1));
        // Only the live-target predicate is under test; avoid remote-player death UI in headless mode.
        var ally = Players.Single(player => player != Player).Creature;
        int allyHp = ally.CurrentHp;
        ally.SetCurrentHpInternal(0);
        Assert.Equal(2, power.ModifyCardPlayCount(card, null, 1));
        ally.SetCurrentHpInternal(allyHp);
    }
}
