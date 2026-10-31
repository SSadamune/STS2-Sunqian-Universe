using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ;
using Squ.Cards;
using Squ.Character;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class DrinkingTableCultureTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>().WithSeed("drinking-table-culture");

    [Fact]
    public async Task Gives_an_upgraded_enthralled_wine_to_the_other_player()
    {
        var recipient = Players.Single(player => player != Player);
        await Play(await AddToHand<DrinkingTableCulture>(), recipient.Creature);

        var wine = Assert.Single(PileType.Hand.GetPile(recipient).Cards.OfType<Wine>());
        Assert.True(wine.IsUpgraded);
        Assert.Contains(SquKeywords.Enthralled, wine.Keywords);
    }

    [Fact]
    public async Task Upgrade_removes_exhaust()
    {
        var card = Upgrade(await AddToHand<DrinkingTableCulture>());
        Assert.DoesNotContain(CardKeyword.Exhaust, card.Keywords);
    }

    [Fact]
    public void Hover_preview_is_upgraded_wine_with_enthralled_and_does_not_change_the_real_card()
    {
        var card = Combat.CreateCard<DrinkingTableCulture>(Player);
        CardModel preview = Assert.Single(
            card.HoverTips.OfType<CardHoverTip>()
                .Where(tip => tip.Card is Wine)).Card;

        Assert.True(preview.IsUpgraded);
        Assert.Contains(SquKeywords.Enthralled, preview.Keywords);
        Assert.DoesNotContain(SquKeywords.Enthralled, ModelDb.Card<Wine>().Keywords);
        Assert.Same(ModelDb.CardPool<SunqianCardPool>(), preview.VisualCardPool);
        var realWine = (Wine)ModelDb.Card<Wine>().ToMutable();
        Upgrade(realWine);
        Assert.Equal(realWine.Title, preview.Title);
        Assert.Contains(realWine.GetDescriptionForPile(PileType.Hand), preview.GetDescriptionForPile(PileType.Hand));
    }
}
