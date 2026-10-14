using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class XiliangSavageScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("xiliang-savage-script");

    [Fact]
    public async Task Vigor_used_by_flame_strike_is_permanently_written_into_its_damage_and_burning()
    {
        await Play(Upgrade(await AddToHand<XiliangSavageScript>()));
        var flame = await AddToHand<FlameStrike>();

        await Play(flame, EnemyAt(0));

        Assert.Equal(FlameStrike.BaseDamage + XiliangSavageScript.UpgradedVigor, flame.DynamicVars.Damage.BaseValue);
        Assert.Equal(FlameStrike.BaseBurning + XiliangSavageScript.UpgradedVigor, flame.DynamicVars[nameof(BurningPower)].BaseValue);
        AssertOnlyScript<ScriptXiliangSavagePower>();
    }
}
