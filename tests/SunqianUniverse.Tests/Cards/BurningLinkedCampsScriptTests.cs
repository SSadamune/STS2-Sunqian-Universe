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
    public async Task Upgraded_script_starts_at_twenty_and_material_reuse_accumulates_levels()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await PlayAndActivateScript(Upgrade(await AddToHand<BurningLinkedCampsScript>()));
        var record = Assert.Single(Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<BurningLinkedCampsScriptRecord>());
        Assert.True(record.UsesMaterialReuse);
        Assert.Equal(1, record.CurrentUpgradeLevel);
        Assert.Equal(20, Player.Creature.GetPower<ScriptBurningLinkedCampsPower>()!.Amount);
        await PlayAndActivateScript(await AddToHand<BurningLinkedCampsScript>());
        Assert.Equal(2, record.CurrentUpgradeLevel);
        Assert.Equal(30, Player.Creature.GetPower<ScriptBurningLinkedCampsPower>()!.Amount);
        await PlayAndActivateScript(Upgrade(await AddToHand<BurningLinkedCampsScript>()));
        Assert.Equal(4, record.CurrentUpgradeLevel);
        Assert.Equal(50, Player.Creature.GetPower<ScriptBurningLinkedCampsPower>()!.Amount);
        var active = Player.Creature.GetPower<ScriptBurningLinkedCampsPower>()!;
        var description = active.SmartDescription;
        description.Add("Amount", active.Amount);
        Assert.Contains("50%", description.GetFormattedText());
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Player.Creature, 3,
            ValueProp.Unpowered, Combat.CreateCard<Burn>(Player), null);
        Assert.Equal(150, Player.Creature.GetPower<PendingGoodFirePower>()!.Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Both_fire_damage_sources_scale_additively_and_prefer_existing_power(bool existing)
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<BurningLinkedCampsScript>()));
        var context = new BlockingPlayerChoiceContext();
        if (existing) await ApplyPower<GoodFirePower>(Player.Creature, 100);
        await CreatureCmd.Damage(context, Player.Creature, 3, ValueProp.Unpowered, Combat.CreateCard<Burn>(Player), null);
        var burning = await PowerCmd.Apply<BurningPower>(context, Player.Creature, 4, Player.Creature, null);
        await burning!.TriggerAsync(context, shouldCheckExtinguish: false);
        if (existing)
        {
            Assert.Equal(240, Player.Creature.GetPower<GoodFirePower>()!.Amount);
            Assert.Null(Player.Creature.GetPower<PendingGoodFirePower>());
        }
        else Assert.Equal(140, Player.Creature.GetPower<PendingGoodFirePower>()!.Amount);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Burning_with_burn_card_source_is_not_counted_twice()
    {
        await PlayAndActivateScript(await AddToHand<BurningLinkedCampsScript>());
        var context = new BlockingPlayerChoiceContext();
        var burning = await PowerCmd.Apply<BurningPower>(context, Player.Creature, 3, Player.Creature, null);
        await burning!.TriggerAsync(context, shouldCheckExtinguish: false, cardSource: Combat.CreateCard<Burn>(Player));
        Assert.Equal(30, Player.Creature.GetPower<PendingGoodFirePower>()!.Amount);
    }

    [Fact]
    public async Task Fully_blocked_burn_damage_accumulates_hidden_bonus_without_granting_tinder()
    {
        await PlayAndActivateScript(await AddToHand<BurningLinkedCampsScript>());
        int hp = Player.Creature.CurrentHp;
        await CreatureCmd.GainBlock(Player.Creature, 100, ValueProp.Unpowered, null);

        await EndTurn();

        Assert.Equal(hp, Player.Creature.CurrentHp);
        CardTestAssertions.AssertPowerAmount<PendingGoodFirePower>(
            Player.Creature,
            2 * ScriptBurningLinkedCampsPower.BurnBonusPercent);
        Assert.False(Player.Creature.GetPower<PendingGoodFirePower>()!.IsVisible);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Fully_blocked_burning_damage_increases_sigh_per_point_without_granting_tinder()
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
        CardTestAssertions.AssertPowerAmount<PendingGoodFirePower>(Player.Creature, burning * ScriptBurningLinkedCampsPower.BurnBonusPercent);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }
}
