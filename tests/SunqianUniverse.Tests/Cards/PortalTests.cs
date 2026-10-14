using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PortalTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-portal");
    }

    [Fact]
    public async Task Portal_discards_selected_scry_cards_then_draws_and_auto_plays_the_revealed_attack()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int totalHpBefore = first.CurrentHp + second.CurrentHp;
        var drawnAttack = await AddToDrawPileTop<StrikeLongtao>();
        var firstDiscard = await AddToDrawPileTop<DefendLongtao>();
        var secondDiscard = await AddToDrawPileTop<DefendLongtao>();
        var portal = await AddToHand<Portal>();
        var selector = new ScriptedCardSelector().ThenSelect(firstDiscard, secondDiscard);

        using (UseCardSelector(selector))
        {
            await Play(portal);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(firstDiscard, PileType.Discard);
        AssertInPile(secondDiscard, PileType.Discard);
        Assert.Equal(
            totalHpBefore - StrikeLongtao.BaseDamage,
            first.CurrentHp + second.CurrentHp);
        AssertInPile(drawnAttack, PileType.Discard);
    }
}
