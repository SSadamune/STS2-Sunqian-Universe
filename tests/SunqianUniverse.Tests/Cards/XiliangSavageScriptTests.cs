using MegaCrit.Sts2.Core.Commands;
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

    [Fact]
    public async Task Vigor_used_by_flame_strike_is_permanently_written_into_its_damage_and_burning()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<XiliangSavageScript>()));
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
        Assert.Equal(18, SquVigorSnapshot.GetEffectiveAmount(Player.Creature, flame));

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
