using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MilkDragonEmergesSunqianTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<SunqianCharacter>().AddEnemy<BigDummy>().WithSeed("milk-dragon-emerges-sunqian");

    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 9)]
    public async Task Sunqian_can_play_milk_dragon_and_choose_advance(bool upgraded, int amount)
    {
        var attack = await AddToHand<StrikeLongtao>();
        var card = await AddToHand<MilkDragonEmerges>();
        if (upgraded)
        {
            Upgrade(card);
        }

        var selector = new ScriptedCardSelector().ThenSelect(options =>
        {
            Assert.Equal(2, options.Count);
            return [options.OfType<MilkDragonAdvance>().Single()];
        });

        using (UseCardSelector(selector))
        {
            await Play(card);
        }

        AssertAllSelectionsConsumed(selector);
        Assert.Equal(amount, Player.Creature.GetPower<VigorPower>()!.Amount);
        AssertInPile(attack, PileType.Discard);
        AssertInPile(card, PileType.Discard);
    }

    [Fact]
    public async Task Sunqian_milk_dragon_preview_is_a_token_with_sunqian_visuals()
    {
        var milk = await AddToHand<MilkDragonEmerges>();
        var preview = Assert.IsType<MilkDragonBowangScriptPreview>(
            Assert.Single(milk.HoverTips.OfType<CardHoverTip>()).Card);

        Assert.Equal(CardRarity.Token, preview.Rarity);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), preview.VisualCardPool);
        Assert.False(preview.ShouldShowInCardLibrary);
    }
}
