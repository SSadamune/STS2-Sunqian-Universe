using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using Squ.Cards;
using Squ.Combat;
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

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(1, false, 3)]
    [InlineData(9, false, 3)]
    [InlineData(1, true, 2)]
    [InlineData(9, true, 2)]
    public void Distinct_selection_preserves_rng_consumption_and_required_target_order(
        int count, bool requireTarget, int expectedDraws)
    {
        var rng = new Rng(1234);
        var expectedRng = new Rng(1234);
        var required = requireTarget ? EnemyAt(3) : null;
        var targets = SquRandomEnemyTargeting.PickRandomEnemiesUnique(
            Player.Creature.CombatState!, count, rng, required).ToList();

        Assert.Equal(Math.Min(count, 4), targets.Count);
        Assert.Equal(targets.Count, targets.Distinct().Count());
        if (requireTarget && count > 0) Assert.Same(required, targets[0]);
        // Full Fisher-Yates consumes poolSize - 1 values, even when only the
        // required target is returned or the requested count exceeds the pool.
        for (int size = expectedDraws + 1; size > 1; size--)
            expectedRng.NextInt(0, size);
        Assert.Equal(expectedRng.NextInt(), rng.NextInt());
    }
}
