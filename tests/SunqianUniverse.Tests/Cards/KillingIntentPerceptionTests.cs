using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KillingIntentPerceptionTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("killing-intent-perception");

    [Fact]
    public async Task Solo_play_hits_all_enemies_without_requesting_an_ally_choice()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHp = first.CurrentHp;
        int secondHp = second.CurrentHp;

        await Play(Upgrade(await AddToHand<KillingIntentPerception>()), first);

        Assert.Equal(firstHp - KillingIntentPerception.UpgradedDamage, first.CurrentHp);
        Assert.Equal(secondHp - KillingIntentPerception.UpgradedDamage, second.CurrentHp);
    }
}
