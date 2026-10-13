using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KeepCalmTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("keep-calm");

    [Fact]
    public async Task Retaining_upgraded_card_adds_two_to_stored_energy_then_play_refunds_it()
    {
        var card = Upgrade(await AddToHand<KeepCalm>());
        await EndTurn();
        AssertInPile(card, PileType.Hand);
        Assert.Equal(KeepCalm.CanonicalEnergy + KeepCalm.UpgradedRetainBonus, card.DynamicVars.Energy.IntValue);

        int before = Player.PlayerCombatState!.Energy;
        await Play(card);
        Assert.Equal(before - 1 + KeepCalm.CanonicalEnergy + KeepCalm.UpgradedRetainBonus, Player.PlayerCombatState.Energy);
    }
}
