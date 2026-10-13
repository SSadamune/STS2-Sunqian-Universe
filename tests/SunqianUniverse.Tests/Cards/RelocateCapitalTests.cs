using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class RelocateCapitalTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("relocate-capital");

    [Fact]
    public async Task Spent_vigor_becomes_block_and_tinder()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        await ApplyPower<VigorPower>(Player.Creature, 4);

        await Play(await AddToHand<RelocateCapital>(), enemy);

        Assert.Equal(hp - RelocateCapital.DamageAmount - 4, enemy.CurrentHp);
        Assert.Equal(4, Player.Creature.Block);
        CardTestAssertions.AssertPowerAmount<TinderPower>(Player.Creature, 4);
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }
}
