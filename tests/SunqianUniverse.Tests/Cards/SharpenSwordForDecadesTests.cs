using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SharpenSwordForDecadesTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("sharpen-sword");

    [Fact]
    public async Task Retain_and_forge_render_on_one_leading_line()
    {
        var card = await AddToHand<SharpenSwordForDecades>();
        Assert.IsAssignableFrom<IRowKeywordsCard>(card);

        string[] lines = card.GetDescriptionForPile(PileType.Hand).Split('\n');
        HoverTip retainTip = Assert.IsType<HoverTip>(HoverTipFactory.FromKeyword(CardKeyword.Retain));
        HoverTip forgeTip = Assert.IsType<HoverTip>(HoverTipFactory.Static(StaticHoverTip.Forge));
        string retainMarker = $"[gold]{retainTip.Title}[/gold]";
        string forgeMarker = $"[gold]{forgeTip.Title}[/gold]";
        string keywordLine = Assert.Single(lines.Where(line =>
            line.Contains(retainMarker, StringComparison.Ordinal)));

        Assert.Equal(keywordLine, lines[0]);
        Assert.Contains(retainMarker, keywordLine, StringComparison.Ordinal);
        Assert.Contains(forgeMarker, keywordLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upgraded_retention_charges_four_forge_then_play_forges_and_clears_charge()
    {
        var card = Upgrade(await AddToHand<SharpenSwordForDecades>());

        await EndTurn();

        AssertInPile(card, PileType.Hand);
        Assert.Equal(SharpenSwordForDecades.UpgradedChargeForge, card.DynamicVars.Forge.IntValue);
        await Play(card);

        var blade = Assert.Single(Player.PlayerCombatState!.AllCards.OfType<SovereignBlade>());
        Assert.Equal(10 + SharpenSwordForDecades.UpgradedChargeForge, blade.DynamicVars.Damage.IntValue);
        Assert.Equal(SharpenSwordForDecades.BaseForge, card.DynamicVars.Forge.IntValue);
    }
}
