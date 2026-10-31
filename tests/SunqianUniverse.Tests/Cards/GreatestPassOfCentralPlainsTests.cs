using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Keywords;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GreatestPassOfCentralPlainsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("greatest-pass");

    [Fact]
    public async Task Retain_and_war_feeds_war_render_on_one_leading_line()
    {
        var card = await AddToHand<GreatestPassOfCentralPlains>();
        Assert.IsAssignableFrom<IRowKeywordsCard>(card);

        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        string[] markers =
        [
            KeywordMarker(CardKeyword.Retain),
            KeywordMarker(SquKeywords.WarFeedsWar),
        ];
        string keywordLine = Assert.Single(
            lines.Where(line => markers.Any(marker => line.Contains(marker, StringComparison.Ordinal))));

        Assert.Equal(keywordLine, lines[0]);
        Assert.All(markers, marker => Assert.Contains(marker, keywordLine, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Moves_to_hand_before_combat_hook_and_grants_upgraded_unpowered_block_to_ally()
    {
        var ally = Players.Single(player => player != Player);
        var card = Upgrade(await AddCardToPile<GreatestPassOfCentralPlains>(PileType.Draw));
        await ApplyPower<DexterityPower>(ally.Creature, 5);

        await card.BeforeCombatStart();
        AssertInPile(card, PileType.Hand);
        await Play(card, ally.Creature);

        Assert.Equal(GreatestPassOfCentralPlains.UpgradedBlockAmount, ally.Creature.Block);
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
