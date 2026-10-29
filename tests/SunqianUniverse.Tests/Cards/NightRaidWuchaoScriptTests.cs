using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using Squ.Script;
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
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 3)]
    public async Task Material_reuse_upgrades_the_record_and_burning(bool firstUpgraded, bool secondUpgraded, int level)
    {
        await PlayerCmd.SetEnergy(10, Player);
        var first = await AddToHand<NightRaidWuchaoScript>();
        if (firstUpgraded) Upgrade(first);
        await Play(first);
        var second = await AddToHand<NightRaidWuchaoScript>();
        if (secondUpgraded) Upgrade(second);
        await Play(second);
        var record = Assert.Single(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.OfType<NightRaidWuchaoScriptRecord>());
        Assert.Equal(level, record.CurrentUpgradeLevel);
        Assert.Equal($"夜袭乌巢+{level}", record.Title);
        Assert.Equal(3 + level * 2, record.StoredAmount);
        CardTestAssertions.AssertPowerAmount<ScriptNightRaidWuchaoPower>(Player.Creature, record.StoredAmount);
        Assert.Contains(SquKeywords.MaterialReuse, record.Keywords);
        Assert.DoesNotContain(SquKeywords.MaterialReuse, first.Keywords);
        string description = record.GetDescriptionForPile(PileType.Deck);
        Assert.EndsWith("[gold]素材复用[/gold]。", description);
        var restored = Assert.IsType<NightRaidWuchaoScriptRecord>(CardModel.FromSerializable(record.ToSerializable()));
        Assert.Equal(record.CurrentUpgradeLevel, restored.CurrentUpgradeLevel);
        Assert.Equal(record.StoredAmount, restored.StoredAmount);
        Assert.Equal(record.Title, restored.Title);
        await Play(await AddToHand<NightRaidWuchaoScript>());
        Assert.Equal(level + 1, record.CurrentUpgradeLevel);
        Assert.Equal(3 + (level + 1) * 2, record.StoredAmount);
        CardTestAssertions.AssertPowerAmount<ScriptNightRaidWuchaoPower>(Player.Creature, record.StoredAmount);
    }

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
