using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
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
}
