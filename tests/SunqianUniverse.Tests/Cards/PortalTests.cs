using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class PortalTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-portal");
    }

    [Fact]
    public async Task Portal_draws_and_auto_plays_an_attack_after_scry()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int totalHpBefore = first.CurrentHp + second.CurrentHp;
        var drawnAttack = await AddToDrawPileTop<StrikeLongtao>();
        var portal = await AddToHand<Portal>();
        portal.DynamicVars[ScryVar.VarName].BaseValue = 0;

        await Play(portal);

        Assert.Equal(
            totalHpBefore - StrikeLongtao.BaseDamage,
            first.CurrentHp + second.CurrentHp);
        AssertInPile(drawnAttack, PileType.Discard);
    }
}
