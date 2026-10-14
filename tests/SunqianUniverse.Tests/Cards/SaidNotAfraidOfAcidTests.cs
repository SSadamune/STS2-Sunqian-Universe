using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class SaidNotAfraidOfAcidTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("said-not-afraid-of-acid");

    [Fact]
    public async Task Exhausts_selected_unplayable_cards_and_replaces_each_with_upgraded_exhausting_wine()
    {
        var enemy = EnemyAt(0);
        var wound = await AddToHand<Wound>();
        var unselected = await AddToHand<Wound>();
        var selector = new ScriptedCardSelector().ThenSelect(wound);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<SaidNotAfraidOfAcid>()), enemy);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(wound, PileType.Exhaust);
        AssertInPile(unselected, PileType.Hand);
        var wine = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<Wine>());
        Assert.True(wine.IsUpgraded);
        Assert.Contains(CardKeyword.Exhaust, wine.Keywords);
    }

    [Fact]
    public async Task No_unplayable_cards_skips_selection_and_deals_upgraded_damage()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await Play(Upgrade(await AddToHand<SaidNotAfraidOfAcid>()), enemy);

        Assert.Equal(hp - SaidNotAfraidOfAcid.UpgradedDamage, enemy.CurrentHp);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<Wine>());
    }
}
