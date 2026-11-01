using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PortalTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-portal");
    }

    [Fact]
    public async Task Portal_discards_selected_scry_cards_then_draws_and_auto_plays_the_revealed_attack()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int totalHpBefore = first.CurrentHp + second.CurrentHp;
        var drawnAttack = await AddToDrawPileTop<StrikeLongtao>();
        var firstDiscard = await AddToDrawPileTop<DefendLongtao>();
        var secondDiscard = await AddToDrawPileTop<DefendLongtao>();
        var portal = await AddToHand<Portal>();
        var selector = new ScriptedCardSelector().ThenSelect(firstDiscard, secondDiscard);

        using (UseCardSelector(selector))
        {
            await Play(portal);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(firstDiscard, PileType.Discard);
        AssertInPile(secondDiscard, PileType.Discard);
        Assert.Equal(
            totalHpBefore - StrikeLongtao.BaseDamage,
            first.CurrentHp + second.CurrentHp);
        AssertInPile(drawnAttack, PileType.Discard);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task Auto_played_attack_scripts_activate_and_replace_the_previous_script(
        bool cheese, bool upgraded, bool directAutoPlay)
    {
        await Play(await AddToHand<SunqianScript>());
        CardModel attackScript = cheese
            ? await AddToDrawPileTop<ChickenFootCheeseScript>()
            : await AddToDrawPileTop<GruntScript>();
        if (upgraded) Upgrade(attackScript);
        int hp = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        if (directAutoPlay)
        {
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), attackScript, null);
        }
        else
        {
            var selector = new ScriptedCardSelector().ThenSkip();
            using (UseCardSelector(selector))
                await Play(await AddToHand<Portal>());
            AssertAllSelectionsConsumed(selector);
        }

        Assert.True(EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp < hp);
        AssertInPile(attackScript, PileType.Exhaust);
        Assert.Null(Player.Creature.GetPower<ScriptSunqianPower>());
        var active = Assert.Single(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
        var records = ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards;
        if (cheese)
        {
            var power = Assert.IsType<ScriptChickenFootCheesePower>(active);
            Assert.Equal(upgraded ? 9 : 3, power.FollowUpDamage);
            Assert.Single(records.OfType<ChickenFootCheeseScriptRecord>());
        }
        else
        {
            var power = Assert.IsType<ScriptGruntPower>(active);
            Assert.Equal(upgraded ? 4 : 3, power.BonusDamage);
            Assert.Single(records.OfType<GruntScriptRecord>());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Drawing_watch_fire_from_shore_keeps_it_in_hand_without_playing_or_discarding_it(bool fullHand)
    {
        foreach (var existing in PileType.Hand.GetPile(Player).Cards.ToArray())
            await CardPileCmd.Add(existing, PileType.Discard);
        var revealed = await AddToDrawPileTop<WatchFireFromShore>();
        if (fullHand)
            for (int i = 0; i < 9; i++) await AddToHand<MegaCrit.Sts2.Core.Models.Cards.Wound>();
        var portal = await AddToHand<Portal>();
        var selector = new ScriptedCardSelector().ThenSkip();
        using (UseCardSelector(selector))
            await Play(portal);
        AssertAllSelectionsConsumed(selector);
        AssertInPile(revealed, PileType.Hand);
        Assert.Equal(0, Player.Creature.Block);
        Assert.All(new[] { EnemyAt(0), EnemyAt(1) }, enemy => Assert.Null(enemy.GetPower<BurningPower>()));
    }
}
