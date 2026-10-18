using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class NeverHadTheseRelicTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("never-had-these-relic");

    [Fact]
    public async Task Counter_tracks_only_family_cards_removed_from_its_owners_deck()
    {
        NeverHadTheseRelic relic = CreateRelic();
        CardModel family = Player.RunState.CreateCard<CutePet>(Player);
        CardModel ordinary = Player.RunState.CreateCard<StrikeIronclad>(Player);

        Assert.False(relic.ShowCounter);
        Assert.Equal(0, relic.DisplayAmount);

        await relic.BeforeCardRemoved(ordinary);
        Assert.Equal(0, relic.RemovedFamilyCards);

        await relic.BeforeCardRemoved(family);
        Assert.Equal(1, relic.RemovedFamilyCards);
        Assert.True(relic.ShowCounter);
        Assert.Equal(1, relic.DisplayAmount);
    }

    [Fact]
    public async Task First_player_turn_grants_one_energy_and_dexterity_per_removed_family_card()
    {
        NeverHadTheseRelic relic = CreateRelic();
        relic.RemovedFamilyCards = 3;
        int energyBefore = Player.PlayerCombatState!.Energy;

        await relic.AfterSideTurnStart(
            CombatSide.Player,
            [Player.Creature],
            Combat);

        Assert.Equal(energyBefore + 3, Player.PlayerCombatState.Energy);
        CardTestAssertions.AssertPowerAmount<DexterityPower>(Player.Creature, 3);
    }

    [Fact]
    public async Task Zero_counter_or_a_turn_without_the_owner_does_not_grant_rewards()
    {
        NeverHadTheseRelic relic = CreateRelic();
        int energyBefore = Player.PlayerCombatState!.Energy;

        await relic.AfterSideTurnStart(CombatSide.Player, [], Combat);
        Assert.Equal(energyBefore, Player.PlayerCombatState.Energy);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());

        relic.RemovedFamilyCards = 2;
        await relic.AfterSideTurnStart(CombatSide.Enemy, [EnemyAt(0)], Combat);
        Assert.Equal(energyBefore, Player.PlayerCombatState.Energy);
        Assert.Null(Player.Creature.GetPower<DexterityPower>());
    }

    private NeverHadTheseRelic CreateRelic()
    {
        var relic = (NeverHadTheseRelic)ModelDb.Relic<NeverHadTheseRelic>().ToMutable();
        relic.Owner = Player;
        return relic;
    }
}
