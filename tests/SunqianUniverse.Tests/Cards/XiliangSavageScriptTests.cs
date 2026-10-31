using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class XiliangSavageScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle
        .Player<Ironclad>()
        .AddEnemy<BigDummy>()
        .AddEnemy<BigDummy>()
        .WithSeed("xiliang-savage-script");

    [Theory]
    [InlineData(false, 16, 5)]
    [InlineData(true, 21, 8)]
    public async Task Playing_script_costs_three_and_grants_its_block_and_vigor(bool upgraded, int block, int vigor)
    {
        var card = await AddToHand<XiliangSavageScript>();
        if (upgraded) Upgrade(card);
        Assert.Equal(3, card.EnergyCost.Canonical);
        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        Assert.Contains("[gold]格挡[/gold]。", lines[0]);
        Assert.Contains("[gold]活力[/gold]。", lines[1]);

        await PlayAndActivateScript(card);

        Assert.Equal(block, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<VigorPower>(
            Player.Creature,
            vigor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reactivating_script_does_not_grant_vigor_but_playing_another_copy_does(bool upgraded)
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<XiliangSavageScript>();
        if (upgraded) Upgrade(card);
        await Play(card);
        int expected = upgraded ? 8 : 5;
        var record = Squ.Script.ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards
            .OfType<XiliangSavageScriptRecord>().Single();
        for (int i = 0; i < 2; i++)
        {
            await Squ.Script.ScriptSystem.ActivatePlayedScriptAsync(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), Player, record);
            CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, expected);
        }
        await PowerCmd.Remove<VigorPower>(Player.Creature);
        await Squ.Script.ScriptSystem.ActivatePlayedScriptAsync(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), Player, record);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
        var next = await AddToHand<XiliangSavageScript>();
        if (upgraded) Upgrade(next);
        await Play(next);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, expected);
    }

    [Fact]
    public async Task Vigor_used_by_flame_strike_is_permanently_written_into_its_damage_and_burning()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<XiliangSavageScript>()));
        await PlayerCmd.SetEnergy(3, Player);
        var flame = await AddToHand<FlameStrike>();

        await Play(flame, EnemyAt(0));

        Assert.Equal(FlameStrike.BaseDamage + XiliangSavageScript.UpgradedVigor, flame.DynamicVars.Damage.BaseValue);
        Assert.Equal(FlameStrike.BaseBurning + XiliangSavageScript.UpgradedVigor, flame.DynamicVars[nameof(BurningPower)].BaseValue);
        AssertOnlyScript<ScriptXiliangSavagePower>();
    }

    [Fact]
    public async Task Retained_values_include_cross_the_river_and_vigor_amplification()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<XiliangSavageScript>()));
        await ApplyPower<CrossTheRiverPower>(Player.Creature, 1);
        await ApplyPower<VigorAmplificationPower>(Player.Creature, 1);
        var flame = await AddToHand<FlameStrike>();
        Assert.Equal(
            XiliangSavageScript.UpgradedVigor * 3,
            SquVigorSnapshot.GetEffectiveAmount(Player.Creature, flame));

        await PlayerCmd.SetEnergy(3, Player);
        await Play(flame, EnemyAt(0));

        const int effectiveVigor = XiliangSavageScript.UpgradedVigor * 3;
        Assert.Equal(FlameStrike.BaseDamage + effectiveVigor, flame.DynamicVars.Damage.BaseValue);
        Assert.Equal(
            FlameStrike.BaseBurning + effectiveVigor,
            flame.DynamicVars[nameof(BurningPower)].BaseValue);
    }

    [Fact]
    public async Task Multi_command_card_retains_vigor_in_each_matching_damage_variable()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<XiliangSavageScript>()));
        var chickenFoot = await AddToHand<ChickenFootCheeseScript>();

        await PlayerCmd.SetEnergy(3, Player);
        await PlayAndActivateScript(chickenFoot);

        Assert.Equal(
            ChickenFootCheeseScript.BaseDamage + XiliangSavageScript.UpgradedVigor,
            chickenFoot.DynamicVars.Damage.BaseValue);
        Assert.Equal(
            ChickenFootCheeseScript.BaseFollowUpDamage + XiliangSavageScript.UpgradedVigor,
            chickenFoot.DynamicVars[ChickenFootCheeseScript.FollowUpDamageVarName].BaseValue);
    }

    [Fact]
    public async Task Repeated_attack_commands_retain_each_shared_value_only_once()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<XiliangSavageScript>()));
        await PlayerCmd.SetEnergy(2, Player);
        var meteorShower = await AddToHand<FireMeteorShower>();

        await Play(meteorShower);

        Assert.Equal(
            FireMeteorShower.DamageAmount + XiliangSavageScript.UpgradedVigor,
            meteorShower.DynamicVars.Damage.BaseValue);
        Assert.Equal(
            FireMeteorShower.BurningStacks + XiliangSavageScript.UpgradedVigor,
            meteorShower.DynamicVars[nameof(BurningPower)].BaseValue);
    }
}
