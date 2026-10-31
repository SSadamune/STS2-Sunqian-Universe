using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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

        var card = await AddToHand<CybertronStrength>();
        await Play(card);

        Assert.Equal(CybertronStrength.BaseBlock * 3, Player.Creature.Block);
        Assert.Equal(3, CombatManager.Instance.History.Entries.OfType<BlockGainedEntry>()
            .Count(entry => entry.CardPlay?.Card == card));
    }
}
