using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class HateTheHeavensStrikeTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-hate-the-heavens-strike");
    }

    [Fact]
    public async Task HateTheHeavensStrike_damages_all_enemies()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var card = await AddToHand<HateTheHeavensStrike>();
        card.DynamicVars[ScryVar.VarName].BaseValue = 0;

        await Play(card);

        Assert.Equal(firstHpBefore - HateTheHeavensStrike.DamageAmount, first.CurrentHp);
        Assert.Equal(secondHpBefore - HateTheHeavensStrike.DamageAmount, second.CurrentHp);
    }
}
