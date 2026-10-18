using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using STS2RitsuLib.Keywords;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BlitzkriegTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("blitzkrieg");

    [Fact]
    public async Task War_feeds_war_and_supreme_general_render_together_before_the_rules_text()
    {
        var card = await AddToHand<Blitzkrieg>();
        Assert.IsAssignableFrom<IRowKeywordsCard>(card);
        Assert.True(ModKeywordRegistry.TryGetByCardKeyword(
            SquKeywords.WarFeedsWar,
            out ModKeywordDefinition definition));
        Assert.Equal(
            ModKeywordCardDescriptionPlacement.BeforeCardDescription,
            definition.CardDescriptionPlacement);

        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        string[] markers =
        [
            KeywordMarker(SquKeywords.WarFeedsWar),
            KeywordMarker(SquKeywords.SupremeGeneral),
        ];
        string keywordLine = Assert.Single(
            lines.Where(line => markers.Any(marker => line.Contains(marker, StringComparison.Ordinal))));

        Assert.Equal(keywordLine, lines[0]);
        Assert.All(markers, marker => Assert.Contains(marker, keywordLine, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Plays_basic_strikes_from_hand_and_draw_but_ignores_non_strikes()
    {
        var handStrike = await AddCardToPile<StrikeIronclad>(PileType.Hand);
        var drawStrike = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var ignored = await AddCardToPile<DefendIronclad>(PileType.Draw);
        CardModel[] allStrikes = PileType.Hand.GetPile(Player).Cards
            .Concat(PileType.Draw.GetPile(Player).Cards)
            .Where(card => card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike))
            .ToArray();
        int hp = EnemyAt(0).CurrentHp;

        await Play(Upgrade(await AddToHand<Blitzkrieg>()), EnemyAt(0));

        Assert.Equal(hp - Blitzkrieg.UpgradedDamage - 6 * allStrikes.Length, EnemyAt(0).CurrentHp);
        Assert.All(allStrikes, strike => AssertInPile(strike, PileType.Discard));
        AssertInPile(handStrike, PileType.Discard);
        AssertInPile(drawStrike, PileType.Discard);
        AssertInPile(ignored, PileType.Draw);
    }

    [Fact]
    public async Task A_distinct_blitzkrieg_can_recurse_without_replaying_the_same_instance_forever()
    {
        await CardPileCmd.Add(PileType.Hand.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
        var nested = await AddCardToPile<Blitzkrieg>(PileType.Draw);
        var strike = await AddCardToPile<StrikeIronclad>(PileType.Draw);
        var root = await AddToHand<Blitzkrieg>();
        int hp = EnemyAt(0).CurrentHp;

        await Play(root, EnemyAt(0));

        Assert.Equal(hp - Blitzkrieg.BaseDamage * 2 - 6, EnemyAt(0).CurrentHp);
        AssertInPile(root, PileType.Discard);
        AssertInPile(nested, PileType.Discard);
        AssertInPile(strike, PileType.Discard);
    }

    private static string KeywordMarker(CardKeyword keyword)
    {
        if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out _))
        {
            return $"[gold]{ModKeywordRegistry.GetTitle(keyword).GetFormattedText()}[/gold]";
        }

        HoverTip tip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(keyword));
        return "[gold]" + tip.Title + "[/gold]";
    }
}
