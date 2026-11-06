using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class MilkDragonEmergesTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<LiubeiCharacter>()
        .AddEnemy<BigDummy>().WithSeed("milk-dragon-emerges");

    [Theory]
    [InlineData("zhs", "并丢弃所有非[gold]保留[/gold]的[gold]攻击[/gold]牌；或者获得")]
    [InlineData("eng", "and discard all [gold]Attacks[/gold] without [gold]Retain[/gold]; or gain")]
    public void Milk_dragon_description_uses_the_discard_and_alternative_wording(string language, string expected)
    {
        using JsonDocument table = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            $"res://sunqian-universe/localization/{language}/cards.json"));
        string text = table.RootElement
            .GetProperty("SUNQIAN_UNIVERSE_CARD_MILK_DRAGON_EMERGES.description")
            .GetString()!;
        Assert.Contains(expected, text);
        Assert.DoesNotContain(language == "zhs" ? "选择一项：" : "Choose one:", text);
    }

    [Theory]
    [InlineData("zhs", "博望悖论剧本", "将[gold]奶龙出山[/gold]的两个选项转化为剧本，并激活其中之一。")]
    [InlineData("eng", "Bowang Paradox Script", "Turn the two options of [gold]Milk Dragon Emerges[/gold] into Scripts, then activate one.")]
    public void Milk_dragon_preview_localization_is_consistent_in_both_languages(
        string language, string title, string description)
    {
        using JsonDocument table = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            $"res://sunqian-universe/localization/{language}/cards.json"));
        Assert.Equal(title, table.RootElement
            .GetProperty("SUNQIAN_UNIVERSE_CARD_MILK_DRAGON_BOWANG_SCRIPT_PREVIEW.title")
            .GetString());
        Assert.Equal(description, table.RootElement
            .GetProperty("SUNQIAN_UNIVERSE_CARD_MILK_DRAGON_BOWANG_SCRIPT_PREVIEW.description")
            .GetString());
    }

    [Theory]
    [InlineData(false, false, 7)]
    [InlineData(false, true, 7)]
    [InlineData(true, false, 9)]
    [InlineData(true, true, 9)]
    public async Task Immediate_options_match_the_two_scripts_without_unlocking_or_activating_scripts(bool upgraded, bool arson, int amount)
    {
        foreach (var existing in Player.PlayerCombatState!.Hand.Cards.ToArray())
            await CardPileCmd.Add(existing, PileType.Discard);
        var attack = await AddToHand<StrikeLiubei>();
        var burningAttack = await AddToHand<FlameStrike>();
        var burningSkill = await AddToHand<BlockFirefighting>();
        var retainedAttack = await AddToHand<FingerSnapStrike>();
        var retainedBurningSkill = await AddToHand<BlockFirefighting>();
        CardCmd.ApplyKeyword(retainedBurningSkill, CardKeyword.Retain);
        var ordinary = await AddToHand<DefendLiubei>();
        var card = await AddToHand<MilkDragonEmerges>();
        if (upgraded) Upgrade(card);
        Assert.Equal(1, card.EnergyCost.GetResolved());
        Assert.Contains("牌；或者获得", card.GetDescriptionForPile(PileType.Hand));
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Contains(SquCardTags.Dragon, card.Tags);
        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Equal(2, options.Count);
            Assert.All(options, option => Assert.Equal(amount, option.DynamicVars["Amount"].IntValue));
            return [options.OfType<MilkDragonOption>().Single(option => option.IsArson == arson)];
        });
        using (UseCardSelector(selector)) await Play(card);
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(amount, arson ? Player.Creature.GetPower<TinderPower>()!.Amount : Player.Creature.GetPower<VigorPower>()!.Amount);
        AssertInPile(attack, arson ? PileType.Hand : PileType.Discard);
        AssertInPile(burningAttack, PileType.Discard);
        AssertInPile(burningSkill, arson ? PileType.Discard : PileType.Hand);
        AssertInPile(retainedAttack, PileType.Hand);
        AssertInPile(retainedBurningSkill, PileType.Hand);
        AssertInPile(ordinary, PileType.Hand);
        AssertInPile(card, PileType.Discard);
        Assert.Empty(ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards);
        Assert.Empty(Player.Creature.Powers.OfType<ScriptPowerTemplate>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Slight_revision_creates_the_matching_bowang_script_which_still_unlocks_both_records(bool upgraded)
    {
        var card = await AddToHand<MilkDragonEmerges>();
        if (upgraded) Upgrade(card);
        Assert.True(SlightRevisionSystem.TryGetRevision(card, out var target, out bool targetUpgraded));
        Assert.IsType<BowangParadoxScript>(target);
        Assert.Equal(upgraded, targetUpgraded);
        Assert.Contains("稍作修改", card.GetDescriptionForPile(PileType.Hand));
        await SlightRevisionSystem.TransformAsync(card, target, targetUpgraded);
        var script = Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<BowangParadoxScript>());
        Assert.Equal(upgraded, script.IsUpgraded);
        Assert.Equal(CardRarity.Token, script.Rarity);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), script.VisualCardPool);
        Assert.Contains(script.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is BowangParadoxAdvanceScriptRecord);
        Assert.Contains(script.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is BowangParadoxArsonScriptRecord);
        var selector = new ScriptedCardSelector().ThenSelectWhere(option => option is BowangParadoxAdvanceScriptRecord);
        using (UseCardSelector(selector)) await Play(script);
        AssertAllSelectionsConsumed(selector);
        Assert.Equal(2, ScriptSystem.ScriptCollectionPile.GetPile(Player).Cards.Count);
        Assert.NotNull(Player.Creature.GetPower<ScriptBowangParadoxAdvancePower>());
        Assert.Null(Player.Creature.GetPower<ScriptBowangParadoxArsonPower>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Only_milk_dragons_bowang_hover_omits_nested_script_cards(bool upgraded)
    {
        var milk = await AddToHand<MilkDragonEmerges>();
        if (upgraded) Upgrade(milk);
        var preview = Assert.IsType<MilkDragonBowangScriptPreview>(Assert.Single(milk.HoverTips.OfType<CardHoverTip>()).Card);
        Assert.Equal(upgraded, preview.IsUpgraded);
        Assert.Equal(CardRarity.Token, preview.Rarity);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), preview.VisualCardPool);
        Assert.Equal("博望悖论剧本" + (upgraded ? "+" : ""), preview.Title);
        Assert.False(preview.ShouldShowInCardLibrary);
        Assert.Contains("将[gold]奶龙出山[/gold]的两个选项转化为剧本，并激活其中之一。", preview.GetDescriptionForPile(PileType.Deck));
        Assert.DoesNotContain(milk.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is ScriptRecordCardTemplate);
        Assert.DoesNotContain(preview.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is ScriptRecordCardTemplate);
        var actual = await AddToHand<BowangParadoxScript>();
        Assert.Equal(2, actual.HoverTips.OfType<CardHoverTip>().Count(tip => tip.Card is ScriptRecordCardTemplate));
        Assert.Contains("从两种[gold]剧本[/gold]中选择一种激活，另一种也会加入剧本集。", actual.GetDescriptionForPile(PileType.Hand));
        Assert.DoesNotContain("奶龙出山", actual.GetDescriptionForPile(PileType.Hand));
        Assert.Equal(2, ModelDb.Card<BowangParadoxScript>().HoverTips.OfType<CardHoverTip>().Count(tip => tip.Card is ScriptRecordCardTemplate));
        var other = await AddToHand<DefendLiubei>();
        SlightRevisionSystem.Set(other, ModelDb.Card<BowangParadoxScript>(), upgraded);
        Assert.Equal(2, other.HoverTips.OfType<CardHoverTip>().Count(tip => tip.Card is ScriptRecordCardTemplate));
        SlightRevisionSystem.Set(milk, ModelDb.Card<BowangParadoxScript>(), upgraded);
        Assert.IsType<MilkDragonBowangScriptPreview>(Assert.Single(milk.HoverTips.OfType<CardHoverTip>()).Card);
        Assert.DoesNotContain(milk.HoverTips.OfType<CardHoverTip>(), tip => tip.Card is ScriptRecordCardTemplate);
    }

    [Fact]
    public async Task Dragon_tag_triggers_after_the_option_has_finished_discarding_attacks()
    {
        await CompanionSummons.SummonSpecific(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            Player, CompanionSummons.SecondBrotherId, 10, null);
        await Play(await AddToHand<DragonEmperorMarch>());
        var earlier = Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<EmperorsMarch>());
        var selector = new ScriptedCardSelector().ThenSelectWhere(option => option is MilkDragonAdvance);
        using (UseCardSelector(selector)) await Play(await AddToHand<MilkDragonEmerges>());
        AssertInPile(earlier, PileType.Discard);
        Assert.Single(Player.PlayerCombatState.Hand.Cards.OfType<EmperorsMarch>());
    }
}
