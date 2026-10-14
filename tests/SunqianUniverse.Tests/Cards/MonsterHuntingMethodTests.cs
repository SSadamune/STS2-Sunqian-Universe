using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class MonsterHuntingMethodTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddRemotePlayer<Ironclad>().AddEnemy<BigDummy>()
            .WithSeed("monster-hunting-method");

    [Fact]
    public async Task Enemy_target_grants_vigor_but_player_target_exhausts_the_card()
    {
        var ally = Players.Single(player => player != Player);
        var enemyCard = Upgrade(await AddToHand<MonsterHuntingMethod>());
        await Play(enemyCard, EnemyAt(0));
        CardTestAssertions.AssertPowerAmount<RegenPower>(EnemyAt(0), MonsterHuntingMethod.UpgradedRegen);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, MonsterHuntingMethod.UpgradedVigor);
        AssertInPile(enemyCard, PileType.Discard);

        var allyCard = Upgrade(await AddToHand<MonsterHuntingMethod>());
        await Play(allyCard, ally.Creature);

        CardTestAssertions.AssertPowerAmount<RegenPower>(ally.Creature, MonsterHuntingMethod.UpgradedRegen);
        AssertInPile(allyCard, PileType.Exhaust);
        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, MonsterHuntingMethod.UpgradedVigor);
    }
}
