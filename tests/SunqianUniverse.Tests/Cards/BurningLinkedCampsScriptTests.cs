using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using Squ.Character;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BurningLinkedCampsScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("burning-linked-camps");

    [Fact]
    public async Task Playing_upgraded_script_generates_burn_and_adds_upgraded_sigh_to_draw_pile()
    {
        var previousTop = await AddToDrawPileTop<Wound>();
        await PlayAndActivateScript(Upgrade(await AddToHand<BurningLinkedCampsScript>()));

        Assert.Contains(PileType.Hand.GetPile(Player).Cards, card => card is Burn);
        CardPile drawPile = PileType.Draw.GetPile(Player);
        var generated = Assert.Single(drawPile.Cards.OfType<ShangfangguSigh>());
        Assert.True(generated.IsUpgraded);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), generated.VisualCardPool);
        Assert.Contains(previousTop, drawPile.Cards);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
        AssertOnlyScript<ScriptBurningLinkedCampsPower>();
    }

    [Fact]
    public async Task Fully_blocked_burn_damage_still_grants_six_tinder()
    {
        await PlayAndActivateScript(await AddToHand<BurningLinkedCampsScript>());
        int hp = Player.Creature.CurrentHp;
        await CreatureCmd.GainBlock(Player.Creature, 100, ValueProp.Unpowered, null);

        await EndTurn();

        Assert.Equal(hp, Player.Creature.CurrentHp);
        CardTestAssertions.AssertPowerAmount<TinderPower>(
            Player.Creature,
            ScriptBurningLinkedCampsPower.BurnTinder);
    }

    [Fact]
    public async Task Fully_blocked_burning_damage_grants_tinder_equal_to_total_damage()
    {
        const int burning = 7;
        await PlayAndActivateScript(await AddToHand<BurningLinkedCampsScript>());
        await CreatureCmd.GainBlock(Player.Creature, 100, ValueProp.Unpowered, null);
        int hp = Player.Creature.CurrentHp;
        await ApplyPower<BurningPower>(Player.Creature, burning);
        var burningPower = Assert.IsType<BurningPower>(Player.Creature.GetPower<BurningPower>());

        await burningPower.TriggerAsync(
            new BlockingPlayerChoiceContext(),
            shouldCheckExtinguish: false);

        Assert.Equal(hp, Player.Creature.CurrentHp);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, burning);
    }
}
