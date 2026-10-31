using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using Squ.Script;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Each_strike_damage_hit_triggers_script_burning(bool chickenFootCheese)
    {
        await ApplyPower<GuanDiFormPower>(Player.Creature, 1);
        await ApplyPower<ScriptNightRaidWuchaoPower>(Player.Creature, 3);
        if (chickenFootCheese)
        {
            await ApplyPower<ScriptChickenFootCheesePower>(Player.Creature, 1);
            var strike = await AddToHand<StrikeIronclad>();
            await Play(strike);
        }
        else await Play(await AddToHand<TwinStrike>(), EnemyAt(0));
        CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0), 6);
    }

    [Fact]
    public async Task Ordinary_strike_keeps_its_description_and_does_not_consume_tinder()
    {
        var strike = await AddToHand<StrikeIronclad>();
        string original = strike.GetDescriptionForPile(PileType.Hand);
        await ApplyPower<ScriptNightRaidWuchaoPower>(Player.Creature, 3);
        await ApplyPower<TinderPower>(Player.Creature, 7);
        Assert.False(SquCardTags.AppliesBurning(strike));
        Assert.Equal(original, strike.GetDescriptionForPile(PileType.Hand));
        await Play(strike, EnemyAt(0));
        CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0), 3);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, 7);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_burning_strike_consumes_tinder_only_for_its_own_burning(bool alreadyBurning)
    {
        await ApplyPower<ScriptNightRaidWuchaoPower>(Player.Creature, 3);
        await ApplyPower<TinderPower>(Player.Creature, 7);
        if (alreadyBurning) await ApplyPower<BurningPower>(EnemyAt(0), 1);
        var strike = await AddToHand<FlameStrike>();
        Assert.True(SquCardTags.AppliesBurning(strike));
        await Play(strike, EnemyAt(0));
        int hits = alreadyBurning ? 2 : 1;
        int nativeApplications = alreadyBurning ? 1 : 2;
        CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0),
            (alreadyBurning ? 1 : 0) + hits * 3 + nativeApplications * (FlameStrike.BaseBurning + 7));
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    [Fact]
    public async Task Bowang_arson_discards_native_burning_cards_but_keeps_ordinary_strikes()
    {
        await ApplyPower<GuanDiFormPower>(Player.Creature, 1);
        await ApplyPower<ScriptNightRaidWuchaoPower>(Player.Creature, 3);
        var ordinary = await AddToHand<StrikeIronclad>();
        var native = await AddToHand<FlameStrike>();
        await ApplyPower<ScriptBowangParadoxArsonPower>(Player.Creature, 7);
        AssertInPile(ordinary, PileType.Hand);
        AssertInPile(native, PileType.Discard);
        await Play(ordinary, EnemyAt(0));
        CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0), 3);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, 7);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Damage_events_require_positive_damage_and_accept_blocked_damage(bool blocked)
    {
        await ApplyPower<ScriptNightRaidWuchaoPower>(Player.Creature, 3);
        var strike = await AddToHand<StrikeIronclad>();
        if (blocked) await CreatureCmd.GainBlock(EnemyAt(0), 100, ValueProp.Unpowered, null);
        else await ApplyPower<StrengthPower>(Player.Creature, -100);
        await Play(strike, EnemyAt(0));
        if (blocked) CardTestAssertions.AssertPowerAmount<BurningPower>(EnemyAt(0), 3);
        else Assert.Null(EnemyAt(0).GetPower<BurningPower>());
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
