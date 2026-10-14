using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class LensPreShootTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("lens-pre-shoot");

    [Fact]
    public async Task Upgrade_moves_only_the_selected_draw_pile_script_to_hand_and_makes_it_free()
    {
        var unselected = await AddToDrawPileTop<SunqianScript>();
        var selected = await AddToDrawPileTop<XiliangSavageScript>();
        var selector = new ScriptedCardSelector().ThenSelect(selected);

        using (UseCardSelector(selector))
        {
            await Play(Upgrade(await AddToHand<LensPreShoot>()));
        }

        AssertAllSelectionsConsumed(selector);
        AssertInPile(selected, PileType.Hand);
        Assert.Equal(0, selected.EnergyCost.GetResolved());
        AssertInPile(unselected, PileType.Draw);
    }
}
