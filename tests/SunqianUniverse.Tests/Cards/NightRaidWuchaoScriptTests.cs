using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class NightRaidWuchaoScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("night-raid-wuchao-script");

    [Fact]
    public async Task Generated_strikes_are_upgraded_exhausting_and_inherit_script_burning()
    {
        var enemy = EnemyAt(0);
        await Play(Upgrade(await AddToHand<NightRaidWuchaoScript>()));
        var strikes = PileType.Hand.GetPile(Player).Cards.OfType<StarryNightStrike>().ToArray();

        Assert.Equal(NightRaidWuchaoScript.GeneratedStrikeCount, strikes.Length);
        Assert.All(strikes, strike => { Assert.True(strike.IsUpgraded); Assert.Contains(CardKeyword.Exhaust, strike.Keywords); });
        await Play(strikes[0], enemy);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, NightRaidWuchaoScript.UpgradedBurning);
    }
}
