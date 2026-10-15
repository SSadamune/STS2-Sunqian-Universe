using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WineTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-wine");
    }

    [Fact]
    public async Task Wine_grants_energy_vigor_and_adds_dazed_to_draw_pile()
    {
        int energyBefore = Player.PlayerCombatState!.Energy;
        int dazedBefore = PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count();
        var card = await AddToHand<Wine>();

        Assert.False(NewsanguoPublicApiInterop.IsReady);
        Assert.DoesNotContain(
            card.HoverTips,
            tip => tip is HoverTip textTip
                && textTip.Title == SquCommonL10n.NewsanguoVigorSwapHoverTitle()
                    .GetFormattedText());
        Assert.DoesNotContain(
            "[purple]",
            card.GetDescriptionForPile(PileType.Hand));
        Assert.Contains(
            ModelDb.Power<VigorPower>().Title.GetFormattedText(),
            card.GetDescriptionForPile(PileType.Hand));

        await Play(card);

        Assert.Equal(energyBefore + Wine.BaseEnergy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, Wine.BaseVigor);
        Assert.Equal(
            dazedBefore + 1,
            PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count());
    }
}
