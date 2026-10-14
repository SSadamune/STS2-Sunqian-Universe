using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
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

        await Play(card);

        Assert.Equal(energyBefore + Wine.BaseEnergy, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, Wine.BaseVigor);
        Assert.Equal(
            dazedBefore + 1,
            PileType.Draw.GetPile(Player).Cards.OfType<Dazed>().Count());
    }
}
