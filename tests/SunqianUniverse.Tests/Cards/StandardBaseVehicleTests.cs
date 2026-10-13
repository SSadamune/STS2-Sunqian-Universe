using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class StandardBaseVehicleTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("standard-base-vehicle");

    [Fact]
    public async Task Upgraded_zero_cost_card_blocks_and_exhausts()
    {
        var card = Upgrade(await AddToHand<StandardBaseVehicle>());
        await Play(card);

        Assert.Equal(StandardBaseVehicle.UpgradedBlock, Player.Creature.Block);
        AssertInPile(card, PileType.Exhaust);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
    }
}
