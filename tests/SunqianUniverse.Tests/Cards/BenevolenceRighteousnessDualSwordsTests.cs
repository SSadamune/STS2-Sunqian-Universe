using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class BenevolenceRighteousnessDualSwordsTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("dual-swords");

    [Fact]
    public async Task Strike_cards_resolve_one_extra_time_and_upgrade_reduces_cost()
    {
        var powerCard = Upgrade(await AddToHand<BenevolenceRighteousnessDualSwords>());
        Assert.Equal(1, powerCard.EnergyCost.GetResolved());
        await Play(powerCard);
        CardTestAssertions.AssertPowerAmount<BenevolenceRighteousnessDualSwordsPower>(
            Player.Creature,
            BenevolenceRighteousnessDualSwords.ExtraPlaysPerCard);
        int hp = EnemyAt(0).CurrentHp;

        await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

        Assert.Equal(hp - 12, EnemyAt(0).CurrentHp);
    }

    [Fact]
    public async Task Non_strike_cards_are_not_replayed()
    {
        await Play(await AddToHand<BenevolenceRighteousnessDualSwords>());

        await Play(await AddToHand<DefendIronclad>());

        Assert.Equal(5, Player.Creature.Block);
    }
}
