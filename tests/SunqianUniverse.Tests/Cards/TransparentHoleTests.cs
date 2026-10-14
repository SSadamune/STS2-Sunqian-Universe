using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TransparentHoleTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("transparent-hole");

    [Fact]
    public async Task Energy_X_cascades_against_a_lone_enemy_with_thirty_percent_growth()
    {
        var enemy = EnemyAt(0);
        int hp = enemy.CurrentHp;
        int x = Player.PlayerCombatState!.Energy;
        Assert.Equal(3, x);

        await Play(await AddToHand<TransparentHole>());

        int second = (int)decimal.Floor(TransparentHole.BaseDamage * (1m + TransparentHole.CascadeDamageMultiplier));
        int third = (int)decimal.Floor(second * (1m + TransparentHole.CascadeDamageMultiplier));
        Assert.Equal(hp - TransparentHole.BaseDamage - second - third, enemy.CurrentHp);
    }
}
