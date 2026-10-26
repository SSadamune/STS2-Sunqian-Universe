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

public sealed class SecurityGuardScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("security-guard-script");

    [Fact]
    public async Task Card_deals_eight_refunds_its_spent_vigor_and_activates_the_script()
    {
        await ApplyPower<VigorPower>(Player.Creature, 4);
        var enemy = EnemyAt(0);
        int hpBefore = enemy.CurrentHp;

        await Play(await AddToHand<SecurityGuardScript>(), enemy);

        Assert.Equal(hpBefore - SecurityGuardScript.DamageAmount - 4, enemy.CurrentHp);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 4);
        Assert.NotNull(Player.Creature.GetPower<ScriptSecurityGuardPower>());
        Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
                .OfType<SecurityGuardScriptRecord>());
    }

    [Fact]
    public async Task Chicken_foot_cheese_strike_gets_security_guard_bonus_on_both_damage_segments()
    {
        var boxLunch = (BoxLunchRelic)MegaCrit.Sts2.Core.Models.ModelDb
            .Relic<BoxLunchRelic>().ToMutable();
        Player.AddRelicInternal(boxLunch, silent: true);
        await PlayerCmd.SetEnergy(10, Player);

        await Play(await AddToHand<NeutralAmbushScript>());
        await Play(await AddToHand<ServantScript>());
        await Play(await AddToHand<ChickenFootCheeseScript>());
        await Play(await AddToHand<SecurityGuardScript>(), EnemyAt(0));

        Assert.Equal(2, ScriptSystem.GetActivationLimit(Player));
        Assert.NotNull(Player.Creature.GetPower<ScriptChickenFootCheesePower>());
        Assert.NotNull(Player.Creature.GetPower<ScriptSecurityGuardPower>());

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

        Assert.Equal(6 + ScriptSecurityGuardPower.BonusDamage,
            strike.DynamicVars.Damage.PreviewValue);
        Assert.Equal(
            ScriptChickenFootCheesePower.BaseFollowUpDamage
                + ScriptSecurityGuardPower.BonusDamage,
            capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
                .PreviewValue);

        await Play(strike);

        const int expectedDamage = (6 + ScriptSecurityGuardPower.BonusDamage)
            + (ScriptChickenFootCheesePower.BaseFollowUpDamage
                + ScriptSecurityGuardPower.BonusDamage);
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
    }
}
