using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class StargazingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("stargazing");

    [Fact]
    public async Task Explicit_discard_immediately_scries_and_discards_the_selected_draw_card()
    {
        var kept = await AddToDrawPileTop<StrikeLongtao>();
        var discarded = await AddToDrawPileTop<DefendLongtao>();
        var stargazing = await AddToHand<Stargazing>();
        var selector = new ScriptedCardSelector().ThenSelect(discarded);

        using (UseCardSelector(selector))
        {
            await CardCmd.Discard(new BlockingPlayerChoiceContext(), stargazing);
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(stargazing, PileType.Discard);
        AssertInPile(discarded, PileType.Discard);
        AssertInPile(kept, PileType.Draw);
    }

    [Fact]
    public async Task Drawing_stargazing_immediately_draws_one_replacement()
    {
        var replacement = await AddToDrawPileTop<DefendLongtao>();
        var stargazing = await AddToDrawPileTop<Stargazing>();
        var portal = await AddToHand<Portal>();
        portal.DynamicVars[ScryVar.VarName].BaseValue = 0;

        await Play(portal);

        AssertInPile(stargazing, PileType.Hand);
        AssertInPile(replacement, PileType.Hand);
    }
}
