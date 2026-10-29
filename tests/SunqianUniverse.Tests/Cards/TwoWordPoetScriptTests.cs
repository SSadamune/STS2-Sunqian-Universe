using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mixed_upgrade_order_preserves_all_enemy_targeting_and_both_vigor_bonuses(bool upgradedFirst)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await ApplyPower<VigorPower>(Player.Creature, 3);
        var first = await AddToHand<TwoWordPoetScript>();
        if (upgradedFirst) Upgrade(first);
        await Play(first, EnemyAt(0));
        await ApplyPower<VigorPower>(Player.Creature, 4);
        var second = await AddToHand<TwoWordPoetScript>();
        if (!upgradedFirst) Upgrade(second);
        await Play(second, EnemyAt(0));
        var record = Assert.Single(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TwoWordPoetScriptRecord>());
        Assert.Equal(9, record.StoredAmount);
        Assert.True(record.IsUpgraded);
        var power = Player.Creature.GetPower<ScriptTwoWordPoetPower>()!;
        int firstHp = EnemyAt(0).CurrentHp;
        int secondHp = EnemyAt(1).CurrentHp;
        await ApplyPower<VigorPower>(Player.Creature, 7);
        await power.AfterSideTurnStart(CombatSide.Enemy, [EnemyAt(0), EnemyAt(1)], Combat);
        Assert.Equal(firstHp, EnemyAt(0).CurrentHp);
        await EndTurn();
        Assert.Equal(firstHp - 18, EnemyAt(0).CurrentHp);
        Assert.Equal(secondHp - 18, EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, 7);
        int hpAfterTurnTrigger = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;
        await record.ActivateAsync(new ThrowingPlayerChoiceContext(), Player);
        Assert.Equal(hpAfterTurnTrigger, EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
    }

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("two-word-poet-script");

    [Fact]
    public async Task Upgraded_play_deals_damage_with_vigor_and_strength_and_stores_only_vigor()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        const int strength = 2;
        const int vigor = 3;
        await ApplyPower<StrengthPower>(Player.Creature, strength);
        await ApplyPower<VigorPower>(Player.Creature, vigor);
        await PlayAndActivateScript(Upgrade(await AddToHand<TwoWordPoetScript>()), EnemyAt(0));

        int storedDamage = TwoWordPoetScript.CanonicalDamage + vigor;
        int damagePerHit = storedDamage + strength;
        int dealt = damagePerHit * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp, second.CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(Player.Creature, storedDamage);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }

    [Fact]
    public async Task Replaying_adds_only_the_newly_consumed_vigor_to_the_existing_script_damage()
    {
        const int firstVigor = 3;
        const int secondVigor = 4;
        await ApplyPower<VigorPower>(Player.Creature, firstVigor);
        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>(), EnemyAt(0));

        TwoWordPoetScriptRecord record = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TwoWordPoetScriptRecord>());
        Assert.False(record.CanBeReinforced);
        Assert.Equal(TwoWordPoetScript.CanonicalDamage + firstVigor, record.StoredAmount);

        await ApplyPower<VigorPower>(Player.Creature, secondVigor);
        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>(), EnemyAt(0));
        Assert.Equal(
            TwoWordPoetScript.CanonicalDamage + firstVigor + secondVigor,
            record.StoredAmount);

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>(), EnemyAt(0));
        Assert.Equal(
            TwoWordPoetScript.CanonicalDamage + firstVigor + secondVigor,
            record.StoredAmount);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            record.StoredAmount);
    }

    [Fact]
    public async Task Vigorous_enchantment_is_stored_and_applies_once_to_the_play_damage()
    {
        const int enchantedVigor = 6;
        var card = await AddToHand<TwoWordPoetScript>();
        Vigorous enchantment = Assert.IsType<Vigorous>(
            CardCmd.Enchant<Vigorous>(card, enchantedVigor));
        int hpBefore = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        await PlayAndActivateScript(card, EnemyAt(0));

        int storedDamage = TwoWordPoetScript.CanonicalDamage + enchantedVigor;
        Assert.Equal(
            hpBefore - storedDamage * TwoWordPoetScript.CanonicalHits,
            EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            storedDamage);
        TwoWordPoetScriptRecord record = Assert.Single(
            ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<TwoWordPoetScriptRecord>());
        Assert.Equal(storedDamage, record.StoredAmount);
        Assert.Equal(EnchantmentStatus.Disabled, enchantment.Status);
    }

    [Fact]
    public async Task Lethality_affects_the_play_damage_but_is_not_stored_in_script_damage()
    {
        await ApplyPower<LethalityPower>(Player.Creature, 50);
        int hpBefore = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>(), EnemyAt(0));

        const int damagePerHit = 3;
        Assert.Equal(
            hpBefore - damagePerHit * TwoWordPoetScript.CanonicalHits,
            EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            TwoWordPoetScript.CanonicalDamage);
    }

    [Fact]
    public async Task Play_damage_is_a_powered_attack_and_respects_target_vulnerable()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;
        await ApplyPower<VulnerablePower>(first, 1, Player.Creature);
        await ApplyPower<VulnerablePower>(second, 1, Player.Creature);

        await PlayAndActivateScript(Upgrade(await AddToHand<TwoWordPoetScript>()), EnemyAt(0));

        const int vulnerableDamagePerHit = 3;
        int dealt = vulnerableDamagePerHit * TwoWordPoetScript.CanonicalHits;
        Assert.Equal(firstHp - dealt, first.CurrentHp);
        Assert.Equal(secondHp, second.CurrentHp);
    }

    [Fact]
    public async Task All_in_affects_the_play_damage_but_is_not_stored_in_script_damage()
    {
        await ApplyPower<AllInPower>(Player.Creature, (int)AllInPower.UpgradedBonusPercent);
        await PlayerCmd.SetEnergy(1, Player);
        int hpBefore = EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp;

        await PlayAndActivateScript(await AddToHand<TwoWordPoetScript>(), EnemyAt(0));

        const int damagePerHit = 3;
        Assert.Equal(
            hpBefore - damagePerHit * TwoWordPoetScript.CanonicalHits,
            EnemyAt(0).CurrentHp + EnemyAt(1).CurrentHp);
        CardTestAssertions.AssertPowerAmount<ScriptTwoWordPoetPower>(
            Player.Creature,
            TwoWordPoetScript.CanonicalDamage);
    }
}
