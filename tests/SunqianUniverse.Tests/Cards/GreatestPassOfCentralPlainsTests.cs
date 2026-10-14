using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class GreatestPassOfCentralPlainsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("greatest-pass");

    [Fact]
    public async Task Moves_to_hand_before_combat_hook_and_grants_upgraded_unpowered_block_to_ally()
    {
        var ally = Players.Single(player => player != Player);
        var card = Upgrade(await AddCardToPile<GreatestPassOfCentralPlains>(PileType.Draw));
        await ApplyPower<DexterityPower>(ally.Creature, 5);

        await card.BeforeCombatStart();
        AssertInPile(card, PileType.Hand);
        await Play(card, ally.Creature);

        Assert.Equal(GreatestPassOfCentralPlains.UpgradedBlockAmount, ally.Creature.Block);
    }
}
