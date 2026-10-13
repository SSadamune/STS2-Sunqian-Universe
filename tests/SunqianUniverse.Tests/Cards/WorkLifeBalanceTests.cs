using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class WorkLifeBalanceTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-work-life-balance");
    }

    [Fact]
    public async Task WorkLifeBalance_adds_spent_vigor_to_block_then_resets_charge()
    {
        var enemy = EnemyAt(0);
        var workLifeBalance = await AddToHand<WorkLifeBalance>();
        await ApplyPower<VigorPower>(Player.Creature, 3);
        var attack = await AddToHand<StrikeLongtao>();

        await Play(attack, enemy);

        Assert.Equal(
            WorkLifeBalance.CanonicalBlock + 3,
            workLifeBalance.DynamicVars.Block.BaseValue);
        await Play(workLifeBalance);

        Assert.Equal(WorkLifeBalance.CanonicalBlock + 3, Player.Creature.Block);
        Assert.Equal(
            WorkLifeBalance.CanonicalBlock,
            workLifeBalance.DynamicVars.Block.BaseValue);
    }
}
