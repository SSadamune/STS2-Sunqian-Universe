using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Character;
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
        await PlayAndActivateScript(Upgrade(await AddToHand<NightRaidWuchaoScript>()));
        var strikes = PileType.Hand.GetPile(Player).Cards.OfType<StarryNightStrike>().ToArray();
		var scriptPower = Assert.IsType<ScriptNightRaidWuchaoPower>(
			Player.Creature.GetPower<ScriptNightRaidWuchaoPower>());

        Assert.Equal(NightRaidWuchaoScript.GeneratedStrikeCount, strikes.Length);
		Assert.Equal(PowerStackType.Counter, scriptPower.StackType);
		Assert.Equal(NightRaidWuchaoScript.UpgradedBurning, scriptPower.Amount);
        Assert.All(strikes, strike => { Assert.True(strike.IsUpgraded); Assert.Contains(CardKeyword.Exhaust, strike.Keywords); });
        await Play(strikes[0], enemy);
        CardTestAssertions.AssertPowerAmount<BurningPower>(enemy, NightRaidWuchaoScript.UpgradedBurning);
    }

    [Fact]
    public void Hover_preview_is_a_separate_exhausting_starry_night_strike()
    {
        NightRaidWuchaoScript script = Combat.CreateCard<NightRaidWuchaoScript>(Player);
        CardModel preview = Assert.Single(
            script.HoverTips
                .OfType<CardHoverTip>()
                .Where(tip => tip.Card is NightRaidWuchaoScriptStarryNightStrikePreview))
            .Card;

        Assert.Contains(CardKeyword.Exhaust, preview.Keywords);
        Assert.DoesNotContain(CardKeyword.Exhaust, ModelDb.Card<StarryNightStrike>().Keywords);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), preview.VisualCardPool);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), ModelDb.Card<StarryNightStrike>().Pool);
    }
}
