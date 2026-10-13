using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LensReshootTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("lens-reshoot");

    [Fact]
    public async Task Retrieves_the_only_discarded_script_and_makes_it_free_this_turn()
    {
        var script = await AddCardToPile<XiliangSavageScript>(PileType.Discard);
        await Play(await AddToHand<LensReshoot>());

        AssertInPile(script, PileType.Hand);
        Assert.Equal(0, script.EnergyCost.GetResolved());
    }
}
