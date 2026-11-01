using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using Squ.Relics;
using Squ.Script;
using STS2RitsuLib.Models.Capabilities;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GruntScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("grunt-script");

    [Fact]
    public async Task Base_card_deals_eight_refunds_its_spent_vigor_and_activates_the_base_script()
    {
        Assert.Equal(8, GruntScript.DamageAmount);
        Assert.Equal(3, ScriptGruntPower.BaseBonusDamage);
        await ApplyPower<VigorPower>(Player.Creature, 4);
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;

        await Play(await AddToHand<GruntScript>(), enemy);

        Assert.Equal(hpBefore - GruntScript.DamageAmount - 4, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 4);
        var power = Assert.IsType<ScriptGruntPower>(
            Player.Creature.GetPower<ScriptGruntPower>());
        Assert.Equal(ScriptGruntPower.BaseBonusDamage, power.BonusDamage);
        Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<GruntScriptRecord>());
    }

    [Fact]
    public async Task Chicken_foot_cheese_strike_gets_grunt_bonus_on_both_damage_segments()
    {
        var boxLunch = (BoxLunchRelic)MegaCrit.Sts2.Core.Models.ModelDb
            .Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(boxLunch, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await Play(await AddToHand<NeutralAmbushScript>());
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<ChickenFootCheeseScript>());
        await Play(await AddToHand<GruntScript>(), EnemyAt(0));

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.NotNull(Player.Creature.GetPower<ScriptChickenFootCheesePower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptGruntPower>());

        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var strike = await AddToHand<StrikeIronclad>();
        var capability = Assert.IsType<ChickenFootCheeseStrikeCapability>(
            strike.Capability<ChickenFootCheeseStrikeCapability>());

        strike.DynamicVars.Damage.UpdateCardPreview(
            strike,
            CardPreviewMode.Normal,
            first,
            runGlobalHooks: true);
        capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
            .UpdateCardPreview(strike, CardPreviewMode.Normal, first, true);

        Assert.Equal(6 + ScriptGruntPower.BaseBonusDamage,
            strike.DynamicVars.Damage.PreviewValue);
        Assert.Equal(
            ScriptChickenFootCheesePower.BaseFollowUpDamage
                + ScriptGruntPower.BaseBonusDamage,
            capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
                .PreviewValue);

        await Play(strike);

        const int expectedDamage = (6 + ScriptGruntPower.BaseBonusDamage)
            + (ScriptChickenFootCheesePower.BaseFollowUpDamage
                + ScriptGruntPower.BaseBonusDamage);
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
    }

    [Fact]
    public async Task Upgraded_card_deals_eleven_and_activates_the_upgraded_script()
    {
        Assert.Equal(11, GruntScript.UpgradedDamageAmount);
        Assert.Equal(4, ScriptGruntPower.UpgradedBonusDamage);
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;
        var card = Upgrade(await AddToHand<GruntScript>());

        Assert.Equal(GruntScript.UpgradedDamageAmount, card.DynamicVars.Damage.BaseValue);
        await Play(card, enemy);

        Assert.Equal(hpBefore - GruntScript.UpgradedDamageAmount, enemy.CurrentHp);
        var power = Assert.IsType<ScriptGruntPower>(
            Player.Creature.GetPower<ScriptGruntPower>());
        Assert.Equal(ScriptGruntPower.UpgradedBonusDamage, power.BonusDamage);
        var record = Assert.IsType<GruntScriptRecord>(
            Assert.Single(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards));
        Assert.True(record.SourceUpgraded);
        Assert.EndsWith("+", record.Title);
    }
}
