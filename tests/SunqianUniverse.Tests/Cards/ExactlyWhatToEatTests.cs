using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ExactlyWhatToEatTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("exactly-what-to-eat");

    [Fact]
    public async Task Upgrade_exhausts_the_selected_card_draws_two_and_exhausts_itself()
    {
        var selected = await AddToHand<Wound>();
        CardModel[] draws =
        [
            await AddToDrawPileTop<StrikeIronclad>(),
            await AddToDrawPileTop<DefendIronclad>(),
        ];
        var token = Upgrade(await AddToHand<ExactlyWhatToEat>());
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await Play(token);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(selected, PileType.Exhaust);
        AssertInPile(token, PileType.Exhaust);
        Assert.All(draws, drawn => AssertInPile(drawn, PileType.Hand));
    }
}
