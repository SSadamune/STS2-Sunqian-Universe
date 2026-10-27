using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TwoWordPoetScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("two-word-poet-script");

    [Fact]
    public async Task Upgraded_activation_adds_consumed_vigor_to_each_hit_and_still_uses_current_strength()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        const int strength = 2;
        const int vigor = 3;
        await ApplyPower<StrengthPower>(Player.Creature, strength);
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        await PlayAndActivateScript(Upgrade(await AddToHand<TwoWordPoetScript>()));

        int storedDamage = TwoWordPoetScript.CanonicalDamage + vigor;
        int damagePerHit = storedDamage + strength;
        int dealt = damagePerHit * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp - dealt, second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(Player.Creature, storedDamage);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }

    [Fact]
    public async Task Replaying_adds_only_the_newly_consumed_vigor_to_the_existing_script_damage()
    {
        const int firstVigor = 3;
        const int secondVigor = 4;
        await ApplyPower<VigorPower>(Player.Creature, firstVigor);
        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>());

        TwoWordPoetScriptRecord record = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TwoWordPoetScriptRecord>());
        Assert.False(record.CanBeReinforced);
        Assert.Equal(TwoWordPoetScript.CanonicalDamage + firstVigor, record.StoredAmount);

        await ApplyPower<VigorPower>(Player.Creature, secondVigor);
        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>());
        Assert.Equal(
            TwoWordPoetScript.CanonicalDamage + firstVigor + secondVigor,
            record.StoredAmount);

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>());
        Assert.Equal(
            TwoWordPoetScript.CanonicalDamage + firstVigor + secondVigor,
            record.StoredAmount);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            record.StoredAmount);
    }

    [Fact]
    public async Task Lethality_affects_the_activation_but_is_not_stored_in_script_damage()
    {
        await ApplyPower<LethalityPower>(Player.Creature, 50);
        int hpBefore = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>());

        const int damagePerHit = 3;
        Assert.Equal(
            hpBefore - damagePerHit * TwoWordPoetScript.CanonicalHits,
            EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            TwoWordPoetScript.CanonicalDamage);
    }

    [Fact]
    public async Task Activation_is_a_powered_attack_and_respects_target_vulnerable()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;
        await ApplyPower<VulnerablePower>(first, 1, Player.Creature);
        await ApplyPower<VulnerablePower>(second, 1, Player.Creature);

        await PlayAndActivateScript(Upgrade(await AddToHand<TwoWordPoetScript>()));

        const int vulnerableDamagePerHit = 3;
        int dealt = vulnerableDamagePerHit * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp - dealt, second.CurrentHp);
    }

    [Fact]
    public async Task All_in_affects_the_immediate_activation_but_is_not_stored_in_script_damage()
    {
        await ApplyPower<AllInPower>(Player.Creature, (int)AllInPower.UpgradedBonusPercent);
        await PlayerCmd.SetEnergy(1, Player);
        int hpBefore = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>());

        const int damagePerHit = 3;
        Assert.Equal(
            hpBefore - damagePerHit * TwoWordPoetScript.CanonicalHits,
            EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            TwoWordPoetScript.CanonicalDamage);
    }
}
