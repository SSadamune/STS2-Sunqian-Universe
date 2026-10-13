using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class StarryNightStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-starry-night-strike");
    }

    [Fact]
    public async Task StarryNightStrike_auto_plays_from_hand_at_turn_end()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int totalHpBefore = first.CurrentHp + second.CurrentHp;
        var card = await AddToHand<StarryNightStrike>();

        await EndTurn();

        Assert.Equal(
            totalHpBefore - StarryNightStrike.BaseDamage,
            first.CurrentHp + second.CurrentHp);
        Assert.NotEqual(PileType.Hand, card.Pile?.Type);
    }
}
