using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class OneManOneCityTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("one-man-one-city");

    [Theory]
    [InlineData(false, 6)]
    [InlineData(true, 8)]
    public async Task Activation_immediately_triggers_plating_then_reduces_it_by_one(bool upgraded, int plating)
    {
        var card = await AddToHand<OneManOneCity>();
        if (upgraded) Upgrade(card);
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
        await Play(card);
        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, plating - 1);
        Assert.Equal(plating, Player.Creature.Block);
        Assert.Null(card.Pile);
        AssertOnlyScript<ScriptOneManOneCityPower>();
    }

    [Fact]
    public async Task Active_script_no_longer_adds_end_turn_block_or_extra_plating_decay()
    {
        await Play(await AddToHand<OneManOneCity>());
        var script = Player.Creature.GetPower<ScriptOneManOneCityPower>()!;
        await script.BeforeSideTurnEndEarly(new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature]);
        Assert.Equal(6, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, 5);
        await EndTurn();
        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, 4);
    }

    [Fact]
    public async Task Box_lunch_reactivation_only_triggers_remaining_plating_without_granting_new_stacks()
    {
        var relic = (BoxLunchRelic)ModelDb.Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(relic, silent: true);
        await Play(await AddToHand<OneManOneCity>());
        var selector = new ScriptedCardSelector().ThenSelectWhere(card => card is OneManOneCityScriptRecord);
        using (UseCardSelector(selector)) { Assert.True(await relic.ReselectFromScriptCollectionAsync()); }
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(11, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<PlatingPower>(Player.Creature, 4);
        Assert.Equal(0, relic.RemainingSwitchesThisTurn);
    }

    [Fact]
    public async Task Triggering_the_last_plating_stack_grants_one_block_and_removes_plating()
    {
        await ApplyPower<PlatingPower>(Player.Creature, 1);
        await ApplyPower<ScriptOneManOneCityPower>(Player.Creature, 1);
        Assert.Equal(1, Player.Creature.Block);
        Assert.Null(Player.Creature.GetPower<PlatingPower>());
    }

    [Fact]
    public async Task Activation_without_plating_does_not_grant_block_or_create_plating()
    {
        await ApplyPower<ScriptOneManOneCityPower>(Player.Creature, 1);
        Assert.Equal(0, Player.Creature.Block);
        Assert.Null(Player.Creature.GetPower<PlatingPower>());
    }
}
