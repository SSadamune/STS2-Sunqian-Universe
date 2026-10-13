using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class DeniedTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-denied");
    }

    [Fact]
    public async Task Denied_returns_from_discard_when_artifact_blocks_an_external_debuff()
    {
        var enemy = EnemyAt(0);
        var denied = await AddCardToPile<Denied>(PileType.Discard);
        var source = await AddToHand<Circus>();
        await ApplyPower<ArtifactPower>(enemy, 1, enemy);

        await ApplyPower<WeakPower>(enemy, 1, Player.Creature, source);

        Assert.Null(enemy.GetPower<WeakPower>());
        AssertInPile(denied, PileType.Hand);
    }
}
