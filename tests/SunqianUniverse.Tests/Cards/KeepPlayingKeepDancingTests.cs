using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class KeepPlayingKeepDancingTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("keep-playing-keep-dancing");

    [Fact]
    public async Task First_attack_spends_then_restores_vigor_and_one_keep_stack()
    {
        var enemy = EnemyAt(0);
        await Play(Upgrade(await AddToHand<KeepPlayingKeepDancing>()));
        await Play(await AddToHand<StrikeLongtao>(), enemy);

        CardTestAssertions.AssertPowerAmount<VigorPower>(Player.Creature, KeepPlayingKeepDancing.BaseVigor);
        CardTestAssertions.AssertPowerAmount<KeepVigorPower>(Player.Creature, KeepPlayingKeepDancing.UpgradedKeepVigor - 1);
    }
}
