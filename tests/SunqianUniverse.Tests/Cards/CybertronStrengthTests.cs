using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class CybertronStrengthTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().AddEnemy<BigDummy>().WithSeed("cybertron-strength");

    [Fact]
    public async Task Total_burning_across_enemies_adds_repeated_block_instances()
    {
        await ApplyPower<BurningPower>(EnemyAt(0), 10);
        await ApplyPower<BurningPower>(EnemyAt(1), 8);

        await Play(await AddToHand<CybertronStrength>());

        Assert.Equal(CybertronStrength.BaseBlock * 3, Player.Creature.Block);
    }
}
