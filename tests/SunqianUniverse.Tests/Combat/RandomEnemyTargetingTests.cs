using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Combat;

public sealed class RandomEnemyTargetingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("required-random-enemy-target");

    [Fact]
    public async Task Explicit_target_is_included_when_random_targets_are_selected()
    {
        var requiredTarget = EnemyAt(3);
        int hp = requiredTarget.CurrentHp;
        var salvo = await AddToHand<SalvoStrike>();

        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), salvo, requiredTarget);

        Assert.Equal(hp - SalvoStrike.BaseDamage, requiredTarget.CurrentHp);
        Assert.Equal(
            SalvoStrike.RandomEnemyTargetCount,
            Enumerable.Range(0, 4).Count(index => EnemyAt(index).CurrentHp < EnemyAt(index).MaxHp));
    }
}
